using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartSolarMicrogrid.Api.DTOs.Stations;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateStationRequest : StationDetailsRequest
{
    private string stationCode = "";
    [Required, RegularExpression("[A-Z0-9][A-Z0-9_-]{1,29}")]
    public string StationCode { get => stationCode; init => stationCode = value?.Trim().ToUpperInvariant() ?? ""; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateStationRequest : StationDetailsRequest;

public class StationDetailsRequest
{
    private string name = "";
    private string address = "";
    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get => name; init => name = value?.Trim() ?? ""; }
    [Required, StringLength(500, MinimumLength = 2)]
    public string Address { get => address; init => address = value?.Trim() ?? ""; }
    [Required, Range(-90d, 90d)]
    public double? Latitude { get; init; }
    [Required, Range(-180d, 180d)]
    public double? Longitude { get; init; }
    [Required, Range(typeof(decimal), "0.001", "1000000000")]
    public decimal? CapacityKWh { get; init; }
    [Required, Range(0, 1000000)]
    public int? AvailableBatterySlots { get; init; }
    [Required, RegularExpression("INACTIVE|ACTIVE|MAINTENANCE")]
    public string Status { get; init; } = "INACTIVE";
    [Required]
    public OperatingScheduleRequest OperatingSchedule { get; init; } = new();
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class OperatingScheduleRequest
{
    [Required, StringLength(100)]
    public string TimeZoneId { get; init; } = "Asia/Colombo";
    [Required, MaxLength(28)]
    public List<OperatingPeriodRequest> WeeklyPeriods { get; init; } = [];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class OperatingPeriodRequest
{
    [Required, RegularExpression("Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday")]
    public string Day { get; init; } = "";
    [Required, Range(0, 1439)]
    public int? OpenMinuteOfDay { get; init; }
    [Required, Range(1, 1440)]
    public int? CloseMinuteOfDay { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class StationAvailabilityRequest
{
    [Required, Range(0, 1000000)]
    public int? AvailableBatterySlots { get; init; }
}

public sealed class StationListQuery
{
    [Range(1, 100000)]
    public int Page { get; init; } = 1;
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
    [StringLength(100)]
    public string? Search { get; init; }
    [RegularExpression("INACTIVE|ACTIVE|MAINTENANCE|DEACTIVATED")]
    public string? Status { get; init; }
}

public sealed class NearbyStationsQuery
{
    [Required, Range(-90d, 90d)]
    public double? Latitude { get; init; }
    [Required, Range(-180d, 180d)]
    public double? Longitude { get; init; }
    [Range(0.1, 100)]
    public double RadiusKm { get; init; } = 10;
    [Range(1, 50)]
    public int Limit { get; init; } = 20;
}
