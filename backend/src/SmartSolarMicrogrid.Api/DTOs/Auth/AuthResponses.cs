/*
 * File: src/SmartSolarMicrogrid.Api/DTOs/Auth/AuthResponses.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: API request/response contracts and validation for Auth Responses.
 */
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.DTOs.Auth;

public sealed record AuthUserResponse(string Id, string? NIC, string FullName, string Email,
    string Phone, string Role, string Status)
{
    // Map the stored model to the public response without exposing internal state.
    public static AuthUserResponse From(User user) => new(user.Id.ToString(), user.NIC,
        user.FullName, user.Email, user.Phone, user.Role.ToString(), user.Status.ToString());
}

public sealed record LoginResponse(string AccessToken, string TokenType,
    DateTimeOffset ExpiresAtUtc, AuthUserResponse User);
