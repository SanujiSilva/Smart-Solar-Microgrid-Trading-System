/*
 * File: src/SmartSolarMicrogrid.Api/Repositories/MongoDbContext.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: MongoDB persistence and query operations for Mongo Db Context.
 */
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public sealed class MongoDbContext(IMongoClient client, IOptions<MongoDbSettings> options)
{
    public IMongoDatabase Database { get; } = client.GetDatabase(options.Value.DatabaseName);
    public IMongoCollection<User> Users => Database.GetCollection<User>("UsersByIdentity");
    public IMongoCollection<SolarStationInfo> Stations => Database.GetCollection<SolarStationInfo>("SolarStationInfo");
    public IMongoCollection<EnergyBookingSlot> Slots => Database.GetCollection<EnergyBookingSlot>("EnergyBookingSlots");
    public IMongoCollection<EnergyReservation> Reservations => Database.GetCollection<EnergyReservation>("EnergyReservations");
}
