/*
 * File: src/SmartSolarMicrogrid.Api/DTOs/Reservations/ReservationRequests.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: API request/response contracts and validation for Reservation Requests.
 */
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartSolarMicrogrid.Api.DTOs.Reservations;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateReservationRequest
{
    [RegularExpression(@"(?:[0-9]{9}[vVxX]|[0-9]{12})")]
    public string? ProsumerNIC { get; init; }

    [Required]
    public string SlotId { get; init; } = "";

    [Required, Range(typeof(decimal), "0.001", "1000000000")]
    public decimal? EnergyAmount { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateReservationRequest
{
    // Omitted by older clients for an energy-only update.
    public string? SlotId { get; init; }

    [Required, Range(typeof(decimal), "0.001", "1000000000")]
    public decimal? EnergyAmount { get; init; }
}
