/*
 * File: src/SmartSolarMicrogrid.Api/Configuration/JwtBearerEventsHandler.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Service setup and configuration for Jwt Bearer Events Handler.
 */
using System.Globalization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MongoDB.Bson;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Repositories;

namespace SmartSolarMicrogrid.Api.Configuration;

public sealed class JwtBearerEventsHandler(IUserRepository users) : JwtBearerEvents
{
    public const string CurrentUserKey = "AuthenticatedUser";

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        // Token Validated for Jwt Bearer Events Handler.
        var principal = context.Principal!;
        if (!ObjectId.TryParse(principal.FindFirst("sub")?.Value, out var id))
        { context.Fail("Invalid identity."); return; }
        var user = await users.FindByIdAsync(id, context.HttpContext.RequestAborted);
        if (user is null || !AuthPolicies.CanSignIn(user) ||
            principal.FindFirst("role")?.Value != user.Role.ToString() ||
            principal.FindFirst("nic")?.Value != user.NIC ||
            principal.FindFirst("ver")?.Value != user.TokenVersion.ToString(CultureInfo.InvariantCulture))
        { context.Fail("Session is no longer valid."); return; }
        context.HttpContext.Items[CurrentUserKey] = user;
    }
}
