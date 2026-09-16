using MongoDB.Bson;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public interface ISlotRepository
{
    Task<List<EnergyBookingSlot>> ListAsync(ObjectId stationId, bool includeCancelled, CancellationToken cancellationToken);
    Task<EnergyBookingSlot?> FindAsync(ObjectId id, CancellationToken cancellationToken);
    Task<bool> HasOverlapAsync(ObjectId stationId, DateTime startTime, DateTime endTime, ObjectId? excludedId,
        CancellationToken cancellationToken);
    Task CreateAsync(EnergyBookingSlot slot, CancellationToken cancellationToken);
    Task<EnergyBookingSlot?> UpdateAsync(EnergyBookingSlot expected, EnergyBookingSlot replacement,
        CancellationToken cancellationToken);
}
