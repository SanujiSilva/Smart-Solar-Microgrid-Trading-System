/*
 * File: src/SmartSolarMicrogrid.Api/Controllers/ProsumersController.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: HTTP endpoints and authorization boundaries for Prosumers.
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs.Users;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController, Route("api/prosumers")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProsumersController(UserManagementService users) : ControllerBase
{
    // List for Prosumers.
    [HttpGet, Authorize(Policy = AuthPolicies.BackofficeOnly)]
    public async Task<ActionResult<UserPageResponse>> List([FromQuery] UserListQuery query, CancellationToken cancellationToken) =>
        Ok(await users.ListAsync(query, true, cancellationToken));

    // Get for Prosumers.
    [HttpGet("{nic}"), Authorize(Policy = AuthPolicies.BackofficeOnly)]
    public async Task<ActionResult<UserDetailsResponse>> Get(string nic, CancellationToken cancellationToken) =>
        Ok(await users.GetProsumerAsync(nic, cancellationToken));

    // Update Me for Prosumers.
    [HttpPut("me"), Authorize(Policy = AuthPolicies.ProsumerOnly)]
    public async Task<ActionResult<UserDetailsResponse>> UpdateMe(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await users.UpdateMyProfileAsync(request, cancellationToken));

    // Request Deactivation for Prosumers.
    [HttpPost("me/deactivation-request"), Authorize(Policy = AuthPolicies.ProsumerOnly)]
    public async Task<ActionResult<UserDetailsResponse>> RequestDeactivation(CancellationToken cancellationToken) =>
        Ok(await users.RequestDeactivationAsync(cancellationToken));

    // Activate for Prosumers.
    [HttpPatch("{nic}/activate"), Authorize(Policy = AuthPolicies.BackofficeOnly)]
    public async Task<ActionResult<UserDetailsResponse>> Activate(string nic, CancellationToken cancellationToken) =>
        Ok(await users.ActivateProsumerAsync(nic, cancellationToken));
}
