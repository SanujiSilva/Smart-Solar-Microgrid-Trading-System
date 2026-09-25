/*
 * File: src/SmartSolarMicrogrid.Api/Services/ReservationService.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Server-side application rules and orchestration for Reservation Service.
 */
using System.Security.Cryptography;
using MongoDB.Bson;
using SmartSolarMicrogrid.Api.DTOs.Reservations;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Repositories;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class ReservationService(IReservationRepository reservations, ISlotRepository slots,
    IStationRepository stations, IUserRepository users, CurrentUser currentUser, TimeProvider clock)
{
    public async Task<ReservationResponse> CreateAsync(CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        // Create for Reservation.
        var actor = currentUser.Get();
        User prosumer;
        if (actor.Role == UserRole.PROSUMER)
        {
            if (request.ProsumerNIC is not null && !string.Equals(request.ProsumerNIC, actor.NIC, StringComparison.OrdinalIgnoreCase))
                throw new ApiException(403, "A prosumer can only book for their own NIC.");
            prosumer = actor;
        }
        else
        {
            RequireStaff(actor);
            if (string.IsNullOrWhiteSpace(request.ProsumerNIC))
                throw new ApiException(400, "A prosumer NIC is required when staff create a reservation.");
            prosumer = await users.FindProsumerByNicAsync(request.ProsumerNIC.Trim().ToUpperInvariant(), cancellationToken)
                ?? throw new ApiException(404, "Prosumer was not found.");
        }
        if (prosumer.Status != UserStatus.ACTIVE)
            throw new ApiException(403, "Only active prosumers can create reservations.");
        if (!ObjectId.TryParse(request.SlotId, out var slotId))
            throw new ApiException(400, "Slot ID must be a valid ObjectId.");
        if (request.EnergyAmount is null || request.EnergyAmount <= 0)
            throw new ApiException(400, "Energy amount must be greater than zero.");

        var slot = await slots.FindAsync(slotId, cancellationToken)
            ?? throw new ApiException(404, "The slot was not found.");
        var station = await stations.FindAsync(slot.StationId, cancellationToken)
            ?? throw new ApiException(404, "The station was not found.");
        StationBookingRules.RequireAvailable(station);
        StationBookingRules.RequireSchedule(station, slot.StartTime, slot.EndTime);
        ValidateBookingWindow(slot.StartTime, clock.GetUtcNow().UtcDateTime);

        var amount = request.EnergyAmount.Value;
        if (!await slots.TryAdjustCapacityAsync(slot.Id, -amount, true, cancellationToken))
            throw new ApiException(409, "The slot is closed or does not have enough available capacity.");

        var now = clock.GetUtcNow().UtcDateTime;
        var reservation = new EnergyReservation
        {
            ReservationCode = CreateReservationCode(), ProsumerNIC = prosumer.NIC
                ?? throw new ApiException(409, "The authenticated account has no prosumer NIC."),
            StationId = slot.StationId, SlotId = slot.Id, EnergyAmount = amount,
            ReservationDateTime = slot.StartTime, Status = ReservationStatus.PENDING,
            CreatedAt = now, UpdatedAt = now
        };
        await reservations.CreateAsync(reservation, cancellationToken);
        return ReservationResponse.From(reservation);
    }

    public async Task<ReservationResponse> GetAsync(string id, CancellationToken cancellationToken)
    {
        // Get for Reservation.
        var reservation = await FindAsync(id, cancellationToken);
        EnsureVisible(reservation);
        return ReservationResponse.From(reservation);
    }

    public async Task<ReservationListResponse> MyAsync(CancellationToken cancellationToken)
    {
        // My for Reservation.
        var actor = currentUser.Require(UserRole.PROSUMER);
        var items = await reservations.ListByProsumerAsync(actor.NIC
            ?? throw new ApiException(409, "The authenticated account has no prosumer NIC."), cancellationToken);
        return new(items.Select(ReservationResponse.From).ToList());
    }

    // Pending for Reservation.
    public Task<ReservationListResponse> PendingAsync(CancellationToken cancellationToken) =>
        ListByStatusAsync(ReservationStatus.PENDING, cancellationToken);

    public async Task<ReservationListResponse> HistoryAsync(CancellationToken cancellationToken)
    {
        // History for Reservation.
        var actor = currentUser.Require(UserRole.PROSUMER);
        var items = await reservations.ListByProsumerAsync(actor.NIC
            ?? throw new ApiException(409, "The authenticated account has no prosumer NIC."), cancellationToken);
        return new(items.Where(x => x.Status is ReservationStatus.CANCELLED or ReservationStatus.COMPLETED or ReservationStatus.REJECTED)
            .Select(ReservationResponse.From).ToList());
    }

    public async Task<ReservationResponse> UpdateAsync(string id, UpdateReservationRequest request,
        CancellationToken cancellationToken)
    {
        // Update for Reservation.
        var actor = currentUser.Get();
        var reservation = await FindAsync(id, cancellationToken);
        if (actor.Role == UserRole.PROSUMER) EnsureOwner(actor, reservation);
        else RequireStaff(actor);
        EnsureEditable(reservation);
        var now = clock.GetUtcNow().UtcDateTime;
        EnsureNotice(reservation, now, "updated");
        var prosumer = await users.FindProsumerByNicAsync(reservation.ProsumerNIC, cancellationToken)
            ?? throw new ApiException(404, "Prosumer was not found.");
        if (prosumer.Status != UserStatus.ACTIVE)
            throw new ApiException(403, "Only active prosumers can change reservations.");
        if (request.EnergyAmount is null || request.EnergyAmount <= 0)
            throw new ApiException(400, "Energy amount must be greater than zero.");
        var amount = request.EnergyAmount.Value;
        var targetId = reservation.SlotId;
        if (request.SlotId is not null && !ObjectId.TryParse(request.SlotId, out targetId))
            throw new ApiException(400, "Slot ID must be a valid ObjectId.");
        var target = await slots.FindAsync(targetId, cancellationToken)
            ?? throw new ApiException(404, "The slot was not found.");
        var station = await stations.FindAsync(target.StationId, cancellationToken)
            ?? throw new ApiException(404, "The station was not found.");
        StationBookingRules.RequireAvailable(station);
        StationBookingRules.RequireSchedule(station, target.StartTime, target.EndTime);
        ValidateBookingWindow(target.StartTime, now);
        if (target.Status != SlotStatus.OPEN)
            throw new ApiException(409, "The slot is not open.");
        var moved = targetId != reservation.SlotId;
        if (moved)
        {
            if (target.StartTime - now < TimeSpan.FromHours(12))
                throw new ApiException(409, "A rescheduled slot requires at least 12 hours notice.");
            // The trading transaction commits both capacity changes and the reservation together.
            if (!await slots.TryAdjustCapacityAsync(targetId, -amount, true, cancellationToken))
                throw new ApiException(409, "The new slot does not have enough available capacity.");
            if (!await slots.TryAdjustCapacityAsync(reservation.SlotId, reservation.EnergyAmount, false, cancellationToken))
                throw new ApiException(409, "The previous slot cannot receive the released capacity.");
            reservation.SlotId = targetId;
            reservation.StationId = target.StationId;
            reservation.ReservationDateTime = target.StartTime;
        }
        else
        {
            var delta = reservation.EnergyAmount - amount;
            if (delta != 0 && !await slots.TryAdjustCapacityAsync(targetId, delta, delta < 0, cancellationToken))
                throw new ApiException(409, "The slot does not have enough available capacity for this update.");
        }
        if (moved || amount != reservation.EnergyAmount)
        {
            reservation.Status = ReservationStatus.PENDING;
            reservation.QrTokenHash = null;
        }
        reservation.EnergyAmount = amount;
        reservation.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        var saved = await reservations.UpdateAsync(reservation, cancellationToken)
                ?? throw new ApiException(409, "The reservation changed during this request. Reload and try again.");
            return ReservationResponse.From(saved);

    }

    public async Task<ReservationResponse> CancelAsync(string id, CancellationToken cancellationToken)
    {
        // Cancel for Reservation.
        var actor = currentUser.Get();
        var reservation = await FindAsync(id, cancellationToken);
        if (actor.Role == UserRole.PROSUMER) EnsureOwner(actor, reservation);
        else if (actor.Role is not (UserRole.BACKOFFICE or UserRole.GRID_OPERATOR))
            throw new ApiException(403, "Staff or reservation owner access is required.");
        EnsureEditable(reservation);
        EnsureNotice(reservation, clock.GetUtcNow().UtcDateTime, "cancelled");
        if (!await slots.TryAdjustCapacityAsync(reservation.SlotId, reservation.EnergyAmount, false, cancellationToken))
            throw new ApiException(409, "The slot cannot receive the released capacity.");

        reservation.Status = ReservationStatus.CANCELLED;
        reservation.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        var saved = await reservations.UpdateAsync(reservation, cancellationToken)
                ?? throw new ApiException(409, "The reservation changed during this request. Reload and try again.");
            return ReservationResponse.From(saved);

    }

    private async Task<ReservationListResponse> ListByStatusAsync(ReservationStatus status,
        CancellationToken cancellationToken)
    {
        // List By Status for Reservation.
        var actor = currentUser.Get();
        if (actor.Role is not (UserRole.BACKOFFICE or UserRole.GRID_OPERATOR))
            throw new ApiException(403, "Staff access is required.");
        var items = await reservations.ListByStatusAsync(status, cancellationToken);
        return new(items.Select(ReservationResponse.From).ToList());
    }

    public async Task<ReservationResponse> ReviewAsync(string id, bool approve, CancellationToken cancellationToken)
    {
        // Review for Reservation.
        currentUser.Require(UserRole.BACKOFFICE);
        var reservation = await FindAsync(id, cancellationToken);
        if (reservation.Status != ReservationStatus.PENDING)
            throw new ApiException(409, "Only pending reservations can be reviewed.");
        if (approve)
        {
            var station = await stations.FindAsync(reservation.StationId, cancellationToken)
                ?? throw new ApiException(404, "Station was not found.");
            var slot = await slots.FindAsync(reservation.SlotId, cancellationToken)
                ?? throw new ApiException(404, "Slot was not found.");
            StationBookingRules.RequireAvailable(station);
            StationBookingRules.RequireSchedule(station, slot.StartTime, slot.EndTime);
            ValidateBookingWindow(reservation.ReservationDateTime, clock.GetUtcNow().UtcDateTime);
            if (slot.Status != SlotStatus.OPEN) throw new ApiException(409, "The slot is not open.");
            reservation.Status = ReservationStatus.APPROVED;
        }
        else
        {
            if (!await slots.TryAdjustCapacityAsync(reservation.SlotId, reservation.EnergyAmount, false, cancellationToken))
                throw new ApiException(409, "The slot cannot receive the released capacity.");
            reservation.Status = ReservationStatus.REJECTED;
        }
        reservation.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        return ReservationResponse.From(await reservations.UpdateAsync(reservation, cancellationToken)
            ?? throw new ApiException(409, "The reservation changed. Reload and retry."));
    }

    // Find for Reservation.
    private async Task<EnergyReservation> FindAsync(string id, CancellationToken cancellationToken) =>
        ObjectId.TryParse(id, out var objectId)
            ? await reservations.FindAsync(objectId, cancellationToken)
                ?? throw new ApiException(404, "The reservation was not found.")
            : throw new ApiException(400, "Reservation ID must be a valid ObjectId.");

    private void EnsureVisible(EnergyReservation reservation)
    {
        // Ensure Visible for Reservation.
        var actor = currentUser.Get();
        if (actor.Role == UserRole.PROSUMER) EnsureOwner(actor, reservation);
        else if (actor.Role is not (UserRole.BACKOFFICE or UserRole.GRID_OPERATOR))
            throw new ApiException(403, "You do not have permission to view this reservation.");
    }

    private static void EnsureOwner(User actor, EnergyReservation reservation)
    {
        // Ensure Owner for Reservation.
        if (!string.Equals(actor.NIC, reservation.ProsumerNIC, StringComparison.OrdinalIgnoreCase))
            throw new ApiException(403, "A prosumer can only access their own reservations.");
    }

    private static void RequireStaff(User actor)
    {
        if (actor.Role is not (UserRole.BACKOFFICE or UserRole.GRID_OPERATOR))
            throw new ApiException(403, "Staff access is required.");
    }

    private static void EnsureEditable(EnergyReservation reservation)
    {
        // Ensure Editable for Reservation.
        if (reservation.Status is not (ReservationStatus.PENDING or ReservationStatus.APPROVED))
            throw new ApiException(409, "Only pending or approved reservations can be changed.");
    }

    private static void EnsureNotice(EnergyReservation reservation, DateTime now, string action)
    {
        // Ensure Notice for Reservation.
        if (reservation.ReservationDateTime - now < TimeSpan.FromHours(12))
            throw new ApiException(409, $"Reservations require at least 12 hours notice to be {action}.");
    }

    private static void ValidateBookingWindow(DateTime reservationTime, DateTime now)
    {
        // Validate Booking Window for Reservation.
        if (reservationTime <= now || reservationTime > now.AddDays(7))
            throw new ApiException(400, "Reservations must be scheduled within the next 7 days.");
    }

    // Create Reservation Code for Reservation.
    private static string CreateReservationCode() => $"RSV-{Convert.ToHexString(RandomNumberGenerator.GetBytes(8))}";
}
