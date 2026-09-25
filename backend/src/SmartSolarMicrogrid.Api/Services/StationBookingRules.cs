/*
 * File: src/SmartSolarMicrogrid.Api/Services/StationBookingRules.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Server-side application rules and orchestration for Station Booking Rules.
 */
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Services;

public static class StationBookingRules
{
    public static void RequireAvailable(SolarStationInfo station)
    {
        // Require Available for Station Booking Rules.
        if (station.Status != StationStatus.ACTIVE || station.AvailableBatterySlots <= 0)
            throw new ApiException(409, "The station is not active or has no battery slots available.");
    }

    public static void RequireSchedule(SolarStationInfo station, DateTime start, DateTime end)
    {
        // Require Schedule for Station Booking Rules.
        var zone = TimeZoneInfo.FindSystemTimeZoneById(station.OperatingSchedule.TimeZoneId);
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(start, zone);
        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(end, zone);
        var endMinute = (localEnd - localStart.Date).TotalMinutes;
        if (end <= start || !station.OperatingSchedule.WeeklyPeriods.Any(period =>
            period.Day == localStart.DayOfWeek && localStart.TimeOfDay.TotalMinutes >= period.OpenMinuteOfDay &&
            endMinute <= period.CloseMinuteOfDay))
            throw new ApiException(409, "The slot must fit inside a station operating period.");
    }
}
