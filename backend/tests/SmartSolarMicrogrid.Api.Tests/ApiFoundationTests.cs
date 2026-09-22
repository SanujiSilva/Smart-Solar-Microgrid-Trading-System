/*
 * File: tests/SmartSolarMicrogrid.Api.Tests/ApiFoundationTests.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Automated verification and test support for Api Foundation Tests.
 */
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SmartSolarMicrogrid.Api.Configuration;
using Xunit;

namespace SmartSolarMicrogrid.Api.Tests;

public sealed class ApiFoundationTests
{
    [Fact]
    public async Task Health_returns_live_utc_response()
    {
        // Verify that health returns live utc response.
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);
        var before = DateTimeOffset.UtcNow;
        using var response = await client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJson(response);
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
        Assert.InRange(body.GetProperty("timestampUtc").GetDateTimeOffset(), before, DateTimeOffset.UtcNow);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Unknown_route_returns_problem_details_with_trace_id()
    {
        // Verify that unknown route returns problem details with trace id.
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/api/missing");
        var body = await AssertProblem(response, HttpStatusCode.NotFound);
        Assert.Equal("/api/missing", body.GetProperty("instance").GetString());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{bad json")]
    public async Task Invalid_input_returns_structured_validation_errors(string json)
    {
        // Verify that invalid input returns structured validation errors.
        await using var factory = CreateFactory(includeTestController: true);
        using var client = CreateClient(factory);
        using var response = await client.PostAsync("/test-probe/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));
        var body = await AssertProblem(response, HttpStatusCode.BadRequest);
        Assert.NotEmpty(body.GetProperty("errors").EnumerateObject());
    }

    [Theory]
    [InlineData("Development", "application/json")]
    [InlineData("Production", "application/json")]
    [InlineData("Development", "text/html")]
    [InlineData("Production", "text/html")]
    public async Task Unhandled_exception_returns_safe_problem_details(string environment, string accept)
    {
        // Verify that unhandled exception returns safe problem details.
        await using var factory = CreateFactory(environment, includeTestController: true);
        using var client = CreateClient(factory);
        client.DefaultRequestHeaders.Accept.ParseAdd(accept);
        using var response = await client.GetAsync("/test-probe/throw");
        await AssertProblem(response, HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("sensitive-test-exception", body);
        Assert.DoesNotContain("stackTrace", body);
    }

    [Fact]
    public async Task Development_exposes_OpenApi_and_Swagger()
    {
        // Verify that development exposes openapi and swagger.
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJson(response);
        Assert.True(body.GetProperty("paths").TryGetProperty("/api/health", out _));
        Assert.False(body.GetProperty("paths").TryGetProperty("/test-probe/throw", out _));
        using var swagger = await client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
        Assert.Contains("swagger-ui", await swagger.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/swagger/index.html")]
    public async Task Production_does_not_expose_API_documentation(string path)
    {
        // Verify that production does not expose api documentation.
        await using var factory = CreateFactory("Production");
        using var client = CreateClient(factory);
        using var response = await client.GetAsync(path);
        await AssertProblem(response, HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("http://localhost:5173", true)]
    [InlineData("https://untrusted.example", false)]
    public async Task Cors_only_allows_configured_browser_origins(string origin, bool allowed)
    {
        // Verify that cors only allows configured browser origins.
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        using var response = await client.SendAsync(request);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        if (allowed)
        {
            Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        }
    }

    // Create Factory for Api Foundation Tests.
    private static WebApplicationFactory<Program> CreateFactory(
        string environment = "Development", bool includeTestController = false) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["MongoDb:ConnectionString"] = "mongodb://127.0.0.1:1", ["Jwt:SigningKey"] = AuthTestSettings.SigningKey }));
            builder.ConfigureServices(services =>
            {
                // Foundation tests exercise HTTP infrastructure without a database dependency.
                var initializer = services.Single(x => x.ImplementationType == typeof(MongoDatabaseInitializer));
                services.Remove(initializer);
            });
            if (includeTestController)
            {
                builder.ConfigureServices(services => services.AddControllers()
                    .AddApplicationPart(typeof(FoundationProbeController).Assembly));
            }
        });

    // Create Client for Api Foundation Tests.
    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

    // Read Json for Api Foundation Tests.
    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static async Task<JsonElement> AssertProblem(HttpResponseMessage response, HttpStatusCode status)
    {
        // Assert Problem for Api Foundation Tests.
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await ReadJson(response);
        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
        return body;
    }
}

// Loaded explicitly by tests only; these routes never ship in the API assembly.
[ApiController]
[AllowAnonymous]
[Route("test-probe")]
public sealed class FoundationProbeController : ControllerBase
{
    // Throw for Api Foundation Tests.
    [HttpGet("throw")]
    public IActionResult Throw() => throw new InvalidOperationException("sensitive-test-exception");

    // Validate for Api Foundation Tests.
    [HttpPost("validate")]
    public IActionResult Validate(ProbeRequest request) => Ok();
}

public sealed class ProbeRequest
{
    [Required]
    public string? Name { get; init; }
}
