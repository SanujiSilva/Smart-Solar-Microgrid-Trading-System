/*
 * File: src/SmartSolarMicrogrid.Api/DTOs/Users/UserResponses.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: API request/response contracts and validation for User Responses.
 */
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.DTOs.Users;

public sealed record UserDetailsResponse(string Id, string? NIC, string FullName, string Email,
    string Phone, string Role, string Status, DateTime CreatedAt, DateTime UpdatedAt)
{
    // Map the stored model to the public response without exposing internal state.
    public static UserDetailsResponse From(User user) => new(user.Id.ToString(), user.NIC, user.FullName,
        user.Email, user.Phone, user.Role.ToString(), user.Status.ToString(), user.CreatedAt, user.UpdatedAt);
}

public sealed record UserPageResponse(IReadOnlyList<UserDetailsResponse> Items, long TotalCount, int Page, int PageSize);
