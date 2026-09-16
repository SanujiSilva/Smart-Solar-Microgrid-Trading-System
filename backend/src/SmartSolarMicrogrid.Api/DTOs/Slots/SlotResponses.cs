using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.DTOs.Slots;

public sealed record SlotResponse(string Id, string StationId, DateTime StartTime, DateTime EndTime,
    decimal Capacity, decimal AvailableCapacity, string Status, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static SlotResponse From(EnergyBookingSlot slot) => new(slot.Id.ToString(), slot.StationId.ToString(),
        slot.StartTime, slot.EndTime, slot.Capacity, slot.AvailableCapacity, slot.Status.ToString(),
        slot.CreatedAt, slot.UpdatedAt);
}

public sealed record SlotListResponse(IReadOnlyList<SlotResponse> Items);
