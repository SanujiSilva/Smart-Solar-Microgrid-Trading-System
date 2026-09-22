/*
 * File: src/SmartSolarMicrogrid.Api/Controllers/SlotsController.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: HTTP endpoints and authorization boundaries for Slots.
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs.Slots;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController, Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SlotsController(SlotService slots) : ControllerBase
{
    // Availability for Slots.
    [HttpPatch("api/slots/{id}/availability"), Authorize(Policy = AuthPolicies.Staff)]
    public async Task<ActionResult<SlotResponse>> Availability(string id, SlotAvailabilityRequest request,
        CancellationToken cancellationToken) => Ok(await slots.UpdateAvailabilityAsync(id, request, cancellationToken));
    // List for Slots.
    [HttpGet("api/stations/{stationId}/slots")]
    public async Task<ActionResult<SlotListResponse>> List(string stationId, [FromQuery] SlotListQuery query,
        CancellationToken cancellationToken) => Ok(await slots.ListAsync(stationId, query, cancellationToken));

    [HttpPost("api/stations/{stationId}/slots"), Authorize(Policy = AuthPolicies.BackofficeOnly)]
    [ProducesResponseType<SlotResponse>(201)]
    public async Task<ActionResult<SlotResponse>> Create(string stationId, CreateSlotRequest request,
        CancellationToken cancellationToken)
    {
        // Create for Slots.
        var slot = await slots.CreateAsync(stationId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = slot.Id }, slot);
    }

    // Get for Slots.
    [HttpGet("api/slots/{id}")]
    public async Task<ActionResult<SlotResponse>> Get(string id, CancellationToken cancellationToken) =>
        Ok(await slots.GetAsync(id, cancellationToken));

    // Update for Slots.
    [HttpPut("api/slots/{id}"), Authorize(Policy = AuthPolicies.BackofficeOnly)]
    public async Task<ActionResult<SlotResponse>> Update(string id, UpdateSlotRequest request,
        CancellationToken cancellationToken) => Ok(await slots.UpdateAsync(id, request, cancellationToken));

    // Delete for Slots.
    [HttpDelete("api/slots/{id}"), Authorize(Policy = AuthPolicies.BackofficeOnly)]
    public async Task<ActionResult<SlotResponse>> Delete(string id, CancellationToken cancellationToken) =>
        Ok(await slots.CancelAsync(id, cancellationToken));
}
