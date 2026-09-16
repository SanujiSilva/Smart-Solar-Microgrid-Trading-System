namespace SmartSolarMicrogrid.Api.Configuration;

public sealed class JwtSettings
{
    public string Issuer { get; init; } = "";
    public string Audience { get; init; } = "";
    public string SigningKey { get; init; } = "";
    public int AccessTokenMinutes { get; init; } = 30;

    public static bool HasValidKey(string? key)
    {
        try { return !string.IsNullOrWhiteSpace(key) && Convert.FromBase64String(key).Length >= 32; }
        catch (FormatException) { return false; }
    }
}
