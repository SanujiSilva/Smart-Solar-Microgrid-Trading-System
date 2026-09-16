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
        var reservation = await reservations.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = reservation.Id }, reservation);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ReservationResponse>> Get(string id, CancellationToken cancellationToken) =>
        Ok(await reservations.GetAsync(id, cancellationToken));

    [HttpGet("my"), Authorize(Policy = AuthPolicies.ProsumerOnly)]
    public async Task<ActionResult<ReservationListResponse>> My(CancellationToken cancellationToken) =>
        Ok(await reservations.MyAsync(cancellationToken));

    [HttpGet("history"), Authorize(Policy = AuthPolicies.ProsumerOnly)]
    public async Task<ActionResult<ReservationListResponse>> History(CancellationToken cancellationToken) =>
        Ok(await reservations.HistoryAsync(cancellationToken));

    [HttpGet("pending"), Authorize(Policy = AuthPolicies.Staff)]
    public async Task<ActionResult<ReservationListResponse>> Pending(CancellationToken cancellationToken) =>
        Ok(await reservations.PendingAsync(cancellationToken));

    [HttpPut("{id}"), Authorize(Policy = AuthPolicies.ProsumerOnly)]
    public async Task<ActionResult<ReservationResponse>> Update(string id, UpdateReservationRequest request,
        CancellationToken cancellationToken) => Ok(await reservations.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id}"), Authorize(Policy = AuthPolicies.ProsumerOnly)]
    public async Task<ActionResult<ReservationResponse>> Delete(string id, CancellationToken cancellationToken) =>
        Ok(await reservations.CancelAsync(id, cancellationToken));
}
