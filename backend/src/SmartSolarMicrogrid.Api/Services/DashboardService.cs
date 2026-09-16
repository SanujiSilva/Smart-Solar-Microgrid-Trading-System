using MongoDB.Bson;
using SmartSolarMicrogrid.Api.DTOs.Dashboards;
using SmartSolarMicrogrid.Api.DTOs.Reservations;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Repositories;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class DashboardService(IReservationRepository reservations, IStationRepository stations,
    ISlotRepository slots, CurrentUser currentUser, TimeProvider clock)
{
    public async Task<ReservationSearchResponse> SearchAsync(ReservationSearchQuery query,
        CancellationToken cancellationToken)
    {
        var actor = currentUser.Get();
        var prosumerNic = actor.Role == UserRole.PROSUMER
            ? actor.NIC ?? throw new ApiException(409, "The authenticated account has no prosumer NIC.")
            : RequireStaff(actor);
        var status = ParseStatus(query.Status);
        ObjectId? stationId = null;
        if (!string.IsNullOrWhiteSpace(query.StationId))
        {
            if (!ObjectId.TryParse(query.StationId, out var parsed))
                throw new ApiException(400, "Station ID must be a valid ObjectId.");
            stationId = parsed;
        }
        var from = query.From?.UtcDateTime;
        var to = query.To?.UtcDateTime;
        if (from.HasValue && to.HasValue && to <= from)
            throw new ApiException(400, "The reservation search end date must be after the start date.");
        var result = await reservations.SearchAsync(prosumerNic, query.ReservationCode, stationId, status,
            from, to, query.Page, query.PageSize, cancellationToken);
        return new(result.Items.Select(ReservationResponse.From).ToList(), result.TotalCount,
            query.Page, query.PageSize);
    }

    public async Task<ReservationDashboardResponse> GetAsync(CancellationToken cancellationToken)
    {
        var actor = currentUser.Get();
        var prosumerNic = actor.Role == UserRole.PROSUMER
            ? actor.NIC ?? throw new ApiException(409, "The authenticated account has no prosumer NIC.")
            : RequireStaff(actor);
        var now = clock.GetUtcNow().UtcDateTime;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var pending = await CountAsync(prosumerNic, ReservationStatus.PENDING, null, null, cancellationToken);
        var approvedFuture = await CountAsync(prosumerNic, ReservationStatus.APPROVED, now, null, cancellationToken);
        var todayReservations = await CountAsync(prosumerNic, null, today, tomorrow, cancellationToken);
        var completed = await reservations.CountCompletedAsync(prosumerNic, today, tomorrow, cancellationToken);
        var recent = await reservations.SearchAsync(prosumerNic, null, null, null, null, null, 1, 5, cancellationToken);
        var activeStations = await stations.SearchAsync(StationStatus.ACTIVE, null, 1, 1, cancellationToken);
        var slotSummary = await slots.GetOperationalSummaryAsync(cancellationToken);
        return new(actor.Role.ToString(), pending, approvedFuture, todayReservations, completed,
            activeStations.TotalCount, slotSummary.OpenSlotCount, slotSummary.AvailableCapacity,
            recent.Items.Select(ReservationResponse.From).ToList());
    }

    private async Task<long> CountAsync(string? prosumerNic, ReservationStatus? status,
        DateTime? from, DateTime? to, CancellationToken cancellationToken) =>
        (await reservations.SearchAsync(prosumerNic, null, null, status, from, to, 1, 1, cancellationToken)).TotalCount;

    private static string? RequireStaff(User actor) => actor.Role is UserRole.BACKOFFICE or UserRole.GRID_OPERATOR
        ? null
        : throw new ApiException(403, "Dashboard and reservation search access is restricted to authenticated users.");

    private static ReservationStatus? ParseStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) ? null : Enum.TryParse<ReservationStatus>(status, out var parsed) && Enum.IsDefined(parsed)
            ? parsed : throw new ApiException(400, "Reservation status is invalid.");
}
