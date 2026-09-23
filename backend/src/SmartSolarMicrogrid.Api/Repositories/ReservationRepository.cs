/*
 * File: src/SmartSolarMicrogrid.Api/Repositories/ReservationRepository.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: MongoDB persistence and query operations for Reservation Repository.
 */
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public sealed class ReservationRepository(MongoDbContext context, MongoOperation operation) : IReservationRepository
{
    // Recent for Reservation.
    public async Task<List<EnergyReservation>> RecentAsync(string? prosumerNic, CancellationToken cancellationToken) =>
        await context.Reservations.Query(operation, prosumerNic is null ? Builders<EnergyReservation>.Filter.Empty :
            Builders<EnergyReservation>.Filter.Eq(x => x.ProsumerNIC, prosumerNic))
            .SortByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id).Limit(5).ToListAsync(cancellationToken);
    // Find for Reservation.
    public async Task<EnergyReservation?> FindAsync(ObjectId id, CancellationToken cancellationToken) =>
        await context.Reservations.Query(operation, x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    // Find By Qr Token Hash for Reservation.
    public async Task<EnergyReservation?> FindByQrTokenHashAsync(string tokenHash,
        CancellationToken cancellationToken) =>
        await context.Reservations.Query(operation, x => x.QrTokenHash == tokenHash).FirstOrDefaultAsync(cancellationToken);

    // List By Prosumer for Reservation.
    public async Task<List<EnergyReservation>> ListByProsumerAsync(string nic, CancellationToken cancellationToken) =>
        await context.Reservations.Query(operation, x => x.ProsumerNIC == nic)
            .SortByDescending(x => x.ReservationDateTime).ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

    // List By Status for Reservation.
    public async Task<List<EnergyReservation>> ListByStatusAsync(ReservationStatus status,
        CancellationToken cancellationToken) =>
        await context.Reservations.Query(operation, x => x.Status == status)
            .SortBy(x => x.ReservationDateTime).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public async Task<long> CountCompletedAsync(string? prosumerNic, DateTime from, DateTime to,
        CancellationToken cancellationToken)
    {
        // Count Completed for Reservation.
        var filter = Builders<EnergyReservation>.Filter.Eq(x => x.Status, ReservationStatus.COMPLETED) &
            Builders<EnergyReservation>.Filter.Gte(x => x.CompletedAt, from) &
            Builders<EnergyReservation>.Filter.Lt(x => x.CompletedAt, to);
        if (!string.IsNullOrWhiteSpace(prosumerNic)) filter &= Builders<EnergyReservation>.Filter.Eq(x => x.ProsumerNIC, prosumerNic);
        return await context.Reservations.Count(operation, filter, cancellationToken: cancellationToken);
    }

    public async Task<(List<EnergyReservation> Items, long TotalCount)> SearchAsync(string? prosumerNic,
        string? reservationCode, ObjectId? stationId, ReservationStatus? status, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        // Search for Reservation.
        var f = Builders<EnergyReservation>.Filter;
        var filter = f.Empty;
        if (!string.IsNullOrWhiteSpace(prosumerNic)) filter &= f.Eq(x => x.ProsumerNIC, prosumerNic);
        if (!string.IsNullOrWhiteSpace(reservationCode))
            filter &= f.Regex(x => x.ReservationCode,
                new BsonRegularExpression(Regex.Escape(reservationCode.Trim()), "i"));
        if (stationId.HasValue) filter &= f.Eq(x => x.StationId, stationId.Value);
        if (status.HasValue) filter &= f.Eq(x => x.Status, status.Value);
        if (from.HasValue) filter &= f.Gte(x => x.ReservationDateTime, from.Value);
        if (to.HasValue) filter &= f.Lt(x => x.ReservationDateTime, to.Value);
        var count = await context.Reservations.Count(operation, filter, cancellationToken: cancellationToken);
        var items = await context.Reservations.Query(operation, filter).SortBy(x => x.ReservationDateTime).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync(cancellationToken);
        return (items, count);
    }

    public async Task CreateAsync(EnergyReservation reservation, CancellationToken cancellationToken)
    {
        // Create for Reservation.
        try { await context.Reservations.Insert(operation, reservation, cancellationToken: cancellationToken); }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        { throw new InvalidOperationException("A generated reservation code was duplicated.", exception); }
    }

    public async Task<EnergyReservation?> IssueQrTokenAsync(EnergyReservation expected, string tokenHash,
        DateTime updatedAt, CancellationToken cancellationToken)
    {
        // Issue Qr Token for Reservation.
        var update = Builders<EnergyReservation>.Update
            .Set(x => x.QrTokenHash, tokenHash).Set(x => x.UpdatedAt, updatedAt);
        return await context.Reservations.Change(operation, 
            x => x.Id == expected.Id && x.Status == ReservationStatus.APPROVED,
            update, new FindOneAndUpdateOptions<EnergyReservation> { ReturnDocument = ReturnDocument.After }, cancellationToken);
    }

    public async Task<EnergyReservation?> CompleteAsync(EnergyReservation expected, ObjectId operatorId,
        DateTime completedAt, CancellationToken cancellationToken)
    {
        // Complete for Reservation.
        var update = Builders<EnergyReservation>.Update
            .Set(x => x.Status, ReservationStatus.COMPLETED)
            .Set(x => x.CompletedAt, completedAt)
            .Set(x => x.CompletedByOperatorId, operatorId)
            .Set(x => x.UpdatedAt, completedAt);
        return await context.Reservations.Change(operation, 
            x => x.Id == expected.Id && x.Status == ReservationStatus.APPROVED && x.QrTokenHash == expected.QrTokenHash,
            update, new FindOneAndUpdateOptions<EnergyReservation> { ReturnDocument = ReturnDocument.After }, cancellationToken);
    }

    public async Task<EnergyReservation?> UpdateAsync(EnergyReservation expected,
        CancellationToken cancellationToken)
    {
        // Update for Reservation.
        var update = Builders<EnergyReservation>.Update
            .Set(x => x.EnergyAmount, expected.EnergyAmount)
            .Set(x => x.SlotId, expected.SlotId)
            .Set(x => x.StationId, expected.StationId)
            .Set(x => x.ReservationDateTime, expected.ReservationDateTime)
            .Set(x => x.QrTokenHash, expected.QrTokenHash)
            .Set(x => x.Status, expected.Status)
            .Set(x => x.UpdatedAt, expected.UpdatedAt);
        return await context.Reservations.Change(operation, 
            x => x.Id == expected.Id && x.Status != ReservationStatus.CANCELLED && x.Status != ReservationStatus.COMPLETED,
            update, new FindOneAndUpdateOptions<EnergyReservation> { ReturnDocument = ReturnDocument.After }, cancellationToken);
    }
}
