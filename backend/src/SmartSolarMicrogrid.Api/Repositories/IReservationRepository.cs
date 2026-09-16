using MongoDB.Bson;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public interface IReservationRepository
{
    Task<EnergyReservation?> FindAsync(ObjectId id, CancellationToken cancellationToken);
    Task<EnergyReservation?> FindByQrTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<List<EnergyReservation>> ListByProsumerAsync(string nic, CancellationToken cancellationToken);
    Task<List<EnergyReservation>> ListByStatusAsync(ReservationStatus status, CancellationToken cancellationToken);
    Task<long> CountCompletedAsync(string? prosumerNic, DateTime from, DateTime to,
        CancellationToken cancellationToken);
    Task<(List<EnergyReservation> Items, long TotalCount)> SearchAsync(string? prosumerNic,
        string? reservationCode, ObjectId? stationId, ReservationStatus? status, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken cancellationToken);
    Task CreateAsync(EnergyReservation reservation, CancellationToken cancellationToken);
    Task<EnergyReservation?> IssueQrTokenAsync(EnergyReservation expected, string tokenHash,
        DateTime updatedAt, CancellationToken cancellationToken);
    Task<EnergyReservation?> CompleteAsync(EnergyReservation expected, ObjectId operatorId,
        DateTime completedAt, CancellationToken cancellationToken);
    Task<EnergyReservation?> UpdateAsync(EnergyReservation expected, CancellationToken cancellationToken);
}
