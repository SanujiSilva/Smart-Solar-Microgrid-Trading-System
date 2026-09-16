using MongoDB.Bson;
using SmartSolarMicrogrid.Api.DTOs.Slots;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Repositories;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class SlotService(ISlotRepository slots, IStationRepository stations, CurrentUser currentUser,
    TimeProvider clock)
{
    public async Task<SlotListResponse> ListAsync(string stationId, SlotListQuery query,
        CancellationToken cancellationToken)
    {
        currentUser.Get();
        var id = ParseId(stationId, "Station ID");
        await RequireStation(id, cancellationToken);
        var result = await slots.ListAsync(id, query.IncludeCancelled, cancellationToken);
        return new(result.Select(SlotResponse.From).ToList());
    }

    public async Task<SlotResponse> CreateAsync(string stationId, CreateSlotRequest request,
        CancellationToken cancellationToken)
    {
        currentUser.Require(UserRole.BACKOFFICE);
        var id = ParseId(stationId, "Station ID");
        var station = await RequireStation(id, cancellationToken);
        RequireUsableStation(station);
        var details = ValidateDetails(request);
        if (await slots.HasOverlapAsync(id, details.StartTime, details.EndTime, null, cancellationToken))
            throw new ApiException(409, "The station already has an overlapping active slot.");

        var now = clock.GetUtcNow().UtcDateTime;
        var slot = new EnergyBookingSlot
        {
            StationId = id, StartTime = details.StartTime, EndTime = details.EndTime,
            Capacity = details.Capacity, AvailableCapacity = details.AvailableCapacity,
            Status = details.Status, CreatedAt = now, UpdatedAt = now
        };
        await slots.CreateAsync(slot, cancellationToken);
        return SlotResponse.From(slot);
    }

    public async Task<SlotResponse> GetAsync(string id, CancellationToken cancellationToken)
    {
        currentUser.Get();
        return SlotResponse.From(await Find(id, cancellationToken));
    }

    public async Task<SlotResponse> UpdateAsync(string id, UpdateSlotRequest request,
        CancellationToken cancellationToken)
    {
        currentUser.Require(UserRole.BACKOFFICE);
        var slot = await Find(id, cancellationToken);
        if (slot.Status == SlotStatus.CANCELLED) throw new ApiException(409, "A cancelled slot cannot be updated.");
        var station = await RequireStation(slot.StationId, cancellationToken);
        RequireUsableStation(station);
        var details = ValidateDetails(request);
        if (await slots.HasOverlapAsync(slot.StationId, details.StartTime, details.EndTime, slot.Id, cancellationToken))
            throw new ApiException(409, "The station already has an overlapping active slot.");

        slot.StartTime = details.StartTime;
        slot.EndTime = details.EndTime;
        slot.Capacity = details.Capacity;
        slot.AvailableCapacity = details.AvailableCapacity;
        slot.Status = details.Status;
        slot.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        var saved = await slots.UpdateAsync(slot, slot, cancellationToken)
            ?? throw new ApiException(409, "The slot changed during this request. Reload and try again.");
        return SlotResponse.From(saved);
    }

    public async Task<SlotResponse> CancelAsync(string id, CancellationToken cancellationToken)
    {
        currentUser.Require(UserRole.BACKOFFICE);
        var slot = await Find(id, cancellationToken);
        if (slot.Status == SlotStatus.CANCELLED) return SlotResponse.From(slot);
        slot.Status = SlotStatus.CANCELLED;
        slot.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        var saved = await slots.UpdateAsync(slot, slot, cancellationToken)
            ?? throw new ApiException(409, "The slot changed during this request. Reload and try again.");
        return SlotResponse.From(saved);
    }

    private async Task<EnergyBookingSlot> Find(string id, CancellationToken cancellationToken)
    {
        var objectId = ParseId(id, "Slot ID");
        return await slots.FindAsync(objectId, cancellationToken)
            ?? throw new ApiException(404, "The slot was not found.");
    }

    private async Task<SolarStationInfo> RequireStation(ObjectId id, CancellationToken cancellationToken) =>
        await stations.FindAsync(id, cancellationToken)
        ?? throw new ApiException(404, "The station was not found.");

    private static void RequireUsableStation(SolarStationInfo station)
    {
        if (station.Status == StationStatus.DEACTIVATED)
            throw new ApiException(409, "A deactivated station cannot have slots managed.");
    }

    private static ObjectId ParseId(string value, string name) =>
        ObjectId.TryParse(value, out var id) ? id : throw new ApiException(400, $"{name} must be a valid ObjectId.");

    private static SlotDetails ValidateDetails(SlotDetailsRequest request)
    {
        if (request.StartTime is null || request.EndTime is null || request.Capacity is null || request.AvailableCapacity is null)
            throw new ApiException(400, "Slot times and capacities are required.");
        var start = request.StartTime.Value.UtcDateTime;
        var end = request.EndTime.Value.UtcDateTime;
        if (end <= start) throw new ApiException(400, "Slot end time must be after its start time.");
        if (request.Capacity.Value <= 0 || request.AvailableCapacity.Value < 0 ||
            request.AvailableCapacity.Value > request.Capacity.Value)
            throw new ApiException(400, "Available capacity must be between zero and total capacity.");
        if (!Enum.TryParse<SlotStatus>(request.Status, out var status) || status is SlotStatus.CANCELLED || !Enum.IsDefined(status))
            throw new ApiException(400, "Slot status must be OPEN or CLOSED.");
        return new SlotDetails(start, end, request.Capacity.Value, request.AvailableCapacity.Value, status);
    }

    private sealed record SlotDetails(DateTime StartTime, DateTime EndTime, decimal Capacity,
        decimal AvailableCapacity, SlotStatus Status);
}
