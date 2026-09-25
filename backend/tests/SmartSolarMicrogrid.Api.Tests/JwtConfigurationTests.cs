/*
 * File: tests/SmartSolarMicrogrid.Api.Tests/JwtConfigurationTests.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Automated verification and test support for Jwt Configuration Tests.
 */
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartSolarMicrogrid.Api.Configuration;
using Xunit;

namespace SmartSolarMicrogrid.Api.Tests;

public sealed class JwtConfigurationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("YWJj")]
    public void Invalid_signing_keys_are_rejected_without_exposing_values(string key)
    {
        // Verify that invalid signing keys are rejected without exposing values.
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Jwt:Issuer"] = "test", ["Jwt:Audience"] = "test", ["Jwt:SigningKey"] = key }).Build();
        var services = new ServiceCollection().AddApiAuthentication(config);
        using var provider = services.BuildServiceProvider();
        var error = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<JwtSettings>>().Value);
        Assert.Contains("Jwt:SigningKey", error.Message);
        if (key.Length > 0) Assert.DoesNotContain(key, error.Message);
    }
}
