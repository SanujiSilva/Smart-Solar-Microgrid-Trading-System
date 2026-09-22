/*
 * File: src/SmartSolarMicrogrid.Api/Controllers/DashboardController.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: HTTP endpoints and authorization boundaries for Dashboard.
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.DTOs.Dashboards;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController, Route("api/reservations"), Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DashboardController(DashboardService dashboard) : ControllerBase
{
    // Search for Dashboard.
    [HttpGet("search")]
    public async Task<ActionResult<ReservationSearchResponse>> Search([FromQuery] ReservationSearchQuery query,
        CancellationToken cancellationToken) => Ok(await dashboard.SearchAsync(query, cancellationToken));

    // Dashboard for Dashboard.
    [HttpGet("dashboard")]
    public async Task<ActionResult<ReservationDashboardResponse>> Dashboard(CancellationToken cancellationToken) =>
        Ok(await dashboard.GetAsync(cancellationToken));
}
