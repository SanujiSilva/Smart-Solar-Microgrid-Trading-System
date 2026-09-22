/*
 * File: src/SmartSolarMicrogrid.Api/DTOs/Reservations/ReservationResponses.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: API request/response contracts and validation for Reservation Responses.
 */
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.DTOs.Reservations;

public sealed record ReservationResponse(string Id, string ReservationCode, string ProsumerNIC,
    string StationId, string SlotId, decimal EnergyAmount, DateTime ReservationDateTime, string Status,
    DateTime CreatedAt, DateTime UpdatedAt, DateTime? CompletedAt, string? CompletedByOperatorId)
{
    // Map the stored model to the public response without exposing internal state.
    public static ReservationResponse From(EnergyReservation reservation) => new(
        reservation.Id.ToString(), reservation.ReservationCode, reservation.ProsumerNIC,
        reservation.StationId.ToString(), reservation.SlotId.ToString(), reservation.EnergyAmount,
        reservation.ReservationDateTime, reservation.Status.ToString(), reservation.CreatedAt,
        reservation.UpdatedAt, reservation.CompletedAt, reservation.CompletedByOperatorId?.ToString());
}

public sealed record ReservationListResponse(IReadOnlyList<ReservationResponse> Items);
