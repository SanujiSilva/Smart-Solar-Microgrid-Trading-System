using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs.Stations;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController, Route("api/stations"), Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class StationsController(StationService stations) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StationPageResponse>> List([FromQuery] StationListQuery query, CancellationToken cancellationToken) =>
        Ok(await stations.ListAsync(query, cancellationToken));

    [HttpGet("nearby")]
    public async Task<ActionResult<NearbyStationsResponse>> Nearby([FromQuery] NearbyStationsQuery query, CancellationToken cancellationToken) =>
        Ok(await stations.NearbyAsync(query, cancellationToken));

    [HttpGet("{id}")]
    public async Task<ActionResult<StationResponse>> Get(string id, CancellationToken cancellationToken) =>
        Ok(await stations.GetAsync(id, cancellationToken));

    [HttpPost, Authorize(Policy = AuthPolicies.BackofficeOnly), ProducesResponseType<StationResponse>(201)]
    public async Task<ActionResult<StationResponse>> Create(CreateStationRequest request, CancellationToken cancellationToken)
    {
        var station = await stations.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = station.Id }, station);
    }

    [HttpPut("{id}"), Authorize(Policy = AuthPolicies.BackofficeOnly)]
    public async Task<ActionResult<StationResponse>> Update(string id, UpdateStationRequest request, CancellationToken cancellationToken) =>
        Ok(await stations.UpdateAsync(id, request, cancellationToken));

    [HttpPatch("{id}/deactivate"), Authorize(Policy = AuthPolicies.BackofficeOnly)]
    public async Task<ActionResult<StationResponse>> Deactivate(string id, CancellationToken cancellationToken) =>
        Ok(await stations.DeactivateAsync(id, cancellationToken));

    [HttpGet("{id}/schedule")]
    public async Task<ActionResult<OperatingScheduleResponse>> Schedule(string id, CancellationToken cancellationToken) =>
        Ok((await stations.GetAsync(id, cancellationToken)).OperatingSchedule);

    [HttpPut("{id}/schedule"), Authorize(Policy = AuthPolicies.BackofficeOnly)]
    public async Task<ActionResult<StationResponse>> UpdateSchedule(string id, OperatingScheduleRequest request, CancellationToken cancellationToken) =>
        Ok(await stations.UpdateScheduleAsync(id, request, cancellationToken));

    [HttpPatch("{id}/availability"), Authorize(Policy = AuthPolicies.Staff)]
    public async Task<ActionResult<StationResponse>> Availability(string id, StationAvailabilityRequest request, CancellationToken cancellationToken) =>
        Ok(await stations.UpdateAvailabilityAsync(id, request, cancellationToken));
}
