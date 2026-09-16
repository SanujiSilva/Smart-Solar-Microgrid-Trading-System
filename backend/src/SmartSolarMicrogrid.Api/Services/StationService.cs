using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using SmartSolarMicrogrid.Api.DTOs.Stations;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Repositories;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class StationService(IStationRepository stations, CurrentUser currentUser, TimeProvider clock)
{
    public async Task<StationPageResponse> ListAsync(StationListQuery query, CancellationToken cancellationToken)
    {
        currentUser.Get();
        StationStatus? status = query.Status is null ? null : Enum.Parse<StationStatus>(query.Status);
        var result = await stations.SearchAsync(status, query.Search, query.Page, query.PageSize, cancellationToken);
        return new(result.Items.Select(StationResponse.From).ToList(), result.TotalCount, query.Page, query.PageSize);
    }

    public async Task<StationResponse> GetAsync(string id, CancellationToken cancellationToken)
    {
        currentUser.Get();
        return StationResponse.From(await Find(id, cancellationToken));
    }

    public async Task<NearbyStationsResponse> NearbyAsync(NearbyStationsQuery query, CancellationToken cancellationToken)
    {
        currentUser.Get();
        ValidateCoordinates(query.Latitude!.Value, query.Longitude!.Value);
        if (!double.IsFinite(query.RadiusKm)) throw new ApiException(400, "Radius must be finite.");
        var results = await stations.NearbyAsync(query.Latitude.Value, query.Longitude.Value, query.RadiusKm, query.Limit, cancellationToken);
        return new(results.Select(StationResponse.From).ToList());
    }

    public async Task<StationResponse> CreateAsync(CreateStationRequest request, CancellationToken cancellationToken)
    {
        currentUser.Require(UserRole.BACKOFFICE);
        var station = MapDetails(request, request.StationCode);
        station.CreatedAt = station.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await stations.CreateAsync(station, cancellationToken);
        return StationResponse.From(station);
    }

    public async Task<StationResponse> UpdateAsync(string id, UpdateStationRequest request, CancellationToken cancellationToken)
    {
        currentUser.Require(UserRole.BACKOFFICE);
        var existing = await Find(id, cancellationToken);
        var replacement = MapDetails(request, existing.StationCode);
        if (existing.Status != replacement.Status || existing.CapacityKWh != replacement.CapacityKWh ||
            existing.Latitude != replacement.Latitude || existing.Longitude != replacement.Longitude ||
            !SameSchedule(existing.OperatingSchedule, replacement.OperatingSchedule))
            await RequireNoReservations(existing.Id, cancellationToken);
        return await Save(existing, replacement, cancellationToken);
    }

    public async Task<StationResponse> UpdateScheduleAsync(string id, OperatingScheduleRequest request, CancellationToken cancellationToken)
    {
        currentUser.Require(UserRole.BACKOFFICE);
        var station = await Find(id, cancellationToken);
        var schedule = ValidateSchedule(request, station.Status);
        if (!SameSchedule(station.OperatingSchedule, schedule)) await RequireNoReservations(station.Id, cancellationToken);
        station.OperatingSchedule = schedule;
        return await Save(station, station, cancellationToken);
    }

    public async Task<StationResponse> DeactivateAsync(string id, CancellationToken cancellationToken)
    {
        currentUser.Require(UserRole.BACKOFFICE);
        var station = await Find(id, cancellationToken);
        await RequireNoReservations(station.Id, cancellationToken);
        if (station.Status == StationStatus.DEACTIVATED) return StationResponse.From(station);
        station.Status = StationStatus.DEACTIVATED;
        return await Save(station, station, cancellationToken);
    }

    public async Task<StationResponse> UpdateAvailabilityAsync(string id, StationAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var actor = currentUser.Get();
        if (actor.Role is not (UserRole.BACKOFFICE or UserRole.GRID_OPERATOR)) throw new ApiException(403, "Staff access is required.");
        var station = await Find(id, cancellationToken);
        if (station.Status == StationStatus.DEACTIVATED) throw new ApiException(409, "A deactivated station cannot have its availability updated.");
        station.AvailableBatterySlots = request.AvailableBatterySlots!.Value;
        return await Save(station, station, cancellationToken);
    }

    private async Task RequireNoReservations(ObjectId id, CancellationToken cancellationToken)
    {
        if (await stations.HasUnresolvedReservationsAsync(id, cancellationToken))
            throw new ApiException(409, "The station has pending or approved reservations. Resolve them before changing its operating configuration or deactivating it.");
    }

    private async Task<StationResponse> Save(SolarStationInfo existing, SolarStationInfo replacement, CancellationToken cancellationToken)
    {
        replacement.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        var saved = await stations.UpdateAsync(existing, replacement, cancellationToken)
            ?? throw new ApiException(409, "The station changed during this request. Reload and try again.");
        return StationResponse.From(saved);
    }

    private async Task<SolarStationInfo> Find(string id, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out var objectId)) throw new ApiException(400, "Station ID must be a valid ObjectId.");
        return await stations.FindAsync(objectId, cancellationToken) ?? throw new ApiException(404, "Station was not found.");
    }

    private static SolarStationInfo MapDetails(StationDetailsRequest request, string stationCode)
    {
        ValidateCoordinates(request.Latitude!.Value, request.Longitude!.Value);
        if (!Enum.TryParse<StationStatus>(request.Status, out var status) || status == StationStatus.DEACTIVATED || !Enum.IsDefined(status))
            throw new ApiException(400, "Use the dedicated deactivation endpoint to deactivate a station.");
        return new SolarStationInfo
        {
            StationCode = stationCode, Name = request.Name, Address = request.Address,
            Location = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new(request.Longitude.Value, request.Latitude.Value)),
            CapacityKWh = request.CapacityKWh!.Value, AvailableBatterySlots = request.AvailableBatterySlots!.Value,
            Status = status, OperatingSchedule = ValidateSchedule(request.OperatingSchedule, status)
        };
    }

    private static void ValidateCoordinates(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90 ||
            !double.IsFinite(longitude) || longitude is < -180 or > 180)
            throw new ApiException(400, "Latitude/longitude must be finite values within geographic bounds.");
    }

    private static OperatingSchedule ValidateSchedule(OperatingScheduleRequest request, StationStatus status)
    {
        if (!TimeZoneInfo.TryConvertIanaIdToWindowsId(request.TimeZoneId, out _))
            throw new ApiException(400, "Operating schedule must use a valid IANA time zone, such as Asia/Colombo or UTC.");
        if (status == StationStatus.ACTIVE && request.WeeklyPeriods.Count == 0)
            throw new ApiException(400, "An active station requires at least one operating period.");
        var periods = new List<OperatingPeriod>();
        foreach (var period in request.WeeklyPeriods)
        {
            if (period is null || !Enum.TryParse<DayOfWeek>(period.Day, out var day) || !Enum.IsDefined(day) ||
                period.OpenMinuteOfDay is null or < 0 or > 1439 || period.CloseMinuteOfDay is null or < 1 or > 1440 ||
                period.OpenMinuteOfDay >= period.CloseMinuteOfDay)
                throw new ApiException(400, "Operating periods must have a valid day and opening time before closing time. Split overnight periods at midnight.");
            periods.Add(new OperatingPeriod { Day = day, OpenMinuteOfDay = period.OpenMinuteOfDay.Value, CloseMinuteOfDay = period.CloseMinuteOfDay.Value });
        }
        var ordered = periods.OrderBy(x => x.Day).ThenBy(x => x.OpenMinuteOfDay).ToList();
        for (var i = 1; i < ordered.Count; i++)
            if (ordered[i].Day == ordered[i - 1].Day && ordered[i].OpenMinuteOfDay < ordered[i - 1].CloseMinuteOfDay)
                throw new ApiException(400, "Operating periods on the same day cannot overlap.");
        return new OperatingSchedule { TimeZoneId = request.TimeZoneId, WeeklyPeriods = ordered };
    }

    private static bool SameSchedule(OperatingSchedule left, OperatingSchedule right) =>
        left.TimeZoneId == right.TimeZoneId &&
        left.WeeklyPeriods.Select(x => (x.Day, x.OpenMinuteOfDay, x.CloseMinuteOfDay)).Order().SequenceEqual(
            right.WeeklyPeriods.Select(x => (x.Day, x.OpenMinuteOfDay, x.CloseMinuteOfDay)).Order());
}
