/*
 * File: src/SmartSolarMicrogrid.Api/Configuration/JwtSettings.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Service setup and configuration for Jwt Settings.
 */
namespace SmartSolarMicrogrid.Api.Configuration;

public sealed class JwtSettings
{
    public string Issuer { get; init; } = "";
    public string Audience { get; init; } = "";
    public string SigningKey { get; init; } = "";
    public int AccessTokenMinutes { get; init; } = 30;

    public static bool HasValidKey(string? key)
    {
        // Has Valid Key for Jwt Settings.
        try { return !string.IsNullOrWhiteSpace(key) && Convert.FromBase64String(key).Length >= 32; }
        catch (FormatException) { return false; }
    }
}
