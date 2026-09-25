/*
 * File: src/SmartSolarMicrogrid.Api/DTOs/Slots/SlotResponses.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: API request/response contracts and validation for Slot Responses.
 */
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.DTOs.Slots;

public sealed record SlotResponse(string Id, string StationId, DateTime StartTime, DateTime EndTime,
    decimal Capacity, decimal AvailableCapacity, string Status, DateTime CreatedAt, DateTime UpdatedAt)
{
    // Map the stored model to the public response without exposing internal state.
    public static SlotResponse From(EnergyBookingSlot slot) => new(slot.Id.ToString(), slot.StationId.ToString(),
        slot.StartTime, slot.EndTime, slot.Capacity, slot.AvailableCapacity, slot.Status.ToString(),
        slot.CreatedAt, slot.UpdatedAt);
}

public sealed record SlotListResponse(IReadOnlyList<SlotResponse> Items);
