/*
 * File: src/SmartSolarMicrogrid.Api/DTOs/Dashboards/DashboardResponses.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: API request/response contracts and validation for Dashboard Responses.
 */
using SmartSolarMicrogrid.Api.DTOs.Reservations;

namespace SmartSolarMicrogrid.Api.DTOs.Dashboards;

public sealed record ReservationSearchResponse(IReadOnlyList<ReservationResponse> Items,
    long TotalCount, int Page, int PageSize);

public sealed record ReservationDashboardResponse(string Role, long PendingReservations,
    long ApprovedFutureReservations, long TodayReservations, long CompletedTransfers,
    long ActiveStations, long OpenSlots, decimal AvailableSlotCapacity,
    IReadOnlyList<ReservationResponse> RecentReservations, long ActiveReservations);
