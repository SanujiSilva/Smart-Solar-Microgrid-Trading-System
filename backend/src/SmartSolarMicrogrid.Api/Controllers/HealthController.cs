/*
 * File: src/SmartSolarMicrogrid.Api/Controllers/HealthController.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: HTTP endpoints and authorization boundaries for Health.
 */
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartSolarMicrogrid.Api.DTOs;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/health")]
public sealed class HealthController(
    HealthCheckService healthChecks, TimeProvider timeProvider) : ControllerBase
{
    /// <summary>Reports API liveness independently of database availability.</summary>
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status503ServiceUnavailable)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<HealthResponse>> Get(CancellationToken cancellationToken)
    {
        // Get for Health.
        var report = await healthChecks.CheckHealthAsync(x => !x.Tags.Contains("ready"), cancellationToken);
        var response = new HealthResponse(report.Status.ToString(), timeProvider.GetUtcNow());
        return StatusCode(report.Status == HealthStatus.Unhealthy
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK, response);
    }

    /// <summary>Reports whether MongoDB is reachable. No connection details are returned.</summary>
    [HttpGet("ready")]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status503ServiceUnavailable)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<HealthResponse>> Ready(CancellationToken cancellationToken)
    {
        // Ready for Health.
        var report = await healthChecks.CheckHealthAsync(x => x.Tags.Contains("ready"), cancellationToken);
        return StatusCode(report.Status == HealthStatus.Healthy
            ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable,
            new HealthResponse(report.Status.ToString(), timeProvider.GetUtcNow()));
    }
}
