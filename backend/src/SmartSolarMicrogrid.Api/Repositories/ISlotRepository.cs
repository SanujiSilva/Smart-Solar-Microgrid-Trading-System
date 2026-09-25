/*
 * File: src/SmartSolarMicrogrid.Api/Repositories/ISlotRepository.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: MongoDB persistence and query operations for ISlot Repository.
 */
using MongoDB.Bson;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public interface ISlotRepository
{
    // Committed Capacity for ISlot.
    Task<decimal> CommittedCapacityAsync(ObjectId id, CancellationToken cancellationToken);
    // Has Active Reservations for ISlot.
    Task<bool> HasActiveReservationsAsync(ObjectId id, CancellationToken cancellationToken);
    // List for ISlot.
    Task<List<EnergyBookingSlot>> ListAsync(ObjectId stationId, bool includeCancelled, CancellationToken cancellationToken);
    // Find for ISlot.
    Task<EnergyBookingSlot?> FindAsync(ObjectId id, CancellationToken cancellationToken);
    // Has Overlap for ISlot.
    Task<bool> HasOverlapAsync(ObjectId stationId, DateTime startTime, DateTime endTime, ObjectId? excludedId,
        CancellationToken cancellationToken);
    // Try Adjust Capacity for ISlot.
    Task<bool> TryAdjustCapacityAsync(ObjectId id, decimal delta, bool requireOpen,
        CancellationToken cancellationToken);
    // Get Operational Summary for ISlot.
    Task<(long OpenSlotCount, decimal AvailableCapacity)> GetOperationalSummaryAsync(CancellationToken cancellationToken);
    // Create for ISlot.
    Task CreateAsync(EnergyBookingSlot slot, CancellationToken cancellationToken);
    // Update for ISlot.
    Task<EnergyBookingSlot?> UpdateAsync(EnergyBookingSlot expected, EnergyBookingSlot replacement,
        CancellationToken cancellationToken);
}
