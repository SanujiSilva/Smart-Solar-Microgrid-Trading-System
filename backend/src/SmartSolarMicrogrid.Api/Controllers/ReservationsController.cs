/*
 * File: src/SmartSolarMicrogrid.Api/Controllers/ReservationsController.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: HTTP endpoints and authorization boundaries for Reservations.
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs.Reservations;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController, Route("api/reservations"), Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ReservationsController(ReservationService reservations) : ControllerBase
{
    [HttpPost, Authorize(Policy = AuthPolicies.ProsumerOnly)]
    [ProducesResponseType<ReservationResponse>(201)]
    public async Task<ActionResult<ReservationResponse>> Create(CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        // Create for Reservations.
        var reservation = await reservations.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = reservation.Id }, reservation);
    }

    // Get for Reservations.
    [HttpGet("{id}")]
    public async Task<ActionResult<ReservationResponse>> Get(string id, CancellationToken cancellationToken) =>
        Ok(await reservations.GetAsync(id, cancellationToken));

    // My for Reservations.
    [HttpGet("my"), Authorize(Policy = AuthPolicies.ProsumerOnly)]
    public async Task<ActionResult<ReservationListResponse>> My(CancellationToken cancellationToken) =>
        Ok(await reservations.MyAsync(cancellationToken));

    // History for Reservations.
    [HttpGet("history"), Authorize(Policy = AuthPolicies.ProsumerOnly)]
    public async Task<ActionResult<ReservationListResponse>> History(CancellationToken cancellationToken) =>
        Ok(await reservations.HistoryAsync(cancellationToken));

    // Pending for Reservations.
    [HttpGet("pending"), Authorize(Policy = AuthPolicies.Staff)]
    public async Task<ActionResult<ReservationListResponse>> Pending(CancellationToken cancellationToken) =>
        Ok(await reservations.PendingAsync(cancellationToken));

    // Update for Reservations.
    [HttpPut("{id}"), Authorize(Policy = AuthPolicies.ProsumerOnly)]
    public async Task<ActionResult<ReservationResponse>> Update(string id, UpdateReservationRequest request,
        CancellationToken cancellationToken) => Ok(await reservations.UpdateAsync(id, request, cancellationToken));

    // Approve for Reservations.
    [HttpPatch("{id}/approve"), Authorize(Policy = AuthPolicies.BackofficeOnly)]
    public async Task<ActionResult<ReservationResponse>> Approve(string id, CancellationToken cancellationToken) =>
        Ok(await reservations.ReviewAsync(id, true, cancellationToken));

    // Reject for Reservations.
    [HttpPatch("{id}/reject"), Authorize(Policy = AuthPolicies.BackofficeOnly)]
    public async Task<ActionResult<ReservationResponse>> Reject(string id, CancellationToken cancellationToken) =>
        Ok(await reservations.ReviewAsync(id, false, cancellationToken));

    // Delete for Reservations.
    [HttpDelete("{id}")]
    public async Task<ActionResult<ReservationResponse>> Delete(string id, CancellationToken cancellationToken) =>
        Ok(await reservations.CancelAsync(id, cancellationToken));
}
