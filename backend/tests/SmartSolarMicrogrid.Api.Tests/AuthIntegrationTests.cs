using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs.Auth;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Repositories;
using SmartSolarMicrogrid.Api.Services;
using Xunit;

namespace SmartSolarMicrogrid.Api.Tests;

public sealed class MongoTheoryAttribute : TheoryAttribute
{
    public MongoTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SMARTSOLAR_TEST_MONGODB_URI")))
            Skip = "Set SMARTSOLAR_TEST_MONGODB_URI to run real MongoDB tests.";
    }
}

public sealed class AuthIntegrationTests : IAsyncLifetime
{
    private const string TestPassword = "Phase four test passphrase!";
    private readonly string databaseName = "smartsolar_tests_" + Guid.NewGuid().ToString("N");
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient http = null!;
    private MongoDbContext context = null!;
    private PasswordService passwords = null!;

    public Task InitializeAsync()
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = Environment.GetEnvironmentVariable("SMARTSOLAR_TEST_MONGODB_URI"),
                ["MongoDb:DatabaseName"] = databaseName,
                ["Jwt:SigningKey"] = AuthTestSettings.SigningKey,
                ["Jwt:Issuer"] = "auth-test-issuer", ["Jwt:Audience"] = "auth-test-audience",
                ["Bootstrap:FullName"] = "Initial Backoffice", ["Bootstrap:Email"] = "bootstrap@example.invalid",
                ["Bootstrap:Phone"] = "0771234567", ["Bootstrap:Password"] = TestPassword
            }));
            builder.ConfigureServices(services => services.AddControllers().AddApplicationPart(typeof(AuthRoleProbeController).Assembly));
        });
        http = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        context = factory.Services.GetRequiredService<MongoDbContext>();
        passwords = factory.Services.GetRequiredService<PasswordService>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (context is not null) await context.Database.Client.DropDatabaseAsync(databaseName);
        }
        finally
        {
            http?.Dispose();
            if (factory is not null) await factory.DisposeAsync();
        }
    }

    [MongoFact]
    public async Task Registration_creates_pending_prosumer_with_salted_hash_and_no_token()
    {
        using var response = await http.PostAsJsonAsync("/api/auth/prosumer/register", Registration());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        var dto = (await response.Content.ReadFromJsonAsync<AuthUserResponse>())!;
        Assert.Equal("PROSUMER", dto.Role);
        Assert.Equal("PENDING", dto.Status);
        Assert.Equal("991234567V", dto.NIC);
        var stored = await context.Users.Find(x => x.NIC == dto.NIC).SingleAsync();
        Assert.NotEqual(TestPassword, stored.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, passwords.Verify(stored, TestPassword));
        Assert.NotEqual(stored.PasswordHash, passwords.Hash(stored, TestPassword));
        using var denied = await http.PostAsJsonAsync("/api/auth/login", new LoginRequest { Identifier = dto.NIC!, Password = TestPassword });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [MongoTheory]
    [InlineData("nic", "invalid")]
    [InlineData("email", "not-an-email")]
    [InlineData("password", "short")]
    [InlineData("fullName", " ")]
    [InlineData("phone", "abc")]
    [InlineData("role", "BACKOFFICE")]
    [InlineData("status", "ACTIVE")]
    [InlineData("id", "000000000000000000000001")]
    public async Task Registration_rejects_invalid_input_and_privilege_injection(string field, string value)
    {
        var body = Registration();
        body[field] = value;
        using var response = await http.PostAsJsonAsync("/api/auth/prosumer/register", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, await context.Users.CountDocumentsAsync(FilterDefinition<User>.Empty));
    }

    [MongoFact]
    public async Task Concurrent_duplicate_registration_returns_one_created_and_one_conflict()
    {
        var body = Registration();
        var responses = await Task.WhenAll(
            http.PostAsJsonAsync("/api/auth/prosumer/register", body),
            http.PostAsJsonAsync("/api/auth/prosumer/register", body));
        try
        {
            Assert.Equal(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }, responses.Select(x => x.StatusCode).OrderBy(x => x));
            Assert.Equal(1, await context.Users.CountDocumentsAsync(FilterDefinition<User>.Empty));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [MongoFact]
    public async Task Email_unique_index_is_case_insensitive()
    {
        var first = Registration();
        first["email"] = "MixedCase@example.invalid";
        using var created = await http.PostAsJsonAsync("/api/auth/prosumer/register", first);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var second = Registration();
        second["nic"] = "200012345678";
        second["email"] = "MIXEDCASE@EXAMPLE.INVALID";
        using var duplicate = await http.PostAsJsonAsync("/api/auth/prosumer/register", second);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [MongoTheory]
    [InlineData(UserRole.BACKOFFICE)]
    [InlineData(UserRole.GRID_OPERATOR)]
    [InlineData(UserRole.PROSUMER)]
    public async Task Active_roles_can_login_and_me_uses_authenticated_identity(UserRole role)
    {
        var user = await SeedUser(role);
        var login = await Login(user.Email.ToUpperInvariant());
        Assert.Equal(role.ToString(), login.User.Role);
        Assert.Equal("Bearer", login.TokenType);
        Assert.InRange(login.ExpiresAtUtc, DateTimeOffset.UtcNow.AddMinutes(29), DateTimeOffset.UtcNow.AddMinutes(31));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);
        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(role.ToString(), jwt.Claims.Single(x => x.Type == "role").Value);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        using var response = await http.GetAsync("/api/auth/me?nic=someone-else&userId=000000000000000000000001");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var me = (await response.Content.ReadFromJsonAsync<AuthUserResponse>())!;
        Assert.Equal(user.Id.ToString(), me.Id);
        Assert.DoesNotContain("passwordHash", await response.Content.ReadAsStringAsync());
        if (role == UserRole.PROSUMER) Assert.Equal(user.Id.ToString(), (await Login(user.NIC!.ToLowerInvariant())).User.Id);
    }

    [MongoTheory]
    [InlineData(UserStatus.PENDING)]
    [InlineData(UserStatus.DEACTIVATED)]
    public async Task Non_active_accounts_cannot_login(UserStatus status)
    {
        var user = await SeedUser(UserRole.PROSUMER, status);
        using var response = await http.PostAsJsonAsync("/api/auth/login", new LoginRequest { Identifier = user.Email, Password = TestPassword });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [MongoFact]
    public async Task Wrong_password_and_unknown_account_return_the_same_generic_401()
    {
        var user = await SeedUser(UserRole.PROSUMER);
        using var wrong = await http.PostAsJsonAsync("/api/auth/login", new LoginRequest { Identifier = user.Email, Password = "wrong password" });
        using var unknown = await http.PostAsJsonAsync("/api/auth/login", new LoginRequest { Identifier = "missing@example.invalid", Password = TestPassword });
        foreach (var response in new[] { wrong, unknown })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString());
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Invalid identifier or password.", body.GetProperty("title").GetString());
        }
    }

    [MongoTheory]
    [InlineData(UserRole.BACKOFFICE, "backoffice", 200)]
    [InlineData(UserRole.GRID_OPERATOR, "backoffice", 403)]
    [InlineData(UserRole.PROSUMER, "backoffice", 403)]
    [InlineData(UserRole.BACKOFFICE, "operator", 403)]
    [InlineData(UserRole.GRID_OPERATOR, "operator", 200)]
    [InlineData(UserRole.PROSUMER, "operator", 403)]
    [InlineData(UserRole.BACKOFFICE, "prosumer", 403)]
    [InlineData(UserRole.GRID_OPERATOR, "prosumer", 403)]
    [InlineData(UserRole.PROSUMER, "prosumer", 200)]
    [InlineData(UserRole.BACKOFFICE, "staff", 200)]
    [InlineData(UserRole.GRID_OPERATOR, "staff", 200)]
    [InlineData(UserRole.PROSUMER, "staff", 403)]
    public async Task Role_policies_enforce_the_permission_matrix(UserRole role, string path, int status)
    {
        var user = await SeedUser(role);
        var login = await Login(user.Email);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        using var response = await http.GetAsync("/test-auth/" + path);
        Assert.Equal(status, (int)response.StatusCode);
        if (status == 403) Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [MongoTheory]
    [InlineData("/api/auth/me")]
    [InlineData("/test-auth/default")]
    [InlineData("/test-auth/backoffice")]
    public async Task Protected_routes_require_a_token(string path)
    {
        using var response = await http.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [MongoTheory]
    [InlineData("signature")]
    [InlineData("expired")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("role")]
    [InlineData("nic")]
    [InlineData("unsigned")]
    [InlineData("future")]
    public async Task Invalid_tokens_are_rejected(string kind)
    {
        var user = await SeedUser(UserRole.PROSUMER);
        var key = kind == "signature" ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) : AuthTestSettings.SigningKey;
        var claims = new[] { new Claim("sub", user.Id.ToString()), new Claim("ver", "0"),
            new Claim("role", kind == "role" ? "BACKOFFICE" : "PROSUMER"), new Claim("nic", kind == "nic" ? "other-nic" : user.NIC!) };
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(kind == "issuer" ? "wrong" : "auth-test-issuer",
            kind == "audience" ? "wrong" : "auth-test-audience", claims,
            kind == "future" ? now.AddHours(1) : now.AddHours(-1),
            kind == "expired" ? now.AddMinutes(-1) : now.AddHours(2),
            kind == "unsigned" ? null : new SigningCredentials(new SymmetricSecurityKey(Convert.FromBase64String(key)), SecurityAlgorithms.HmacSha256));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        using var response = await http.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [MongoTheory]
    [InlineData("deactivated")]
    [InlineData("pending")]
    [InlineData("deleted")]
    [InlineData("role")]
    [InlineData("version")]
    public async Task Existing_token_cannot_bypass_database_changes(string change)
    {
        var user = await SeedUser(UserRole.PROSUMER);
        var login = await Login(user.Email);
        if (change == "deleted") await context.Users.DeleteOneAsync(x => x.Id == user.Id);
        else
        {
            var update = change switch
            {
                "role" => Builders<User>.Update.Set(x => x.Role, UserRole.GRID_OPERATOR),
                "version" => Builders<User>.Update.Inc(x => x.TokenVersion, 1),
                "pending" => Builders<User>.Update.Set(x => x.Status, UserStatus.PENDING),
                _ => Builders<User>.Update.Set(x => x.Status, UserStatus.DEACTIVATED)
            };
            await context.Users.UpdateOneAsync(x => x.Id == user.Id, update);
        }
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        using var response = await http.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [MongoFact]
    public async Task Deactivation_requested_account_can_sign_in_until_deactivation_is_approved()
    {
        var user = await SeedUser(UserRole.PROSUMER, UserStatus.DEACTIVATION_REQUESTED);
        Assert.Equal(user.Id.ToString(), (await Login(user.Email)).User.Id);
    }

    [MongoFact]
    public async Task Login_upgrades_older_password_hash()
    {
        var user = await SeedUser(UserRole.PROSUMER);
        var legacyHash = new PasswordHasher<User>(Options.Create(new PasswordHasherOptions { IterationCount = 1000 })).HashPassword(user, TestPassword);
        await context.Users.UpdateOneAsync(x => x.Id == user.Id, Builders<User>.Update.Set(x => x.PasswordHash, legacyHash));
        await Login(user.Email);
        var updated = await context.Users.Find(x => x.Id == user.Id).SingleAsync();
        Assert.NotEqual(legacyHash, updated.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, passwords.Verify(updated, TestPassword));
    }

    [MongoFact]
    public async Task Auth_rate_limit_returns_429_with_retry_header()
    {
        for (var i = 0; i < 10; i++)
        {
            using var denied = await http.PostAsJsonAsync("/api/auth/login", new LoginRequest { Identifier = "unknown", Password = "wrong" });
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        }
        using var limited = await http.PostAsJsonAsync("/api/auth/login", new LoginRequest { Identifier = "unknown", Password = "wrong" });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.NotNull(limited.Headers.RetryAfter);
    }

    [MongoFact]
    public async Task Bootstrap_creates_one_active_backoffice_and_never_overwrites_it()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var bootstrap = scope.ServiceProvider.GetRequiredService<BackofficeBootstrapService>();
        await bootstrap.CreateAsync(CancellationToken.None);
        var user = await context.Users.Find(x => x.Role == UserRole.BACKOFFICE).SingleAsync();
        Assert.Equal(UserStatus.ACTIVE, user.Status);
        Assert.Null(user.NIC);
        Assert.Equal(PasswordVerificationResult.Success, passwords.Verify(user, TestPassword));
        var error = await Assert.ThrowsAsync<ApiException>(() => bootstrap.CreateAsync(CancellationToken.None));
        Assert.Equal(409, error.StatusCode);
        Assert.Equal(user.Id.ToString(), (await Login(user.Email)).User.Id);
    }

    [MongoFact]
    public async Task OpenApi_has_Bearer_security_only_on_protected_operations()
    {
        var document = await http.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        Assert.Equal("bearer", document.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString());
        var paths = document.GetProperty("paths");
        Assert.NotEmpty(paths.GetProperty("/api/auth/me").GetProperty("get").GetProperty("security").EnumerateArray());
        Assert.False(paths.GetProperty("/api/auth/login").GetProperty("post").TryGetProperty("security", out _));
        Assert.False(paths.TryGetProperty("/test-auth/backoffice", out _));
    }

    private static Dictionary<string, string> Registration() => new()
    {
        ["nic"] = "991234567v", ["fullName"] = "Test Prosumer", ["phone"] = "0771234567",
        ["email"] = $"{Guid.NewGuid():N}@example.invalid", ["password"] = TestPassword
    };

    private async Task<User> SeedUser(UserRole role, UserStatus status = UserStatus.ACTIVE)
    {
        var user = MongoModelTests.NewUser(role == UserRole.PROSUMER ? "991234567V" : null);
        user.Role = role; user.Status = status; user.PasswordHash = passwords.Hash(user, TestPassword);
        await context.Users.InsertOneAsync(user);
        return user;
    }

    private async Task<LoginResponse> Login(string identifier)
    {
        using var response = await http.PostAsJsonAsync("/api/auth/login", new LoginRequest { Identifier = identifier, Password = TestPassword });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }
}

// Test assembly only: demonstrates reusable policies without shipping placeholder business endpoints.
[ApiController, Route("test-auth"), ApiExplorerSettings(IgnoreApi = true)]
public sealed class AuthRoleProbeController : ControllerBase
{
    [HttpGet("default")]
    public IActionResult Default() => Ok();
    [HttpGet("backoffice"), Authorize(Policy = AuthPolicies.BackofficeOnly)]
    public IActionResult Backoffice() => Ok();
    [HttpGet("operator"), Authorize(Policy = AuthPolicies.OperatorOnly)]
    public IActionResult Operator() => Ok();
    [HttpGet("prosumer"), Authorize(Policy = AuthPolicies.ProsumerOnly)]
    public IActionResult Prosumer() => Ok();
    [HttpGet("staff"), Authorize(Policy = AuthPolicies.Staff)]
    public IActionResult Staff() => Ok();
}
