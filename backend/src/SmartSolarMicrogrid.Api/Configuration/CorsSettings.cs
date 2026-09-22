/*
 * File: src/SmartSolarMicrogrid.Api/Configuration/CorsSettings.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Service setup and configuration for Cors Settings.
 */
namespace SmartSolarMicrogrid.Api.Configuration;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];

    // Is Valid Origin for Cors Settings.
    internal static bool IsValidOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        string.IsNullOrEmpty(uri.UserInfo) &&
        !uri.Host.Contains('*') &&
        string.Equals(origin, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);
}
