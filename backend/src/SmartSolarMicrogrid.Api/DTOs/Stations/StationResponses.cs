using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.DTOs.Stations;

public sealed record OperatingPeriodResponse(string Day, int OpenMinuteOfDay, int CloseMinuteOfDay);
public sealed record OperatingScheduleResponse(string TimeZoneId, IReadOnlyList<OperatingPeriodResponse> WeeklyPeriods)
{
    public static OperatingScheduleResponse From(OperatingSchedule schedule) => new(schedule.TimeZoneId,
        schedule.WeeklyPeriods.Select(x => new OperatingPeriodResponse(x.Day.ToString(), x.OpenMinuteOfDay, x.CloseMinuteOfDay)).ToList());
}

public sealed record StationResponse(string Id, string StationCode, string Name, string Address,
    double Latitude, double Longitude, decimal CapacityKWh, int AvailableBatterySlots,
    string Status, OperatingScheduleResponse OperatingSchedule, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static StationResponse From(SolarStationInfo station) => new(station.Id.ToString(), station.StationCode,
        station.Name, station.Address, station.Latitude, station.Longitude, station.CapacityKWh,
        station.AvailableBatterySlots, station.Status.ToString(), OperatingScheduleResponse.From(station.OperatingSchedule),
        station.CreatedAt, station.UpdatedAt);
}

public sealed record StationPageResponse(IReadOnlyList<StationResponse> Items, long TotalCount, int Page, int PageSize);
public sealed record NearbyStationsResponse(IReadOnlyList<StationResponse> Items);
