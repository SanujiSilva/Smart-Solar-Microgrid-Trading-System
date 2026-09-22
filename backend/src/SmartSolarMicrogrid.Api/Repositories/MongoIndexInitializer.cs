/*
 * File: src/SmartSolarMicrogrid.Api/Repositories/MongoIndexInitializer.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: MongoDB persistence and query operations for Mongo Index Initializer.
 */
using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public sealed class MongoIndexInitializer(MongoDbContext context)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Creating indexes also creates empty collections. Repeating identical named definitions is safe.
        await context.Users.Indexes.CreateManyAsync([
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.NIC),
                new CreateIndexOptions<User>
                {
                    Name = "ux_users_nic", Unique = true,
                    PartialFilterExpression = Builders<User>.Filter.Type(x => x.NIC, BsonType.String)
                }),
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.Email),
                new CreateIndexOptions { Name = "ux_users_email", Unique = true, Collation = UserRepository.EmailCollation })
        ], cancellationToken);

        await context.Stations.Indexes.CreateManyAsync([
            new CreateIndexModel<SolarStationInfo>(Builders<SolarStationInfo>.IndexKeys.Ascending(x => x.StationCode),
                new CreateIndexOptions { Name = "ux_stations_code", Unique = true }),
            new CreateIndexModel<SolarStationInfo>(Builders<SolarStationInfo>.IndexKeys.Geo2DSphere(x => x.Location),
                new CreateIndexOptions { Name = "ix_stations_location" })
        ], cancellationToken);

        await context.Slots.Indexes.CreateManyAsync([
            new CreateIndexModel<EnergyBookingSlot>(Builders<EnergyBookingSlot>.IndexKeys
                .Ascending(x => x.StationId).Ascending(x => x.StartTime),
                new CreateIndexOptions { Name = "ix_slots_station_start" })
        ], cancellationToken);

        await context.Reservations.Indexes.CreateManyAsync([
            new CreateIndexModel<EnergyReservation>(Builders<EnergyReservation>.IndexKeys.Ascending(x => x.ReservationCode),
                new CreateIndexOptions { Name = "ux_reservations_code", Unique = true }),
            new CreateIndexModel<EnergyReservation>(Builders<EnergyReservation>.IndexKeys
                .Ascending(x => x.ProsumerNIC).Descending(x => x.ReservationDateTime),
                new CreateIndexOptions { Name = "ix_reservations_prosumer_date" }),
            new CreateIndexModel<EnergyReservation>(Builders<EnergyReservation>.IndexKeys
                .Ascending(x => x.StationId).Ascending(x => x.Status).Ascending(x => x.ReservationDateTime),
                new CreateIndexOptions { Name = "ix_reservations_station_status_date" }),
            new CreateIndexModel<EnergyReservation>(Builders<EnergyReservation>.IndexKeys
                .Ascending(x => x.SlotId).Ascending(x => x.Status),
                new CreateIndexOptions { Name = "ix_reservations_slot_status" })
        ], cancellationToken);
    }
}
