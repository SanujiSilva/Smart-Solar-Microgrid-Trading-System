using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs.Users;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController, Route("api/users"), Authorize(Policy = AuthPolicies.BackofficeOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UsersController(UserManagementService users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserPageResponse>> List([FromQuery] UserListQuery query, CancellationToken cancellationToken) =>
        Ok(await users.ListAsync(query, false, cancellationToken));

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDetailsResponse>> Get(string id, CancellationToken cancellationToken) =>
        Ok(await users.GetAsync(id, cancellationToken));

    [HttpPost, ProducesResponseType<UserDetailsResponse>(201)]
    public async Task<ActionResult<UserDetailsResponse>> Create(CreateStaffRequest request, CancellationToken cancellationToken)
    {
        var user = await users.CreateStaffAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDetailsResponse>> Update(string id, UpdateProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await users.UpdateStaffAsync(id, request, cancellationToken));

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<UserDetailsResponse>> ChangeStatus(string id, ChangeUserStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await users.ChangeStatusAsync(id, request, cancellationToken));
}
