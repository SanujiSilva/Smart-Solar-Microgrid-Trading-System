using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Repositories;
using Xunit;

namespace SmartSolarMicrogrid.Api.Tests;

// Explicitly skipped without a test URI. Never silently substitute mocks for database tests.
public sealed class MongoFactAttribute : FactAttribute
{
    public MongoFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SMARTSOLAR_TEST_MONGODB_URI")))
            Skip = "Set SMARTSOLAR_TEST_MONGODB_URI to run real MongoDB tests.";
    }
}

public sealed class MongoDatabaseTests : IAsyncLifetime
{
    private readonly string databaseName = "smartsolar_tests_" + Guid.NewGuid().ToString("N");
    private MongoClient client = null!;
    private MongoDbContext context = null!;
    private MongoIndexInitializer indexes = null!;

    public async Task InitializeAsync()
    {
        var settings = MongoClientSettings.FromConnectionString(
            Environment.GetEnvironmentVariable("SMARTSOLAR_TEST_MONGODB_URI"));
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        client = new MongoClient(settings);
        context = new MongoDbContext(client, Options.Create(new MongoDbSettings { DatabaseName = databaseName }));
        indexes = new MongoIndexInitializer(context);
        await indexes.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (client is not null)
        {
            try
            {
                // Only the randomly named database owned by this test is removed.
                await client.DropDatabaseAsync(databaseName);
            }
            finally { client.Dispose(); }
        }
    }

    [MongoFact]
    public async Task Initialization_creates_four_collections_and_is_repeatable()
    {
        await indexes.InitializeAsync();
        using var cursor = await context.Database.ListCollectionNamesAsync();
        var collections = await cursor.ToListAsync();
        Assert.Equal(new[] { "EnergyBookingSlots", "EnergyReservations", "SolarStationInfo", "Users" },
            collections.OrderBy(x => x));
        using var reservationIndexes = await context.Reservations.Indexes.ListAsync();
        var definitions = await reservationIndexes.ToListAsync();
        Assert.Equal(5, definitions.Count); // Four application indexes plus _id.
        Assert.Contains(definitions, x => x["name"] == "ux_reservations_code" && x["unique"].AsBoolean);
    }

    [MongoFact]
    public async Task NIC_unique_index_rejects_normalized_duplicates_but_allows_staff_without_NIC()
    {
        await context.Users.InsertOneAsync(MongoModelTests.NewUser(" 991234567v "));
        var error = await Assert.ThrowsAsync<MongoWriteException>(() =>
            context.Users.InsertOneAsync(MongoModelTests.NewUser("991234567V")));
        Assert.Equal(ServerErrorCategory.DuplicateKey, error.WriteError.Category);
        var first = MongoModelTests.NewUser(null); first.Role = UserRole.BACKOFFICE;
        var second = MongoModelTests.NewUser(null); second.Role = UserRole.GRID_OPERATOR;
        await context.Users.InsertManyAsync([first, second]);
        Assert.Equal(3, await context.Users.CountDocumentsAsync(FilterDefinition<User>.Empty));
    }

    [MongoFact]
    public async Task Station_and_reservation_codes_are_unique()
    {
        await context.Stations.InsertOneAsync(MongoModelTests.NewStation("TEST-S1"));
        var stationError = await Assert.ThrowsAsync<MongoWriteException>(() =>
            context.Stations.InsertOneAsync(MongoModelTests.NewStation("TEST-S1")));
        Assert.Equal(ServerErrorCategory.DuplicateKey, stationError.WriteError.Category);
        EnergyReservation Reservation() => new()
        {
            ReservationCode = "TEST-R1", ProsumerNIC = "991234567V",
            StationId = ObjectId.GenerateNewId(), SlotId = ObjectId.GenerateNewId(),
            ReservationDateTime = DateTime.UtcNow.AddDays(1), EnergyAmount = 3.25m
        };
        await context.Reservations.InsertOneAsync(Reservation());
        var reservationError = await Assert.ThrowsAsync<MongoWriteException>(() =>
            context.Reservations.InsertOneAsync(Reservation()));
        Assert.Equal(ServerErrorCategory.DuplicateKey, reservationError.WriteError.Category);
    }

    [MongoFact]
    public async Task Geo_index_and_decimal_slot_roundtrip_work_on_the_server()
    {
        var station = MongoModelTests.NewStation("TEST-S1");
        await context.Stations.InsertOneAsync(station);
        var filter = Builders<SolarStationInfo>.Filter.NearSphere(x => x.Location,
            new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new(79.8612, 6.9271)), maxDistance: 1000);
        Assert.Equal(station.Id, (await context.Stations.Find(filter).SingleAsync()).Id);
        var slot = new EnergyBookingSlot
        {
            StationId = station.Id, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1),
            Capacity = 12.123456789012345m, AvailableCapacity = 11.987654321098765m
        };
        await context.Slots.InsertOneAsync(slot);
        var stored = await context.Slots.Find(x => x.Id == slot.Id).SingleAsync();
        Assert.Equal(slot.AvailableCapacity, stored.AvailableCapacity);
        Assert.Equal(station.Id, stored.StationId);
        Assert.Equal(DateTimeKind.Utc, stored.StartTime.Kind);
    }

    [MongoFact]
    public async Task Real_API_startup_initializes_database_and_reports_ready()
    {
        // Exercise startup against an absent database, not only existing fixture indexes.
        await client.DropDatabaseAsync(databaseName);
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["MongoDb:ConnectionString"] = Environment.GetEnvironmentVariable("SMARTSOLAR_TEST_MONGODB_URI"),
                    ["MongoDb:DatabaseName"] = databaseName,
                    ["Jwt:SigningKey"] = AuthTestSettings.SigningKey
                }));
        });
        using var http = factory.CreateClient();
        using var response = await http.GetAsync("/api/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", await response.Content.ReadAsStringAsync());
        using var collections = await context.Database.ListCollectionNamesAsync();
        Assert.Equal(4, (await collections.ToListAsync()).Count);
    }
}
