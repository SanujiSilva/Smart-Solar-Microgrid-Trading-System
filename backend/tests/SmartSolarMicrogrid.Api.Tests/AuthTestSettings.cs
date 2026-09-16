using System.Security.Cryptography;

namespace SmartSolarMicrogrid.Api.Tests;

internal static class AuthTestSettings
{
    // Ephemeral test key; never shares application user secrets or production tokens.
    internal static readonly string SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
