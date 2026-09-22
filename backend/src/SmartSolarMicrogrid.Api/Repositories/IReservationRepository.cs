/*
 * File: src/SmartSolarMicrogrid.Api/Repositories/IReservationRepository.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: MongoDB persistence and query operations for IReservation Repository.
 */
using MongoDB.Bson;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public interface IReservationRepository
{
    // Recent for IReservation.
    Task<List<EnergyReservation>> RecentAsync(string? prosumerNic, CancellationToken cancellationToken);
    // Find for IReservation.
    Task<EnergyReservation?> FindAsync(ObjectId id, CancellationToken cancellationToken);
    // Find By Qr Token Hash for IReservation.
    Task<EnergyReservation?> FindByQrTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    // List By Prosumer for IReservation.
    Task<List<EnergyReservation>> ListByProsumerAsync(string nic, CancellationToken cancellationToken);
    // List By Status for IReservation.
    Task<List<EnergyReservation>> ListByStatusAsync(ReservationStatus status, CancellationToken cancellationToken);
    // Count Completed for IReservation.
    Task<long> CountCompletedAsync(string? prosumerNic, DateTime from, DateTime to,
        CancellationToken cancellationToken);
    // Search for IReservation.
    Task<(List<EnergyReservation> Items, long TotalCount)> SearchAsync(string? prosumerNic,
        string? reservationCode, ObjectId? stationId, ReservationStatus? status, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken cancellationToken);
    // Create for IReservation.
    Task CreateAsync(EnergyReservation reservation, CancellationToken cancellationToken);
    // Issue Qr Token for IReservation.
    Task<EnergyReservation?> IssueQrTokenAsync(EnergyReservation expected, string tokenHash,
        DateTime updatedAt, CancellationToken cancellationToken);
    // Complete for IReservation.
    Task<EnergyReservation?> CompleteAsync(EnergyReservation expected, ObjectId operatorId,
        DateTime completedAt, CancellationToken cancellationToken);
    // Update for IReservation.
    Task<EnergyReservation?> UpdateAsync(EnergyReservation expected, CancellationToken cancellationToken);
}
