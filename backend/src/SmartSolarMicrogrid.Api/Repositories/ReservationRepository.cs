using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public sealed class ReservationRepository(MongoDbContext context) : IReservationRepository
{
    public async Task<EnergyReservation?> FindAsync(ObjectId id, CancellationToken cancellationToken) =>
        await context.Reservations.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task<EnergyReservation?> FindByQrTokenHashAsync(string tokenHash,
        CancellationToken cancellationToken) =>
        await context.Reservations.Find(x => x.QrTokenHash == tokenHash).FirstOrDefaultAsync(cancellationToken);

    public async Task<List<EnergyReservation>> ListByProsumerAsync(string nic, CancellationToken cancellationToken) =>
        await context.Reservations.Find(x => x.ProsumerNIC == nic)
            .SortByDescending(x => x.ReservationDateTime).ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

    public async Task<List<EnergyReservation>> ListByStatusAsync(ReservationStatus status,
        CancellationToken cancellationToken) =>
        await context.Reservations.Find(x => x.Status == status)
            .SortBy(x => x.ReservationDateTime).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public async Task CreateAsync(EnergyReservation reservation, CancellationToken cancellationToken)
    {
        try { await context.Reservations.InsertOneAsync(reservation, cancellationToken: cancellationToken); }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        { throw new InvalidOperationException("A generated reservation code was duplicated.", exception); }
    }

    public async Task<EnergyReservation?> IssueQrTokenAsync(EnergyReservation expected, string tokenHash,
        DateTime updatedAt, CancellationToken cancellationToken)
    {
        var update = Builders<EnergyReservation>.Update
            .Set(x => x.QrTokenHash, tokenHash).Set(x => x.UpdatedAt, updatedAt);
        return await context.Reservations.FindOneAndUpdateAsync(
            x => x.Id == expected.Id && x.Status == ReservationStatus.APPROVED,
            update, new FindOneAndUpdateOptions<EnergyReservation> { ReturnDocument = ReturnDocument.After }, cancellationToken);
    }

    public async Task<EnergyReservation?> CompleteAsync(EnergyReservation expected, ObjectId operatorId,
        DateTime completedAt, CancellationToken cancellationToken)
    {
        var update = Builders<EnergyReservation>.Update
            .Set(x => x.Status, ReservationStatus.COMPLETED)
            .Set(x => x.CompletedAt, completedAt)
            .Set(x => x.CompletedByOperatorId, operatorId)
            .Set(x => x.UpdatedAt, completedAt);
        return await context.Reservations.FindOneAndUpdateAsync(
            x => x.Id == expected.Id && x.Status == ReservationStatus.APPROVED && x.QrTokenHash == expected.QrTokenHash,
            update, new FindOneAndUpdateOptions<EnergyReservation> { ReturnDocument = ReturnDocument.After }, cancellationToken);
    }

    public async Task<EnergyReservation?> UpdateAsync(EnergyReservation expected,
        CancellationToken cancellationToken)
    {
        var update = Builders<EnergyReservation>.Update
            .Set(x => x.EnergyAmount, expected.EnergyAmount)
            .Set(x => x.Status, expected.Status)
            .Set(x => x.UpdatedAt, expected.UpdatedAt);
        return await context.Reservations.FindOneAndUpdateAsync(
            x => x.Id == expected.Id && x.Status != ReservationStatus.CANCELLED && x.Status != ReservationStatus.COMPLETED,
            update, new FindOneAndUpdateOptions<EnergyReservation> { ReturnDocument = ReturnDocument.After }, cancellationToken);
    }
}
