using System.Security.Cryptography;
using MongoDB.Bson;
using SmartSolarMicrogrid.Api.DTOs.Reservations;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Repositories;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class ReservationService(IReservationRepository reservations, ISlotRepository slots,
    IStationRepository stations, CurrentUser currentUser, TimeProvider clock)
{
    public async Task<ReservationResponse> CreateAsync(CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var actor = currentUser.Require(UserRole.PROSUMER);
        if (actor.Status != UserStatus.ACTIVE)
            throw new ApiException(403, "Only active prosumers can create reservations.");
        if (!ObjectId.TryParse(request.SlotId, out var slotId))
            throw new ApiException(400, "Slot ID must be a valid ObjectId.");
        if (request.EnergyAmount is null || request.EnergyAmount <= 0)
            throw new ApiException(400, "Energy amount must be greater than zero.");

        var slot = await slots.FindAsync(slotId, cancellationToken)
            ?? throw new ApiException(404, "The slot was not found.");
        var station = await stations.FindAsync(slot.StationId, cancellationToken)
            ?? throw new ApiException(404, "The station was not found.");
        if (station.Status == StationStatus.DEACTIVATED)
            throw new ApiException(409, "Reservations cannot be created for a deactivated station.");
        ValidateBookingWindow(slot.StartTime, clock.GetUtcNow().UtcDateTime);

        var amount = request.EnergyAmount.Value;
        if (!await slots.TryAdjustCapacityAsync(slot.Id, -amount, true, cancellationToken))
            throw new ApiException(409, "The slot is closed or does not have enough available capacity.");

        var now = clock.GetUtcNow().UtcDateTime;
        var reservation = new EnergyReservation
        {
            ReservationCode = CreateReservationCode(), ProsumerNIC = actor.NIC
                ?? throw new ApiException(409, "The authenticated account has no prosumer NIC."),
            StationId = slot.StationId, SlotId = slot.Id, EnergyAmount = amount,
            ReservationDateTime = slot.StartTime, Status = ReservationStatus.PENDING,
            CreatedAt = now, UpdatedAt = now
        };
        try
        {
            await reservations.CreateAsync(reservation, cancellationToken);
        }
        catch
        {
            await slots.TryAdjustCapacityAsync(slot.Id, amount, false, CancellationToken.None);
            throw;
        }
        return ReservationResponse.From(reservation);
    }

    public async Task<ReservationResponse> GetAsync(string id, CancellationToken cancellationToken)
    {
        var reservation = await FindAsync(id, cancellationToken);
        EnsureVisible(reservation);
        return ReservationResponse.From(reservation);
    }

    public async Task<ReservationListResponse> MyAsync(CancellationToken cancellationToken)
    {
        var actor = currentUser.Require(UserRole.PROSUMER);
        var items = await reservations.ListByProsumerAsync(actor.NIC
            ?? throw new ApiException(409, "The authenticated account has no prosumer NIC."), cancellationToken);
        return new(items.Select(ReservationResponse.From).ToList());
    }

    public Task<ReservationListResponse> PendingAsync(CancellationToken cancellationToken) =>
        ListByStatusAsync(ReservationStatus.PENDING, cancellationToken);

    public async Task<ReservationListResponse> HistoryAsync(CancellationToken cancellationToken)
    {
        var actor = currentUser.Require(UserRole.PROSUMER);
        var items = await reservations.ListByProsumerAsync(actor.NIC
            ?? throw new ApiException(409, "The authenticated account has no prosumer NIC."), cancellationToken);
        return new(items.Where(x => x.Status is ReservationStatus.CANCELLED or ReservationStatus.COMPLETED or ReservationStatus.REJECTED)
            .Select(ReservationResponse.From).ToList());
    }

    public async Task<ReservationResponse> UpdateAsync(string id, UpdateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var actor = currentUser.Require(UserRole.PROSUMER);
        var reservation = await FindAsync(id, cancellationToken);
        EnsureOwner(actor, reservation);
        EnsureEditable(reservation);
        EnsureNotice(reservation, clock.GetUtcNow().UtcDateTime, "updated");
        if (request.EnergyAmount is null || request.EnergyAmount <= 0)
            throw new ApiException(400, "Energy amount must be greater than zero.");
        var amount = request.EnergyAmount.Value;
        var delta = reservation.EnergyAmount - amount;
        if (delta != 0 && !await slots.TryAdjustCapacityAsync(reservation.SlotId, delta, delta < 0, cancellationToken))
            throw new ApiException(409, "The slot does not have enough available capacity for this update.");

        reservation.EnergyAmount = amount;
        reservation.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        try
        {
            var saved = await reservations.UpdateAsync(reservation, cancellationToken)
                ?? throw new ApiException(409, "The reservation changed during this request. Reload and try again.");
            return ReservationResponse.From(saved);
        }
        catch
        {
            if (delta != 0) await slots.TryAdjustCapacityAsync(reservation.SlotId, -delta, false, CancellationToken.None);
            throw;
        }
    }

    public async Task<ReservationResponse> CancelAsync(string id, CancellationToken cancellationToken)
    {
        var actor = currentUser.Require(UserRole.PROSUMER);
        var reservation = await FindAsync(id, cancellationToken);
        EnsureOwner(actor, reservation);
        EnsureEditable(reservation);
        EnsureNotice(reservation, clock.GetUtcNow().UtcDateTime, "cancelled");
        if (!await slots.TryAdjustCapacityAsync(reservation.SlotId, reservation.EnergyAmount, false, cancellationToken))
            throw new ApiException(409, "The slot cannot receive the released capacity.");

        reservation.Status = ReservationStatus.CANCELLED;
        reservation.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        try
        {
            var saved = await reservations.UpdateAsync(reservation, cancellationToken)
                ?? throw new ApiException(409, "The reservation changed during this request. Reload and try again.");
            return ReservationResponse.From(saved);
        }
        catch
        {
            await slots.TryAdjustCapacityAsync(reservation.SlotId, -reservation.EnergyAmount, false, CancellationToken.None);
            throw;
        }
    }

    private async Task<ReservationListResponse> ListByStatusAsync(ReservationStatus status,
        CancellationToken cancellationToken)
    {
        var actor = currentUser.Get();
        if (actor.Role is not (UserRole.BACKOFFICE or UserRole.GRID_OPERATOR))
            throw new ApiException(403, "Staff access is required.");
        var items = await reservations.ListByStatusAsync(status, cancellationToken);
        return new(items.Select(ReservationResponse.From).ToList());
    }

    private async Task<EnergyReservation> FindAsync(string id, CancellationToken cancellationToken) =>
        ObjectId.TryParse(id, out var objectId)
            ? await reservations.FindAsync(objectId, cancellationToken)
                ?? throw new ApiException(404, "The reservation was not found.")
            : throw new ApiException(400, "Reservation ID must be a valid ObjectId.");

    private void EnsureVisible(EnergyReservation reservation)
    {
        var actor = currentUser.Get();
        if (actor.Role == UserRole.PROSUMER) EnsureOwner(actor, reservation);
        else if (actor.Role is not (UserRole.BACKOFFICE or UserRole.GRID_OPERATOR))
            throw new ApiException(403, "You do not have permission to view this reservation.");
    }

    private static void EnsureOwner(User actor, EnergyReservation reservation)
    {
        if (!string.Equals(actor.NIC, reservation.ProsumerNIC, StringComparison.OrdinalIgnoreCase))
            throw new ApiException(403, "A prosumer can only access their own reservations.");
    }

    private static void EnsureEditable(EnergyReservation reservation)
    {
        if (reservation.Status is not (ReservationStatus.PENDING or ReservationStatus.APPROVED))
            throw new ApiException(409, "Only pending or approved reservations can be changed.");
    }

    private static void EnsureNotice(EnergyReservation reservation, DateTime now, string action)
    {
        if (reservation.ReservationDateTime - now < TimeSpan.FromHours(12))
            throw new ApiException(409, $"Reservations require at least 12 hours notice to be {action}.");
    }

    private static void ValidateBookingWindow(DateTime reservationTime, DateTime now)
    {
        if (reservationTime <= now || reservationTime > now.AddDays(7))
            throw new ApiException(400, "Reservations must be scheduled within the next 7 days.");
    }

    private static string CreateReservationCode() => $"RSV-{Convert.ToHexString(RandomNumberGenerator.GetBytes(8))}";
}
