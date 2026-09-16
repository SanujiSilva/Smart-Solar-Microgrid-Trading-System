using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs.Reservations;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController, Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class QrController(QrTransactionService qr) : ControllerBase
{
    [HttpGet("api/reservations/{id}/qr"), Authorize(Policy = AuthPolicies.ProsumerOnly)]
    public async Task<ActionResult<QrTokenResponse>> Issue(string id, CancellationToken cancellationToken) =>
        Ok(await qr.IssueAsync(id, cancellationToken));

    [HttpPost("api/operator/verify-qr"), Authorize(Policy = AuthPolicies.OperatorOnly)]
    public async Task<ActionResult<QrVerificationResponse>> Verify(QrTokenRequest request,
        CancellationToken cancellationToken) => Ok(await qr.VerifyAsync(request, cancellationToken));

    [HttpPost("api/operator/complete-transfer"), Authorize(Policy = AuthPolicies.OperatorOnly)]
    public async Task<ActionResult<ReservationResponse>> Complete(QrTokenRequest request,
        CancellationToken cancellationToken) => Ok(await qr.CompleteAsync(request, cancellationToken));
}
