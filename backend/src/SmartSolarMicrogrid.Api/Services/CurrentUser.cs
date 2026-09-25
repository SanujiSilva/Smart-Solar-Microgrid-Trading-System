/*
 * File: src/SmartSolarMicrogrid.Api/Services/CurrentUser.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Server-side application rules and orchestration for Current User.
 */
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class CurrentUser(IHttpContextAccessor accessor)
{
    // Get for Current User.
    public User Get() => accessor.HttpContext?.Items[JwtBearerEventsHandler.CurrentUserKey] as User
        ?? throw new ApiException(401, "Authentication is required.");

    public User Require(UserRole role)
    {
        // Require for Current User.
        var user = Get();
        if (user.Role != role) throw new ApiException(403, "You do not have permission to perform this action.");
        return user;
    }
}
