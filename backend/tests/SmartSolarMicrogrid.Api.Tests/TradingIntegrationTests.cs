/*
 * File: tests/SmartSolarMicrogrid.Api.Tests/TradingIntegrationTests.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Automated verification and test support for Trading Integration Tests.
 */
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using SmartSolarMicrogrid.Api.DTOs.Auth;
using SmartSolarMicrogrid.Api.DTOs.Reservations;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Repositories;
using SmartSolarMicrogrid.Api.Services;
using Xunit;

namespace SmartSolarMicrogrid.Api.Tests;

public sealed class TradingIntegrationTests : IAsyncLifetime
{
    private readonly string databaseName = "ss_test_" + Guid.NewGuid().ToString("N")[..24];
    private readonly DateTime now = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).UtcDateTime;
    private WebApplicationFactory<Program> factory = null!;
    private MongoDbContext database = null!;
    private HttpClient owner = null!, other = null!, admin = null!, grid = null!;
    private SolarStationInfo station = null!;
    private EnergyBookingSlot slot = null!;

    public async Task InitializeAsync()
    {
        // Create an isolated test database and initialize the API test clients.
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = Environment.GetEnvironmentVariable("SMARTSOLAR_TEST_MONGODB_URI"),
                ["MongoDb:DatabaseName"] = databaseName,
                ["Jwt:SigningKey"] = AuthTestSettings.SigningKey
            }));
            builder.ConfigureServices(services => services.AddSingleton<TimeProvider>(new FixedClock(now)));
        });
        using var start = factory.CreateClient();
        database = factory.Services.GetRequiredService<MongoDbContext>();
        owner = await Client(UserRole.PROSUMER, "200012345678");
        other = await Client(UserRole.PROSUMER, "200012345679");
        admin = await Client(UserRole.BACKOFFICE, null);
        grid = await Client(UserRole.GRID_OPERATOR, null);
        station = new SolarStationInfo { StationCode = "TEST-STATION", Name = "Test station", Address = "Test address",
            Location = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new(79.86, 6.92)), CapacityKWh = 100,
            AvailableBatterySlots = 10, Status = StationStatus.ACTIVE,
            OperatingSchedule = new OperatingSchedule { TimeZoneId = "UTC", WeeklyPeriods = Enum.GetValues<DayOfWeek>()
                .Select(d => new OperatingPeriod { Day = d, OpenMinuteOfDay = 0, CloseMinuteOfDay = 1440 }).ToList() } };
        await database.Stations.InsertOneAsync(station);
        slot = new EnergyBookingSlot { StationId = station.Id, StartTime = now.Date.AddDays(2).AddHours(8),
            EndTime = now.Date.AddDays(2).AddHours(9), Capacity = 100, AvailableCapacity = 100, Status = SlotStatus.OPEN };
        await database.Slots.InsertOneAsync(slot);
    }

    private async Task<HttpClient> Client(UserRole role, string? nic)
    {
        // Client for Trading Integration Tests.
        const string password = "Integration test passphrase 123!";
        var user = new User { FullName = "Test User", Email = Guid.NewGuid() + "@example.invalid", Phone = "0771234567",
            NIC = nic, Role = role, Status = UserStatus.ACTIVE };
        user.PasswordHash = factory.Services.GetRequiredService<PasswordService>().Hash(user, password);
        await database.Users.InsertOneAsync(user);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/auth/login", new { identifier = user.Email, password });
        await Expect(response, HttpStatusCode.OK);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken);
        return client;
    }

    [MongoFact]
    public async Task Staff_book_for_active_prosumers_but_cannot_bypass_identity_or_notice_rules()
    {
        foreach (var client in new[] { admin, grid })
        {
            var response = await client.PostAsJsonAsync("/api/reservations", new { slotId = slot.Id.ToString(), energyAmount = 10, prosumerNIC = "200012345678" });
            await Expect(response, HttpStatusCode.Created);
            var booking = (await response.Content.ReadFromJsonAsync<ReservationResponse>())!;
            Assert.Equal("200012345678", booking.ProsumerNIC);
            await Expect(await client.PutAsJsonAsync($"/api/reservations/{booking.Id}", new { energyAmount = 12 }), HttpStatusCode.OK);
            await database.Reservations.UpdateOneAsync(x => x.Id == ObjectId.Parse(booking.Id),
                Builders<EnergyReservation>.Update.Set(x => x.ReservationDateTime, now.AddHours(11)));
            await Expect(await client.PutAsJsonAsync($"/api/reservations/{booking.Id}", new { energyAmount = 14 }), HttpStatusCode.Conflict);
        }
        await Expect(await admin.PostAsJsonAsync("/api/reservations", new { slotId = slot.Id.ToString(), energyAmount = 10 }), HttpStatusCode.BadRequest);
        await Expect(await owner.PostAsJsonAsync("/api/reservations", new { slotId = slot.Id.ToString(), energyAmount = 10, prosumerNIC = "200012345679" }), HttpStatusCode.Forbidden);
        await database.Users.UpdateOneAsync(x => x.NIC == "200012345679", Builders<User>.Update.Set(x => x.Status, UserStatus.DEACTIVATED));
        await Expect(await grid.PostAsJsonAsync("/api/reservations", new { slotId = slot.Id.ToString(), energyAmount = 10, prosumerNIC = "200012345679" }), HttpStatusCode.Forbidden);
    }

    [MongoFact]
    public async Task Reschedule_moves_capacity_and_invalidates_approval_and_QR()
    {
        var booking = await Book();
        await Expect(await admin.PatchAsync($"/api/reservations/{booking.Id}/approve", null), HttpStatusCode.OK);
        var qr = (await owner.GetFromJsonAsync<QrTokenResponse>($"/api/reservations/{booking.Id}/qr"))!;
        var secondStation = MongoModelTests.NewStation("SECOND-STATION");
        secondStation.Status = StationStatus.ACTIVE; secondStation.AvailableBatterySlots = 5;
        secondStation.OperatingSchedule = station.OperatingSchedule;
        await database.Stations.InsertOneAsync(secondStation);
        var target = new EnergyBookingSlot { StationId = secondStation.Id, StartTime = slot.StartTime.AddDays(1),
            EndTime = slot.EndTime.AddDays(1), Capacity = 50, AvailableCapacity = 50, Status = SlotStatus.OPEN };
        await database.Slots.InsertOneAsync(target);
        await Expect(await other.PutAsJsonAsync($"/api/reservations/{booking.Id}", new { slotId = target.Id.ToString(), energyAmount = 20 }), HttpStatusCode.Forbidden);
        var response = await owner.PutAsJsonAsync($"/api/reservations/{booking.Id}", new { slotId = target.Id.ToString(), energyAmount = 20 });
        await Expect(response, HttpStatusCode.OK);
        var saved = (await response.Content.ReadFromJsonAsync<ReservationResponse>())!;
        Assert.Equal(target.Id.ToString(), saved.SlotId);
        Assert.Equal(secondStation.Id.ToString(), saved.StationId);
        Assert.Equal(target.StartTime, saved.ReservationDateTime);
        Assert.Equal("PENDING", saved.Status);
        Assert.Equal(100, (await database.Slots.Find(x => x.Id == slot.Id).SingleAsync()).AvailableCapacity);
        Assert.Equal(30, (await database.Slots.Find(x => x.Id == target.Id).SingleAsync()).AvailableCapacity);
        await Expect(await grid.PostAsJsonAsync("/api/operator/verify-qr", new { qrToken = qr.QrToken }), HttpStatusCode.BadRequest);
        await Expect(await owner.GetAsync($"/api/reservations/{booking.Id}/qr"), HttpStatusCode.Conflict);
        await Expect(await admin.PatchAsync($"/api/reservations/{booking.Id}/approve", null), HttpStatusCode.OK);
        await Expect(await owner.GetAsync($"/api/reservations/{booking.Id}/qr"), HttpStatusCode.OK);
    }

    [MongoTheory]
    [InlineData(24, 5, "OPEN", HttpStatusCode.Conflict)]
    [InlineData(11, 50, "OPEN", HttpStatusCode.Conflict)]
    [InlineData(193, 50, "OPEN", HttpStatusCode.BadRequest)]
    [InlineData(24, 50, "CLOSED", HttpStatusCode.Conflict)]
    public async Task Invalid_rescheduling_preserves_original_capacity_and_booking(int hours, int capacity, string status, HttpStatusCode expected)
    {
        var booking = await Book();
        var target = new EnergyBookingSlot { StationId = station.Id, StartTime = now.AddHours(hours),
            EndTime = now.AddHours(hours).AddMinutes(1), Capacity = capacity, AvailableCapacity = capacity,
            Status = Enum.Parse<SlotStatus>(status) };
        await database.Slots.InsertOneAsync(target);
        await Expect(await grid.PutAsJsonAsync($"/api/reservations/{booking.Id}", new { slotId = target.Id.ToString(), energyAmount = 20 }), expected);
        Assert.Equal(90, (await database.Slots.Find(x => x.Id == slot.Id).SingleAsync()).AvailableCapacity);
        Assert.Equal(capacity, (await database.Slots.Find(x => x.Id == target.Id).SingleAsync()).AvailableCapacity);
        Assert.Equal(slot.Id, (await database.Reservations.Find(x => x.Id == ObjectId.Parse(booking.Id)).SingleAsync()).SlotId);
    }

    [MongoFact]
    public async Task Failed_reschedule_write_rolls_back_both_slots()
    {
        var booking = await Book();
        var target = new EnergyBookingSlot { StationId = station.Id, StartTime = slot.StartTime.AddDays(1),
            EndTime = slot.EndTime.AddDays(1), Capacity = 50, AvailableCapacity = 50, Status = SlotStatus.OPEN };
        await database.Slots.InsertOneAsync(target);
        await database.Database.RunCommandAsync<BsonDocument>(new BsonDocument {
            { "collMod", "EnergyReservations" }, { "validator", new BsonDocument("EnergyAmount", new BsonDocument("$lt", 0)) }
        });
        await Expect(await owner.PutAsJsonAsync($"/api/reservations/{booking.Id}", new { slotId = target.Id.ToString(), energyAmount = 20 }), HttpStatusCode.InternalServerError);
        Assert.Equal(90, (await database.Slots.Find(x => x.Id == slot.Id).SingleAsync()).AvailableCapacity);
        Assert.Equal(50, (await database.Slots.Find(x => x.Id == target.Id).SingleAsync()).AvailableCapacity);
        Assert.Equal(slot.Id, (await database.Reservations.Find(x => x.Id == ObjectId.Parse(booking.Id)).SingleAsync()).SlotId);
    }

    private async Task<ReservationResponse> Book(decimal amount = 10)
    {
        // Book for Trading Integration Tests.
        var response = await owner.PostAsJsonAsync("/api/reservations", new { slotId = slot.Id.ToString(), energyAmount = amount });
        await Expect(response, HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ReservationResponse>())!;
    }
    // Expect for Trading Integration Tests.
    private static async Task Expect(HttpResponseMessage response, HttpStatusCode status) =>
        Assert.True(response.StatusCode == status, $"Expected {status}, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

    [MongoTheory]
    [InlineData(7, true)]
    [InlineData(8, false)]
    public async Task Seven_day_window_is_server_enforced(int days, bool allowed)
    {
        // Verify that seven day window is server enforced.
        await database.Slots.UpdateOneAsync(x => x.Id == slot.Id, Builders<EnergyBookingSlot>.Update
            .Set(x => x.StartTime, now.AddDays(days)).Set(x => x.EndTime, now.AddDays(days).AddMinutes(1)));
        await Expect(await owner.PostAsJsonAsync("/api/reservations", new { slotId = slot.Id.ToString(), energyAmount = 10 }),
            allowed ? HttpStatusCode.Created : HttpStatusCode.BadRequest);
    }

    [MongoTheory]
    [InlineData(12, true)]
    [InlineData(11, false)]
    public async Task Update_and_cancel_enforce_twelve_hours(int hours, bool allowed)
    {
        // Verify that update and cancel enforce twelve hours.
        var booking = await Book();
        await database.Reservations.UpdateOneAsync(x => x.Id == ObjectId.Parse(booking.Id),
            Builders<EnergyReservation>.Update.Set(x => x.ReservationDateTime, now.AddHours(hours)));
        await Expect(await owner.PutAsJsonAsync($"/api/reservations/{booking.Id}", new { energyAmount = 15 }), allowed ? HttpStatusCode.OK : HttpStatusCode.Conflict);
        await Expect(await owner.DeleteAsync($"/api/reservations/{booking.Id}"), allowed ? HttpStatusCode.OK : HttpStatusCode.Conflict);
        Assert.Equal(allowed ? 100 : 90, (await database.Slots.Find(x => x.Id == slot.Id).SingleAsync()).AvailableCapacity);
    }

    [MongoFact]
    public async Task Approval_QR_completion_and_reuse_are_authorized_and_atomic()
    {
        // Verify that approval qr completion and reuse are authorized and atomic.
        var booking = await Book();
        await Expect(await owner.GetAsync($"/api/reservations/{booking.Id}/qr"), HttpStatusCode.Conflict);
        await Expect(await grid.PatchAsync($"/api/reservations/{booking.Id}/approve", null), HttpStatusCode.Forbidden);
        await Expect(await admin.PatchAsync($"/api/reservations/{booking.Id}/approve", null), HttpStatusCode.OK);
        await Expect(await other.GetAsync($"/api/reservations/{booking.Id}/qr"), HttpStatusCode.Forbidden);
        var qr = await owner.GetFromJsonAsync<QrTokenResponse>($"/api/reservations/{booking.Id}/qr");
        await Expect(await grid.PostAsJsonAsync("/api/operator/verify-qr", new { qrToken = "invalid" }), HttpStatusCode.BadRequest);
        await Expect(await grid.PostAsJsonAsync("/api/operator/verify-qr", new { qrToken = qr!.QrToken }), HttpStatusCode.OK);
        await Expect(await owner.PostAsJsonAsync("/api/operator/complete-transfer", new { qrToken = qr.QrToken }), HttpStatusCode.Forbidden);
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => grid.PostAsJsonAsync("/api/operator/complete-transfer", new { qrToken = qr.QrToken })));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        await Expect(await grid.PostAsJsonAsync("/api/operator/verify-qr", new { qrToken = qr.QrToken }), HttpStatusCode.Conflict);
        var saved = await database.Reservations.Find(x => x.Id == ObjectId.Parse(booking.Id)).SingleAsync();
        Assert.NotNull(saved.CompletedAt); Assert.NotNull(saved.CompletedByOperatorId);
    }

    [MongoFact]
    public async Task Ownership_and_operator_administration_are_restricted()
    {
        // Verify that ownership and operator administration are restricted.
        var booking = await Book();
        await Expect(await other.GetAsync($"/api/reservations/{booking.Id}"), HttpStatusCode.Forbidden);
        await Expect(await other.DeleteAsync($"/api/reservations/{booking.Id}"), HttpStatusCode.Forbidden);
        await Expect(await grid.DeleteAsync($"/api/slots/{slot.Id}"), HttpStatusCode.Forbidden);
        await Expect(await grid.PatchAsync($"/api/stations/{station.Id}/deactivate", null), HttpStatusCode.Forbidden);
        await Expect(await grid.DeleteAsync($"/api/reservations/{booking.Id}"), HttpStatusCode.OK);
    }

    [MongoFact]
    public async Task Active_reservations_protect_station_slot_and_allocated_energy()
    {
        // Verify that active reservations protect station slot and allocated energy.
        var booking = await Book(40);
        await Expect(await admin.PatchAsync($"/api/stations/{station.Id}/deactivate", null), HttpStatusCode.Conflict);
        await Expect(await admin.DeleteAsync($"/api/slots/{slot.Id}"), HttpStatusCode.Conflict);
        await Expect(await grid.PatchAsJsonAsync($"/api/slots/{slot.Id}/availability", new { availableCapacity = 100, status = "OPEN" }), HttpStatusCode.Conflict);
        await Expect(await admin.PutAsJsonAsync($"/api/slots/{slot.Id}", new { startTime = slot.StartTime.AddHours(1), endTime = slot.EndTime.AddHours(1), capacity = 100, availableCapacity = 60, status = "OPEN" }), HttpStatusCode.Conflict);
        await Expect(await grid.PatchAsJsonAsync($"/api/slots/{slot.Id}/availability", new { availableCapacity = 60, status = "OPEN" }), HttpStatusCode.OK);
        await Expect(await admin.PatchAsync($"/api/reservations/{booking.Id}/reject", null), HttpStatusCode.OK);
        Assert.Equal(100, (await database.Slots.Find(x => x.Id == slot.Id).SingleAsync()).AvailableCapacity);
        await Expect(await admin.PatchAsync($"/api/stations/{station.Id}/deactivate", null), HttpStatusCode.OK);
    }

    [MongoFact]
    public async Task Concurrent_bookings_never_overbook_or_leak_capacity()
    {
        // Verify that concurrent bookings never overbook or leak capacity.
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => owner.PostAsJsonAsync("/api/reservations", new { slotId = slot.Id.ToString(), energyAmount = 60 })));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Created);
        Assert.All(results.Where(x => x.StatusCode != HttpStatusCode.Created), x => Assert.Equal(HttpStatusCode.Conflict, x.StatusCode));
        Assert.Equal(40, (await database.Slots.Find(x => x.Id == slot.Id).SingleAsync()).AvailableCapacity);
        Assert.Equal(1, await database.Reservations.CountDocumentsAsync(FilterDefinition<EnergyReservation>.Empty));
    }

    [MongoFact]
    public async Task Concurrent_updates_and_cancellation_keep_capacity_equal_to_ledger()
    {
        // Verify that concurrent updates and cancellation keep capacity equal to ledger.
        var booking = await Book(20);
        var results = await Task.WhenAll(owner.PutAsJsonAsync($"/api/reservations/{booking.Id}", new { energyAmount = 30 }),
            owner.PutAsJsonAsync($"/api/reservations/{booking.Id}", new { energyAmount = 50 }), owner.DeleteAsync($"/api/reservations/{booking.Id}"));
        Assert.All(results, x => Assert.Contains(x.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        var saved = await database.Reservations.Find(x => x.Id == ObjectId.Parse(booking.Id)).SingleAsync();
        var available = (await database.Slots.Find(x => x.Id == slot.Id).SingleAsync()).AvailableCapacity;
        Assert.Equal(saved.Status == ReservationStatus.CANCELLED ? 100 : 100 - saved.EnergyAmount, available);
    }

    [MongoFact]
    public async Task Unavailable_station_and_insufficient_capacity_are_rejected()
    {
        // Verify that unavailable station and insufficient capacity are rejected.
        await Expect(await owner.PostAsJsonAsync("/api/reservations", new { slotId = slot.Id.ToString(), energyAmount = 101 }), HttpStatusCode.Conflict);
        await database.Stations.UpdateOneAsync(x => x.Id == station.Id, Builders<SolarStationInfo>.Update.Set(x => x.Status, StationStatus.MAINTENANCE));
        await Expect(await owner.PostAsJsonAsync("/api/reservations", new { slotId = slot.Id.ToString(), energyAmount = 1 }), HttpStatusCode.Conflict);
        Assert.Equal(100, (await database.Slots.Find(x => x.Id == slot.Id).SingleAsync()).AvailableCapacity);
    }

    public async Task DisposeAsync()
    {
        // Release owned resources and clean up this test or request scope.
        owner?.Dispose(); other?.Dispose(); admin?.Dispose(); grid?.Dispose();
        if (database is not null) await database.Database.Client.DropDatabaseAsync(databaseName);
        if (factory is not null) await factory.DisposeAsync();
    }

    [MongoFact]
    public async Task Failed_reservation_write_rolls_back_capacity()
    {
        // Verify that failed reservation write rolls back capacity.
        await database.Database.RunCommandAsync<BsonDocument>(new BsonDocument {
            { "collMod", "EnergyReservations" }, { "validator", new BsonDocument("EnergyAmount", new BsonDocument("$lt", 0)) }
        });
        await Expect(await owner.PostAsJsonAsync("/api/reservations", new { slotId = slot.Id.ToString(), energyAmount = 10 }), HttpStatusCode.InternalServerError);
        Assert.Equal(100, (await database.Slots.Find(x => x.Id == slot.Id).SingleAsync()).AvailableCapacity);
        Assert.Equal(0, await database.Reservations.CountDocumentsAsync(FilterDefinition<EnergyReservation>.Empty));
    }

    [MongoFact]
    public async Task Station_deactivation_and_booking_cannot_both_succeed()
    {
        // Verify that station deactivation and booking cannot both succeed.
        var results = await Task.WhenAll(admin.PatchAsync($"/api/stations/{station.Id}/deactivate", null),
            owner.PostAsJsonAsync("/api/reservations", new { slotId = slot.Id.ToString(), energyAmount = 10 }));
        Assert.Single(results, x => x.IsSuccessStatusCode);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        var saved = await database.Stations.Find(x => x.Id == station.Id).SingleAsync();
        if (saved.Status == StationStatus.DEACTIVATED)
            Assert.Equal(0, await database.Reservations.CountDocumentsAsync(FilterDefinition<EnergyReservation>.Empty));
    }

    [MongoFact]
    public async Task Slot_schedule_capacity_overlap_and_operator_permissions_are_checked()
    {
        // Verify that slot schedule capacity overlap and operator permissions are checked.
        var body = new { startTime = slot.StartTime, endTime = slot.EndTime, capacity = 100, availableCapacity = 100, status = "OPEN" };
        await Expect(await admin.PostAsJsonAsync($"/api/stations/{station.Id}/slots", body), HttpStatusCode.Conflict);
        await Expect(await grid.PostAsJsonAsync($"/api/stations/{station.Id}/slots", body), HttpStatusCode.Forbidden);
        await Expect(await admin.PostAsJsonAsync($"/api/stations/{station.Id}/slots", new { startTime = slot.StartTime.AddHours(2), endTime = slot.EndTime.AddHours(2), capacity = 101, availableCapacity = 101, status = "OPEN" }), HttpStatusCode.Conflict);
        await database.Stations.UpdateOneAsync(x => x.Id == station.Id, Builders<SolarStationInfo>.Update
            .Set(x => x.OperatingSchedule, new OperatingSchedule { TimeZoneId = "UTC" }));
        await Expect(await admin.PostAsJsonAsync($"/api/stations/{station.Id}/slots", new { startTime = slot.StartTime.AddHours(2), endTime = slot.EndTime.AddHours(2), capacity = 1, availableCapacity = 1, status = "OPEN" }), HttpStatusCode.Conflict);
    }

    [MongoFact]
    public async Task Dashboard_and_search_are_scoped_to_authenticated_prosumer()
    {
        // Verify that dashboard and search are scoped to authenticated prosumer.
        var booking = await Book();
        var ownerDashboard = await owner.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/reservations/dashboard");
        var otherDashboard = await other.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/reservations/dashboard");
        Assert.Equal(1, ownerDashboard.GetProperty("activeReservations").GetInt32());
        Assert.Equal(0, otherDashboard.GetProperty("activeReservations").GetInt32());
        var search = await other.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/reservations/search?reservationCode={booking.ReservationCode}");
        Assert.Equal(0, search.GetProperty("totalCount").GetInt32());
    }

    [MongoFact]
    public async Task Station_deletion_requires_backoffice_and_preserves_references()
    {
        // Verify that station deletion requires backoffice and preserves references.
        await Expect(await grid.DeleteAsync($"/api/stations/{station.Id}"), HttpStatusCode.Forbidden);
        await Expect(await owner.DeleteAsync($"/api/stations/{station.Id}"), HttpStatusCode.Forbidden);
        await Expect(await admin.DeleteAsync($"/api/stations/{station.Id}"), HttpStatusCode.Conflict);
        Assert.True(await database.Stations.Find(x => x.Id == station.Id).AnyAsync());
        await database.Slots.DeleteOneAsync(x => x.Id == slot.Id);
        await Expect(await admin.DeleteAsync($"/api/stations/{station.Id}"), HttpStatusCode.NoContent);
        Assert.False(await database.Stations.Find(x => x.Id == station.Id).AnyAsync());
        await Expect(await admin.DeleteAsync($"/api/stations/{station.Id}"), HttpStatusCode.NotFound);
        await Expect(await admin.DeleteAsync("/api/stations/not-an-id"), HttpStatusCode.BadRequest);
    }

    [MongoFact]
    public async Task Station_deletion_preserves_cancelled_reservation_history()
    {
        // Verify that station deletion preserves cancelled reservation history.
        var booking = await Book();
        await Expect(await owner.DeleteAsync($"/api/reservations/{booking.Id}"), HttpStatusCode.OK);
        await database.Slots.DeleteOneAsync(x => x.Id == slot.Id);
        await Expect(await admin.DeleteAsync($"/api/stations/{station.Id}"), HttpStatusCode.Conflict);
        Assert.True(await database.Reservations.Find(x => x.Id == ObjectId.Parse(booking.Id)).AnyAsync());
    }

    private sealed class FixedClock(DateTime value) : TimeProvider
    {
        // Return the configured UTC clock value for deterministic business-rule checks.
        public override DateTimeOffset GetUtcNow() => new(value, TimeSpan.Zero);
    }
}
