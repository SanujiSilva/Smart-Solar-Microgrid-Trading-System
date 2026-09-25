/*
 * File: src/SmartSolarMicrogrid.Api/Controllers/UsersController.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: HTTP endpoints and authorization boundaries for Users.
 */
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
    // List for Users.
    [HttpGet]
    public async Task<ActionResult<UserPageResponse>> List([FromQuery] UserListQuery query, CancellationToken cancellationToken) =>
        Ok(await users.ListAsync(query, false, cancellationToken));

    // Get for Users.
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDetailsResponse>> Get(string id, CancellationToken cancellationToken) =>
        Ok(await users.GetAsync(id, cancellationToken));

    [HttpPost, ProducesResponseType<UserDetailsResponse>(201)]
    public async Task<ActionResult<UserDetailsResponse>> Create(CreateStaffRequest request, CancellationToken cancellationToken)
    {
        // Create for Users.
        var user = await users.CreateStaffAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    // Update for Users.
    [HttpPut("{id}")]
    public async Task<ActionResult<UserDetailsResponse>> Update(string id, UpdateProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await users.UpdateStaffAsync(id, request, cancellationToken));

    // Change Status for Users.
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<UserDetailsResponse>> ChangeStatus(string id, ChangeUserStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await users.ChangeStatusAsync(id, request, cancellationToken));
}
