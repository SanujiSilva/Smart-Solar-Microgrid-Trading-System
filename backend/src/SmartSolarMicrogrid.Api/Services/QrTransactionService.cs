using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;
using SmartSolarMicrogrid.Api.DTOs.Reservations;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Repositories;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class QrTransactionService(IReservationRepository reservations, CurrentUser currentUser,
    TimeProvider clock)
{
    public async Task<QrTokenResponse> IssueAsync(string reservationId, CancellationToken cancellationToken)
    {
        var actor = currentUser.Require(UserRole.PROSUMER);
        var reservation = await FindAsync(reservationId, cancellationToken);
        if (!string.Equals(actor.NIC, reservation.ProsumerNIC, StringComparison.OrdinalIgnoreCase))
            throw new ApiException(403, "A prosumer can only issue a QR token for their own reservation.");
        if (reservation.Status != ReservationStatus.APPROVED)
            throw new ApiException(409, "A QR token is available only for an approved reservation.");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var hash = HashToken(token);
        var saved = await reservations.IssueQrTokenAsync(reservation, hash,
                clock.GetUtcNow().UtcDateTime, cancellationToken)
            ?? throw new ApiException(409, "The reservation changed during QR issuance. Try again.");
        return new(token, ReservationResponse.From(saved));
    }

    public async Task<QrVerificationResponse> VerifyAsync(QrTokenRequest request,
        CancellationToken cancellationToken)
    {
        currentUser.Require(UserRole.GRID_OPERATOR);
        var reservation = await FindByTokenAsync(request.QrToken, cancellationToken);
        EnsureApproved(reservation);
        return new(true, ReservationResponse.From(reservation));
    }

    public async Task<ReservationResponse> CompleteAsync(QrTokenRequest request,
        CancellationToken cancellationToken)
    {
        var actor = currentUser.Require(UserRole.GRID_OPERATOR);
        var reservation = await FindByTokenAsync(request.QrToken, cancellationToken);
        EnsureApproved(reservation);
        var operatorId = actor.Id;
        var completed = await reservations.CompleteAsync(reservation, operatorId,
                clock.GetUtcNow().UtcDateTime, cancellationToken)
            ?? throw new ApiException(409, "This QR token was already used or the reservation changed.");
        return ReservationResponse.From(completed);
    }

    private async Task<EnergyReservation> FindAsync(string id, CancellationToken cancellationToken) =>
        ObjectId.TryParse(id, out var objectId)
            ? await reservations.FindAsync(objectId, cancellationToken)
                ?? throw new ApiException(404, "The reservation was not found.")
            : throw new ApiException(400, "Reservation ID must be a valid ObjectId.");

    private async Task<EnergyReservation> FindByTokenAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ApiException(400, "A QR token is required.");
        var reservation = await reservations.FindByQrTokenHashAsync(HashToken(token.Trim()), cancellationToken);
        return reservation ?? throw new ApiException(400, "The QR token is invalid.");
    }

    private static void EnsureApproved(EnergyReservation reservation)
    {
        if (reservation.Status != ReservationStatus.APPROVED)
            throw new ApiException(409, "The QR token is no longer valid for this reservation.");
    }

    private static string HashToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
