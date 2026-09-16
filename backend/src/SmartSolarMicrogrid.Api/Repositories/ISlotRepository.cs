using MongoDB.Bson;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public interface ISlotRepository
{
    Task<decimal> CommittedCapacityAsync(ObjectId id, CancellationToken cancellationToken);
    Task<bool> HasActiveReservationsAsync(ObjectId id, CancellationToken cancellationToken);
    Task<List<EnergyBookingSlot>> ListAsync(ObjectId stationId, bool includeCancelled, CancellationToken cancellationToken);
    Task<EnergyBookingSlot?> FindAsync(ObjectId id, CancellationToken cancellationToken);
    Task<bool> HasOverlapAsync(ObjectId stationId, DateTime startTime, DateTime endTime, ObjectId? excludedId,
        CancellationToken cancellationToken);
    Task<bool> TryAdjustCapacityAsync(ObjectId id, decimal delta, bool requireOpen,
        CancellationToken cancellationToken);
    Task<(long OpenSlotCount, decimal AvailableCapacity)> GetOperationalSummaryAsync(CancellationToken cancellationToken);
    Task CreateAsync(EnergyBookingSlot slot, CancellationToken cancellationToken);
    Task<EnergyBookingSlot?> UpdateAsync(EnergyBookingSlot expected, EnergyBookingSlot replacement,
        CancellationToken cancellationToken);
}
