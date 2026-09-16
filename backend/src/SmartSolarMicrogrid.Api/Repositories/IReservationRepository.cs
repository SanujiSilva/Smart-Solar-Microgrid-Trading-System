using MongoDB.Bson;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public interface IReservationRepository
{
    Task<EnergyReservation?> FindAsync(ObjectId id, CancellationToken cancellationToken);
    Task<List<EnergyReservation>> ListByProsumerAsync(string nic, CancellationToken cancellationToken);
    Task<List<EnergyReservation>> ListByStatusAsync(ReservationStatus status, CancellationToken cancellationToken);
    Task CreateAsync(EnergyReservation reservation, CancellationToken cancellationToken);
    Task<EnergyReservation?> UpdateAsync(EnergyReservation expected, CancellationToken cancellationToken);
}
