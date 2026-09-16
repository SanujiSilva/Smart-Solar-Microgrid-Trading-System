using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.DTOs.Auth;

public sealed record AuthUserResponse(string Id, string? NIC, string FullName, string Email,
    string Phone, string Role, string Status)
{
    public static AuthUserResponse From(User user) => new(user.Id.ToString(), user.NIC,
        user.FullName, user.Email, user.Phone, user.Role.ToString(), user.Status.ToString());
}

public sealed record LoginResponse(string AccessToken, string TokenType,
    DateTimeOffset ExpiresAtUtc, AuthUserResponse User);
