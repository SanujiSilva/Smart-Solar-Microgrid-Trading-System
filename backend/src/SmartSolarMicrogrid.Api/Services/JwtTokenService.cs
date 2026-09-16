using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs.Auth;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class JwtTokenService(IOptions<JwtSettings> options, TimeProvider clock)
{
    public LoginResponse Issue(User user)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(settings.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()), new("jti", Guid.NewGuid().ToString("N")),
            new("role", user.Role.ToString()), new("ver", user.TokenVersion.ToString(CultureInfo.InvariantCulture)),
            new("iat", now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };
        if (user.NIC is not null) claims.Add(new Claim("nic", user.NIC));
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims,
            now.UtcDateTime, expires.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Convert.FromBase64String(settings.SigningKey)), SecurityAlgorithms.HmacSha256));
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", expires, AuthUserResponse.From(user));
    }
}
