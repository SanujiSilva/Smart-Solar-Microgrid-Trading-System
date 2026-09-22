/*
 * File: src/SmartSolarMicrogrid.Api/Repositories/IStationRepository.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: MongoDB persistence and query operations for IStation Repository.
 */
using MongoDB.Bson;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public interface IStationRepository
{
    // Find for IStation.
    Task<SolarStationInfo?> FindAsync(ObjectId id, CancellationToken cancellationToken);
    // Check both slots and reservation history before allowing physical station deletion.
    Task<bool> HasReferencesAsync(ObjectId id, CancellationToken cancellationToken);
    // Delete for IStation.
    Task<bool> DeleteAsync(ObjectId id, CancellationToken cancellationToken);
    // Create for IStation.
    Task CreateAsync(SolarStationInfo station, CancellationToken cancellationToken);
    // Search for IStation.
    Task<(List<SolarStationInfo> Items, long TotalCount)> SearchAsync(StationStatus? status, string? search,
        int page, int pageSize, CancellationToken cancellationToken);
    // Nearby for IStation.
    Task<List<SolarStationInfo>> NearbyAsync(double latitude, double longitude, double radiusKm, int limit, CancellationToken cancellationToken);
    // Has Unresolved Reservations for IStation.
    Task<bool> HasUnresolvedReservationsAsync(ObjectId stationId, CancellationToken cancellationToken);
    // Update for IStation.
    Task<SolarStationInfo?> UpdateAsync(SolarStationInfo expected, SolarStationInfo replacement, CancellationToken cancellationToken);
}
