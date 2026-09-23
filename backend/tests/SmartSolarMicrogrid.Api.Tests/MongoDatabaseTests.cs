/*
 * File: tests/SmartSolarMicrogrid.Api.Tests/MongoDatabaseTests.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Automated verification and test support for Mongo Database Tests.
 */
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
        // Initialize Mongo Database Tests dependencies and configuration.
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SMARTSOLAR_TEST_MONGODB_URI")))
            Skip = "Set SMARTSOLAR_TEST_MONGODB_URI to run real MongoDB tests.";
    }
}

public sealed class MongoDatabaseTests : IAsyncLifetime
{
    private readonly string databaseName = "ss_test_" + Guid.NewGuid().ToString("N")[..24];
    private MongoClient client = null!;
    private MongoDbContext context = null!;
    private MongoIndexInitializer indexes = null!;

    public async Task InitializeAsync()
    {
        // Create an isolated test database and initialize the API test clients.
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
        // Release owned resources and clean up this test or request scope.
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
        // Verify that initialization creates four collections and is repeatable.
        await indexes.InitializeAsync();
        using var cursor = await context.Database.ListCollectionNamesAsync();
        var collections = await cursor.ToListAsync();
        Assert.Equal(new[] { "EnergyBookingSlots", "EnergyReservations", "SolarStationInfo", "UsersByIdentity" },
            collections.OrderBy(x => x));
        using var reservationIndexes = await context.Reservations.Indexes.ListAsync();
        var definitions = await reservationIndexes.ToListAsync();
        Assert.Equal(5, definitions.Count); // Four application indexes plus _id.
        Assert.Contains(definitions, x => x["name"] == "ux_reservations_code" && x["unique"].AsBoolean);
    }

    [MongoFact]
    public async Task NIC_unique_index_rejects_normalized_duplicates_but_allows_staff_without_NIC()
    {
        // Verify that nic unique index rejects normalized duplicates but allows staff without nic.
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
    public async Task Legacy_identity_migration_preserves_source_credentials_and_references_and_is_repeatable()
    {
        var user = MongoModelTests.NewUser("991234567V");
        var staff = MongoModelTests.NewUser(null); staff.Role = UserRole.GRID_OPERATOR;
        var source = context.Database.GetCollection<BsonDocument>("Users");
        foreach (var item in new[] { user, staff })
        {
            var document = item.ToBsonDocument();
            document["_id"] = item.Id; document.Remove("UserId");
            await source.InsertOneAsync(document);
        }
        var migration = new UserIdentityMigration(context);
        await migration.MigrateAsync();
        await migration.MigrateAsync();
        var migrated = await context.Users.Find(x => x.Id == user.Id).SingleAsync();
        Assert.Equal(user.NIC, migrated.PrimaryKey.AsString);
        Assert.Equal(user.PasswordHash, migrated.PasswordHash);
        Assert.Equal(staff.Id, (await context.Users.Find(x => x.Id == staff.Id).SingleAsync()).PrimaryKey.AsObjectId);
        Assert.Equal(2, await context.Users.CountDocumentsAsync(FilterDefinition<User>.Empty));
        Assert.Equal(2, await source.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.False((await source.Find(new BsonDocument("_id", user.Id)).SingleAsync()).Contains("UserId"));
    }

    [MongoFact]
    public async Task Invalid_legacy_NIC_rolls_back_entire_identity_migration()
    {
        var source = context.Database.GetCollection<BsonDocument>("Users");
        var valid = MongoModelTests.NewUser("991234567V").ToBsonDocument();
        valid["_id"] = valid["UserId"]; valid.Remove("UserId");
        var invalid = MongoModelTests.NewUser("991234568V").ToBsonDocument();
        invalid["_id"] = invalid["UserId"]; invalid.Remove("UserId"); invalid["NIC"] = "invalid";
        await source.InsertManyAsync([valid, invalid]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new UserIdentityMigration(context).MigrateAsync());
        Assert.Equal(0, await context.Users.CountDocumentsAsync(FilterDefinition<User>.Empty));
        Assert.Equal(2, await source.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    [MongoFact]
    public async Task Station_and_reservation_codes_are_unique()
    {
        // Verify that station and reservation codes are unique.
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
        // Verify that geo index and decimal slot roundtrip work on the server.
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
