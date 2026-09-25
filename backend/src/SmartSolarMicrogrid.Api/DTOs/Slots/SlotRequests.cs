/*
 * File: src/SmartSolarMicrogrid.Api/DTOs/Slots/SlotRequests.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: API request/response contracts and validation for Slot Requests.
 */
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartSolarMicrogrid.Api.DTOs.Slots;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateSlotRequest : SlotDetailsRequest;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateSlotRequest : SlotDetailsRequest;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SlotAvailabilityRequest
{
    [Required, Range(typeof(decimal), "0", "1000000000")]
    public decimal? AvailableCapacity { get; init; }
    [Required, RegularExpression("OPEN|CLOSED")]
    public string Status { get; init; } = "OPEN";
}

public class SlotDetailsRequest
{
    [Required]
    public DateTimeOffset? StartTime { get; init; }

    [Required]
    public DateTimeOffset? EndTime { get; init; }

    [Required, Range(typeof(decimal), "0.001", "1000000000")]
    public decimal? Capacity { get; init; }

    [Required, Range(typeof(decimal), "0", "1000000000")]
    public decimal? AvailableCapacity { get; init; }

    [Required, RegularExpression("OPEN|CLOSED")]
    public string Status { get; init; } = "OPEN";
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SlotListQuery
{
    public bool IncludeCancelled { get; init; }
}
