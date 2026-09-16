using MongoDB.Bson;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public interface IStationRepository
{
    Task<SolarStationInfo?> FindAsync(ObjectId id, CancellationToken cancellationToken);
    Task CreateAsync(SolarStationInfo station, CancellationToken cancellationToken);
    Task<(List<SolarStationInfo> Items, long TotalCount)> SearchAsync(StationStatus? status, string? search,
        int page, int pageSize, CancellationToken cancellationToken);
    Task<List<SolarStationInfo>> NearbyAsync(double latitude, double longitude, double radiusKm, int limit, CancellationToken cancellationToken);
    Task<bool> HasUnresolvedReservationsAsync(ObjectId stationId, CancellationToken cancellationToken);
    Task<SolarStationInfo?> UpdateAsync(SolarStationInfo expected, SolarStationInfo replacement, CancellationToken cancellationToken);
}
