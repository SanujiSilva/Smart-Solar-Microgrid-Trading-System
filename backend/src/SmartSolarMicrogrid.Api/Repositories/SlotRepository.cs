using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public sealed class SlotRepository(MongoDbContext context) : ISlotRepository
{
    public async Task<List<EnergyBookingSlot>> ListAsync(ObjectId stationId, bool includeCancelled,
        CancellationToken cancellationToken)
    {
        var filter = Builders<EnergyBookingSlot>.Filter.Eq(x => x.StationId, stationId);
        if (!includeCancelled) filter &= Builders<EnergyBookingSlot>.Filter.Ne(x => x.Status, SlotStatus.CANCELLED);
        return await context.Slots.Find(filter).SortBy(x => x.StartTime).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<EnergyBookingSlot?> FindAsync(ObjectId id, CancellationToken cancellationToken) =>
        await context.Slots.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> HasOverlapAsync(ObjectId stationId, DateTime startTime, DateTime endTime,
        ObjectId? excludedId, CancellationToken cancellationToken)
    {
        var filter = Builders<EnergyBookingSlot>.Filter.Eq(x => x.StationId, stationId) &
            Builders<EnergyBookingSlot>.Filter.Ne(x => x.Status, SlotStatus.CANCELLED) &
            Builders<EnergyBookingSlot>.Filter.Lt(x => x.StartTime, endTime) &
            Builders<EnergyBookingSlot>.Filter.Gt(x => x.EndTime, startTime);
        if (excludedId.HasValue) filter &= Builders<EnergyBookingSlot>.Filter.Ne(x => x.Id, excludedId.Value);
        return await context.Slots.Find(filter).Limit(1).AnyAsync(cancellationToken);
    }

    public async Task<bool> TryAdjustCapacityAsync(ObjectId id, decimal delta, bool requireOpen,
        CancellationToken cancellationToken)
    {
        var filter = Builders<EnergyBookingSlot>.Filter.Eq(x => x.Id, id) &
            Builders<EnergyBookingSlot>.Filter.Ne(x => x.Status, SlotStatus.CANCELLED);
        if (delta < 0) filter &= Builders<EnergyBookingSlot>.Filter.Gte(x => x.AvailableCapacity, -delta);
        if (requireOpen) filter &= Builders<EnergyBookingSlot>.Filter.Eq(x => x.Status, SlotStatus.OPEN);
        var update = Builders<EnergyBookingSlot>.Update
            .Inc(x => x.AvailableCapacity, delta).Set(x => x.UpdatedAt, DateTime.UtcNow);
        return await context.Slots.FindOneAndUpdateAsync(filter, update,
            new FindOneAndUpdateOptions<EnergyBookingSlot> { ReturnDocument = ReturnDocument.After }, cancellationToken) is not null;
    }

    public async Task CreateAsync(EnergyBookingSlot slot, CancellationToken cancellationToken) =>
        await context.Slots.InsertOneAsync(slot, cancellationToken: cancellationToken);

    public async Task<EnergyBookingSlot?> UpdateAsync(EnergyBookingSlot expected, EnergyBookingSlot replacement,
        CancellationToken cancellationToken)
    {
        var update = Builders<EnergyBookingSlot>.Update
            .Set(x => x.StartTime, replacement.StartTime)
            .Set(x => x.EndTime, replacement.EndTime)
            .Set(x => x.Capacity, replacement.Capacity)
            .Set(x => x.AvailableCapacity, replacement.AvailableCapacity)
            .Set(x => x.Status, replacement.Status)
            .Set(x => x.UpdatedAt, replacement.UpdatedAt);
        return await context.Slots.FindOneAndUpdateAsync(x => x.Id == expected.Id, update,
            new FindOneAndUpdateOptions<EnergyBookingSlot> { ReturnDocument = ReturnDocument.After }, cancellationToken);
    }
}
