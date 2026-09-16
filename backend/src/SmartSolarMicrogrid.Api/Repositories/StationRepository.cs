using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public sealed class StationRepository(MongoDbContext context) : IStationRepository
{
    public async Task<SolarStationInfo?> FindAsync(ObjectId id, CancellationToken cancellationToken) =>
        await context.Stations.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task CreateAsync(SolarStationInfo station, CancellationToken cancellationToken)
    {
        try { await context.Stations.InsertOneAsync(station, cancellationToken: cancellationToken); }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        { throw new ApiException(409, "A station with this station code already exists."); }
    }

    public async Task<(List<SolarStationInfo> Items, long TotalCount)> SearchAsync(StationStatus? status, string? search,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var f = Builders<SolarStationInfo>.Filter;
        var filter = f.Empty;
        if (status.HasValue) filter &= f.Eq(x => x.Status, status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = new BsonRegularExpression(Regex.Escape(search.Trim()), "i");
            filter &= f.Or(f.Regex(x => x.StationCode, text), f.Regex(x => x.Name, text), f.Regex(x => x.Address, text));
        }
        var count = await context.Stations.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var items = await context.Stations.Find(filter).SortBy(x => x.StationCode).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync(cancellationToken);
        return (items, count);
    }

    public async Task<List<SolarStationInfo>> NearbyAsync(double latitude, double longitude, double radiusKm, int limit, CancellationToken cancellationToken)
    {
        var f = Builders<SolarStationInfo>.Filter;
        var filter = f.Eq(x => x.Status, StationStatus.ACTIVE) & f.NearSphere(x => x.Location,
            new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new(longitude, latitude)), maxDistance: radiusKm * 1000);
        // $nearSphere returns nearest first using the existing 2dsphere index.
        return await context.Stations.Find(filter).Limit(limit).ToListAsync(cancellationToken);
    }

    public async Task<bool> HasUnresolvedReservationsAsync(ObjectId stationId, CancellationToken cancellationToken) =>
        await context.Reservations.Find(x => x.StationId == stationId &&
            (x.Status == ReservationStatus.PENDING || x.Status == ReservationStatus.APPROVED)).AnyAsync(cancellationToken);

    public async Task<SolarStationInfo?> UpdateAsync(SolarStationInfo expected, SolarStationInfo replacement, CancellationToken cancellationToken)
    {
        var f = Builders<SolarStationInfo>.Filter;
        var revision = f.Eq(x => x.Revision, expected.Revision);
        if (expected.Revision == 0) revision |= f.Exists(x => x.Revision, false);
        var update = Builders<SolarStationInfo>.Update
            .Set(x => x.Name, replacement.Name).Set(x => x.Address, replacement.Address)
            .Set(x => x.Location, replacement.Location).Set(x => x.CapacityKWh, replacement.CapacityKWh)
            .Set(x => x.AvailableBatterySlots, replacement.AvailableBatterySlots).Set(x => x.Status, replacement.Status)
            .Set(x => x.OperatingSchedule, replacement.OperatingSchedule).Set(x => x.UpdatedAt, replacement.UpdatedAt)
            .Inc(x => x.Revision, 1);
        return await context.Stations.FindOneAndUpdateAsync(f.Eq(x => x.Id, expected.Id) & revision, update,
            new FindOneAndUpdateOptions<SolarStationInfo> { ReturnDocument = ReturnDocument.After }, cancellationToken);
    }
}
