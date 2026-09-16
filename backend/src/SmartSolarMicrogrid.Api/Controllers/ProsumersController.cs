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
    [HttpGet, Authorize(Policy = AuthPolicies.BackofficeOnly)]
    public async Task<ActionResult<UserPageResponse>> List([FromQuery] UserListQuery query, CancellationToken cancellationToken) =>
        Ok(await users.ListAsync(query, true, cancellationToken));

    [HttpGet("{nic}"), Authorize(Policy = AuthPolicies.BackofficeOnly)]
    public async Task<ActionResult<UserDetailsResponse>> Get(string nic, CancellationToken cancellationToken) =>
        Ok(await users.GetProsumerAsync(nic, cancellationToken));

    [HttpPut("me"), Authorize(Policy = AuthPolicies.ProsumerOnly)]
    public async Task<ActionResult<UserDetailsResponse>> UpdateMe(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await users.UpdateMyProfileAsync(request, cancellationToken));

    [HttpPost("me/deactivation-request"), Authorize(Policy = AuthPolicies.ProsumerOnly)]
    public async Task<ActionResult<UserDetailsResponse>> RequestDeactivation(CancellationToken cancellationToken) =>
        Ok(await users.RequestDeactivationAsync(cancellationToken));

    [HttpPatch("{nic}/activate"), Authorize(Policy = AuthPolicies.BackofficeOnly)]
    public async Task<ActionResult<UserDetailsResponse>> Activate(string nic, CancellationToken cancellationToken) =>
        Ok(await users.ActivateProsumerAsync(nic, cancellationToken));
}
