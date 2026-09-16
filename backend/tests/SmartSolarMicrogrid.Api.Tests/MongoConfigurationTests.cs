using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartSolarMicrogrid.Api.Configuration;
using Xunit;

namespace SmartSolarMicrogrid.Api.Tests;

public sealed class MongoConfigurationTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("http://localhost:27017", false)]
    [InlineData("mongodb://127.0.0.1:27017", true)]
    [InlineData("mongodb+srv://example.invalid", true)]
    public void Connection_string_validation(string uri, bool valid) =>
        Assert.Equal(valid, MongoDbSettings.IsValidConnectionString(uri));

    [Theory]
    [InlineData("SmartSolarMicrogrid", true)]
    [InlineData("smartsolar_tests_123", true)]
    [InlineData("bad.name", false)]
    [InlineData("bad/name", false)]
    [InlineData("", false)]
    [InlineData("admin", false)]
    [InlineData("LOCAL", false)]
    public void Database_name_validation(string name, bool valid) =>
        Assert.Equal(valid, MongoDbSettings.IsValidDatabaseName(name));

    [Fact]
    public void Invalid_configuration_errors_do_not_include_connection_secrets()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MongoDb:ConnectionString"] = "http://private-user:private-password@localhost",
            ["MongoDb:TimeoutSeconds"] = "0"
        }).Build();
        var services = new ServiceCollection();
        services.AddMongoDatabase(config);
        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<MongoDbSettings>>().Value);
        Assert.DoesNotContain("private-password", exception.Message);
        Assert.Contains("MongoDb:ConnectionString", exception.Message);
        Assert.Contains("MongoDb:TimeoutSeconds", exception.Message);
    }

    [Fact]
    public async Task Database_outage_does_not_break_liveness_and_returns_safe_503_readiness()
    {
        await using var factory = CreateUnavailableFactory(skipInitialization: true);
        using var client = factory.CreateClient();
        using var live = await client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        using var ready = await client.GetAsync("/api/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        var body = await ready.Content.ReadAsStringAsync();
        Assert.Contains("Unhealthy", body);
        Assert.DoesNotContain("127.0.0.1", body);
        Assert.DoesNotContain("mongodb://", body);
    }

    [Fact]
    public async Task Unreachable_database_prevents_application_startup()
    {
        await using var factory = CreateUnavailableFactory(skipInitialization: false);
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("MongoDB initialization failed", exception.Message);
        Assert.DoesNotContain("127.0.0.1", exception.Message);
    }

    private static WebApplicationFactory<Program> CreateUnavailableFactory(bool skipInitialization) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["MongoDb:ConnectionString"] = "mongodb://127.0.0.1:1",
                    ["MongoDb:TimeoutSeconds"] = "1",
                    ["MongoDb:InitializationTimeoutSeconds"] = "2"
                }));
            if (skipInitialization)
                builder.ConfigureServices(services => services.Remove(
                    services.Single(x => x.ImplementationType == typeof(MongoDatabaseInitializer))));
        });
}
