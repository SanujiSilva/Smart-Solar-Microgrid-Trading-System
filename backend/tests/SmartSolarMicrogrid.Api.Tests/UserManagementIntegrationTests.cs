/*
 * File: tests/SmartSolarMicrogrid.Api.Tests/UserManagementIntegrationTests.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Automated verification and test support for User Management Integration Tests.
 */
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.DTOs.Auth;
using SmartSolarMicrogrid.Api.DTOs.Users;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Repositories;
using SmartSolarMicrogrid.Api.Services;
using Xunit;

namespace SmartSolarMicrogrid.Api.Tests;

public sealed class UserManagementIntegrationTests : IAsyncLifetime
{
    private const string TestPassword = "Phase five test passphrase!";
    private readonly string databaseName = "ss_test_" + Guid.NewGuid().ToString("N")[..24];
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient admin = null!;
    private User backoffice = null!;
    private MongoDbContext context = null!;
    private PasswordService passwords = null!;

    public async Task InitializeAsync()
    {
        // Create an isolated test database and initialize the API test clients.
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = Environment.GetEnvironmentVariable("SMARTSOLAR_TEST_MONGODB_URI"),
                ["MongoDb:DatabaseName"] = databaseName, ["Jwt:SigningKey"] = AuthTestSettings.SigningKey,
                ["Bootstrap:FullName"] = "Phase Five Backoffice", ["Bootstrap:Email"] = "admin@example.invalid",
                ["Bootstrap:Phone"] = "0771234567", ["Bootstrap:Password"] = TestPassword
            }));
        });
        using var startup = factory.CreateClient();
        context = factory.Services.GetRequiredService<MongoDbContext>();
        passwords = factory.Services.GetRequiredService<PasswordService>();
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<BackofficeBootstrapService>().CreateAsync(CancellationToken.None);
        backoffice = await context.Users.Find(x => x.Role == UserRole.BACKOFFICE).SingleAsync();
        admin = await LoginClient(backoffice.Email);
    }

    public async Task DisposeAsync()
    {
        // Release owned resources and clean up this test or request scope.
        try { if (context is not null) await context.Database.Client.DropDatabaseAsync(databaseName); }
        finally
        {
            admin?.Dispose();
            if (factory is not null) await factory.DisposeAsync();
        }
    }

    [MongoFact]
    public async Task Full_lifecycle_registration_approval_profile_deactivation_and_reactivation()
    {
        // Verify that full lifecycle registration approval profile deactivation and reactivation.
        using var anonymous = factory.CreateClient();
        using var registration = await anonymous.PostAsJsonAsync("/api/auth/prosumer/register", new
        { nic = "991234567v", fullName = "New Prosumer", email = "prosumer@example.invalid", phone = "0771234567", password = TestPassword });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var registered = (await registration.Content.ReadFromJsonAsync<AuthUserResponse>())!;
        var pending = await admin.GetFromJsonAsync<UserPageResponse>("/api/prosumers?status=PENDING");
        Assert.Equal(registered.Id, Assert.Single(pending!.Items).Id);
        using var approval = await admin.PatchAsJsonAsync($"/api/users/{registered.Id}/status", new { status = "ACTIVE" });
        Assert.Equal(HttpStatusCode.OK, approval.StatusCode);
        using var prosumer = await LoginClient("991234567V");
        using var profile = await prosumer.PutAsJsonAsync("/api/prosumers/me", Profile("Updated Prosumer", "new-email@example.invalid"));
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        using var requested = await prosumer.PostAsync("/api/prosumers/me/deactivation-request", null);
        Assert.Equal("DEACTIVATION_REQUESTED", (await requested.Content.ReadFromJsonAsync<UserDetailsResponse>())!.Status);
        using var repeat = await prosumer.PostAsync("/api/prosumers/me/deactivation-request", null);
        Assert.Equal(HttpStatusCode.OK, repeat.StatusCode);
        using var stillActive = await prosumer.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, stillActive.StatusCode);
        using var deactivated = await admin.PatchAsJsonAsync($"/api/users/{registered.Id}/status", new { status = "DEACTIVATED" });
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        using var oldSession = await prosumer.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);
        using var loginDenied = await anonymous.PostAsJsonAsync("/api/auth/login", new { identifier = registered.NIC, password = TestPassword });
        Assert.Equal(HttpStatusCode.Forbidden, loginDenied.StatusCode);
        using var reactivated = await admin.PatchAsync($"/api/prosumers/{registered.NIC}/activate", null);
        Assert.Equal(HttpStatusCode.OK, reactivated.StatusCode);
        using var staleToken = await prosumer.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, staleToken.StatusCode);
        using var newSession = await LoginClient("new-email@example.invalid");
        using var me = await newSession.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var stored = await context.Users.Find(x => x.NIC == registered.NIC).SingleAsync();
        Assert.Equal(3, stored.TokenVersion); // Approval, deactivation, reactivation.
        Assert.Equal("Updated Prosumer", stored.FullName);
    }

    [MongoTheory]
    [InlineData("BACKOFFICE")]
    [InlineData("GRID_OPERATOR")]
    public async Task Backoffice_creates_active_staff_with_safe_response_and_valid_location(string role)
    {
        // Verify that backoffice creates active staff with safe response and valid location.
        using var created = await admin.PostAsJsonAsync("/api/users", new
        { fullName = "New Staff", email = "new-staff@example.invalid", phone = "0771234567", password = TestPassword, role });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var user = (await created.Content.ReadFromJsonAsync<UserDetailsResponse>())!;
        Assert.Equal(role, user.Role);
        Assert.Equal("ACTIVE", user.Status);
        Assert.Null(user.NIC);
        Assert.NotNull(created.Headers.Location);
        using var location = await admin.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, location.StatusCode);
        await AssertSafe(created);
        using var staff = await LoginClient(user.Email);
        using var me = await staff.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var stored = await context.Users.Find(x => x.Id == ObjectId.Parse(user.Id)).SingleAsync();
        Assert.Equal(PasswordVerificationResult.Success, passwords.Verify(stored, TestPassword));
    }

    [MongoFact]
    public async Task Staff_profile_edit_preserves_role_hash_and_normalizes_contacts()
    {
        // Verify that staff profile edit preserves role hash and normalizes contacts.
        var user = await Seed(UserRole.GRID_OPERATOR);
        using var result = await admin.PutAsJsonAsync($"/api/users/{user.Id}", Profile("  New Name  ", " New.Email@Example.Invalid "));
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var stored = await context.Users.Find(x => x.Id == user.Id).SingleAsync();
        Assert.Equal("New Name", stored.FullName);
        Assert.Equal("new.email@example.invalid", stored.Email);
        Assert.Equal(user.PasswordHash, stored.PasswordHash);
        Assert.Equal(user.Role, stored.Role);
        Assert.True(stored.UpdatedAt >= user.CreatedAt.AddMilliseconds(-1));
        await AssertSafe(result);
    }

    [MongoTheory]
    [InlineData(UserRole.GRID_OPERATOR, "GET", "/api/users")]
    [InlineData(UserRole.PROSUMER, "GET", "/api/users")]
    [InlineData(UserRole.GRID_OPERATOR, "GET", "/api/users/{id}")]
    [InlineData(UserRole.PROSUMER, "GET", "/api/users/{id}")]
    [InlineData(UserRole.GRID_OPERATOR, "POST", "/api/users")]
    [InlineData(UserRole.PROSUMER, "POST", "/api/users")]
    [InlineData(UserRole.GRID_OPERATOR, "PUT", "/api/users/{id}")]
    [InlineData(UserRole.PROSUMER, "PUT", "/api/users/{id}")]
    [InlineData(UserRole.GRID_OPERATOR, "PATCH", "/api/users/{id}/status")]
    [InlineData(UserRole.PROSUMER, "PATCH", "/api/users/{id}/status")]
    [InlineData(UserRole.GRID_OPERATOR, "GET", "/api/prosumers")]
    [InlineData(UserRole.PROSUMER, "GET", "/api/prosumers")]
    [InlineData(UserRole.GRID_OPERATOR, "GET", "/api/prosumers/991234567V")]
    [InlineData(UserRole.PROSUMER, "GET", "/api/prosumers/991234567V")]
    [InlineData(UserRole.GRID_OPERATOR, "PATCH", "/api/prosumers/991234567V/activate")]
    [InlineData(UserRole.PROSUMER, "PATCH", "/api/prosumers/991234567V/activate")]
    public async Task Non_backoffice_cannot_access_administration(UserRole role, string method, string path)
    {
        // Verify that non backoffice cannot access administration.
        var user = await Seed(role);
        using var caller = await LoginClient(user.Email);
        using var request = new HttpRequestMessage(new HttpMethod(method), path.Replace("{id}", backoffice.Id.ToString()))
        { Content = JsonContent.Create(new { status = "ACTIVE" }) };
        using var response = await caller.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [MongoTheory]
    [InlineData("/api/users")]
    [InlineData("/api/prosumers")]
    public async Task Administration_requires_authentication(string path)
    {
        // Verify that administration requires authentication.
        using var caller = factory.CreateClient();
        using var response = await caller.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [MongoTheory]
    [InlineData("id")]
    [InlineData("nic")]
    [InlineData("role")]
    [InlineData("status")]
    [InlineData("password")]
    [InlineData("tokenVersion")]
    public async Task Prosumer_cannot_inject_identity_credentials_or_privileges(string field)
    {
        // Verify that prosumer cannot inject identity credentials or privileges.
        var user = await Seed(UserRole.PROSUMER);
        using var caller = await LoginClient(user.Email);
        var body = new Dictionary<string, string>
        { ["fullName"] = "Updated Name", ["email"] = "updated@example.invalid", ["phone"] = "0771234567", [field] = "BACKOFFICE" };
        using var response = await caller.PutAsJsonAsync("/api/prosumers/me", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(user.Email, (await context.Users.Find(x => x.Id == user.Id).SingleAsync()).Email);
    }

    [MongoFact]
    public async Task Own_profile_and_deactivation_ignore_another_users_query_identity()
    {
        // Verify that own profile and deactivation ignore another users query identity.
        var first = await Seed(UserRole.PROSUMER);
        var other = await Seed(UserRole.PROSUMER);
        using var caller = await LoginClient(first.Email);
        var query = $"?nic={other.NIC}&userId={other.Id}";
        using var updated = await caller.PutAsJsonAsync("/api/prosumers/me" + query, Profile("My New Name", first.Email));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(first.Id.ToString(), (await updated.Content.ReadFromJsonAsync<UserDetailsResponse>())!.Id);
        using var requested = await caller.PostAsync("/api/prosumers/me/deactivation-request" + query, null);
        Assert.Equal(HttpStatusCode.OK, requested.StatusCode);
        var untouched = await context.Users.Find(x => x.Id == other.Id).SingleAsync();
        Assert.Equal(other.FullName, untouched.FullName);
        Assert.Equal(UserStatus.ACTIVE, untouched.Status);
    }

    [MongoTheory]
    [InlineData(UserRole.BACKOFFICE)]
    [InlineData(UserRole.GRID_OPERATOR)]
    public async Task Staff_cannot_use_prosumer_self_service(UserRole role)
    {
        // Verify that staff cannot use prosumer self service.
        var user = role == UserRole.BACKOFFICE ? backoffice : await Seed(role);
        using var caller = await LoginClient(user.Email);
        using var profile = await caller.PutAsJsonAsync("/api/prosumers/me", Profile("Some Name", user.Email));
        using var deactivate = await caller.PostAsync("/api/prosumers/me/deactivation-request", null);
        Assert.Equal(HttpStatusCode.Forbidden, profile.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deactivate.StatusCode);
    }

    [MongoFact]
    public async Task List_supports_pagination_role_status_and_literal_search_without_secrets()
    {
        // Verify that list supports pagination role status and literal search without secrets.
        await Seed(UserRole.PROSUMER, UserStatus.PENDING);
        await Seed(UserRole.PROSUMER, UserStatus.PENDING);
        await Seed(UserRole.GRID_OPERATOR);
        using var response = await admin.GetAsync("/api/prosumers?status=PENDING&pageSize=1&page=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var first = (await response.Content.ReadFromJsonAsync<UserPageResponse>())!;
        Assert.Equal(2, first.TotalCount);
        Assert.Single(first.Items);
        var second = (await admin.GetFromJsonAsync<UserPageResponse>("/api/prosumers?status=PENDING&pageSize=1&page=2"))!;
        Assert.NotEqual(first.Items[0].Id, second.Items[0].Id);
        var staff = (await admin.GetFromJsonAsync<UserPageResponse>("/api/users?role=GRID_OPERATOR"))!;
        Assert.Equal("GRID_OPERATOR", Assert.Single(staff.Items).Role);
        var empty = (await admin.GetFromJsonAsync<UserPageResponse>("/api/users?search=%2E%2A"))!;
        Assert.Equal(0, empty.TotalCount);
        var match = (await admin.GetFromJsonAsync<UserPageResponse>("/api/users?search=PHASE%20FIVE"))!;
        Assert.Equal(backoffice.Id.ToString(), Assert.Single(match.Items).Id);
        await AssertSafe(response);
    }

    [MongoTheory]
    [InlineData("/api/users?page=0", 400)]
    [InlineData("/api/users?pageSize=101", 400)]
    [InlineData("/api/users?role=INVALID", 400)]
    [InlineData("/api/users?status=INVALID", 400)]
    [InlineData("/api/users/not-an-objectid", 400)]
    [InlineData("/api/users/000000000000000000000001", 404)]
    [InlineData("/api/prosumers/bad-nic", 400)]
    [InlineData("/api/prosumers/991234567V", 404)]
    [InlineData("/api/prosumers?role=BACKOFFICE", 400)]
    public async Task Invalid_queries_and_missing_accounts_return_meaningful_errors(string path, int status)
    {
        // Verify that invalid queries and missing accounts return meaningful errors.
        using var response = await admin.GetAsync(path);
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [MongoFact]
    public async Task Duplicate_email_is_conflict_on_staff_creation_and_profile_update()
    {
        // Verify that duplicate email is conflict on staff creation and profile update.
        using var duplicate = await admin.PostAsJsonAsync("/api/users", new
        { fullName = "Duplicate Staff", email = backoffice.Email.ToUpperInvariant(), phone = "0771234567", password = TestPassword, role = "GRID_OPERATOR" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var user = await Seed(UserRole.PROSUMER);
        using var caller = await LoginClient(user.Email);
        using var profile = await caller.PutAsJsonAsync("/api/prosumers/me", Profile("Duplicate Email", backoffice.Email.ToUpperInvariant()));
        Assert.Equal(HttpStatusCode.Conflict, profile.StatusCode);
        Assert.Equal(user.Email, (await context.Users.Find(x => x.Id == user.Id).SingleAsync()).Email);
    }

    [MongoFact]
    public async Task Staff_creation_rejects_prosumer_role_and_weak_password()
    {
        // Verify that staff creation rejects prosumer role and weak password.
        using var prosumer = await admin.PostAsJsonAsync("/api/users", new
        { fullName = "Invalid Role", email = "invalid@example.invalid", phone = "0771234567", password = TestPassword, role = "PROSUMER" });
        Assert.Equal(HttpStatusCode.BadRequest, prosumer.StatusCode);
        using var weak = await admin.PostAsJsonAsync("/api/users", new
        { fullName = "Invalid Password", email = "invalid@example.invalid", phone = "0771234567", password = "short", role = "BACKOFFICE" });
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
    }

    [MongoFact]
    public async Task Self_deactivation_and_invalid_status_targets_are_rejected()
    {
        // Verify that self deactivation and invalid status targets are rejected.
        using var self = await admin.PatchAsJsonAsync($"/api/users/{backoffice.Id}/status", new { status = "DEACTIVATED" });
        Assert.Equal(HttpStatusCode.Conflict, self.StatusCode);
        using var invalid = await admin.PatchAsJsonAsync($"/api/users/{backoffice.Id}/status", new { status = "PENDING" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var pendingRole = await admin.PutAsJsonAsync($"/api/users/{backoffice.Id}", new
        { fullName = "Changed Admin", email = backoffice.Email, phone = "0771234567", role = "GRID_OPERATOR" });
        Assert.Equal(HttpStatusCode.BadRequest, pendingRole.StatusCode);
    }

    [MongoFact]
    public async Task Staff_deactivation_revokes_tokens_and_reactivation_requires_new_login()
    {
        // Verify that staff deactivation revokes tokens and reactivation requires new login.
        var user = await Seed(UserRole.GRID_OPERATOR);
        using var caller = await LoginClient(user.Email);
        using var deactivated = await admin.PatchAsJsonAsync($"/api/users/{user.Id}/status", new { status = "DEACTIVATED" });
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        using var denied = await caller.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using var active = await admin.PatchAsJsonAsync($"/api/users/{user.Id}/status", new { status = "ACTIVE" });
        Assert.Equal(HttpStatusCode.OK, active.StatusCode);
        using var stale = await caller.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, stale.StatusCode);
        using var fresh = await LoginClient(user.Email);
        using var ok = await fresh.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [MongoFact]
    public async Task Pending_rejection_and_deactivation_request_decline_are_supported()
    {
        // Verify that pending rejection and deactivation request decline are supported.
        var pending = await Seed(UserRole.PROSUMER, UserStatus.PENDING);
        using var rejected = await admin.PatchAsJsonAsync($"/api/users/{pending.Id}/status", new { status = "DEACTIVATED" });
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
        var active = await Seed(UserRole.PROSUMER);
        using var caller = await LoginClient(active.Email);
        using var request = await caller.PostAsync("/api/prosumers/me/deactivation-request", null);
        Assert.Equal(HttpStatusCode.OK, request.StatusCode);
        using var declined = await admin.PatchAsJsonAsync($"/api/users/{active.Id}/status", new { status = "ACTIVE" });
        Assert.Equal(HttpStatusCode.OK, declined.StatusCode);
        using var fresh = await LoginClient(active.Email);
        using var me = await fresh.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [MongoFact]
    public async Task Pending_and_active_prosumers_cannot_use_reactivation_and_admin_cannot_edit_their_profile()
    {
        // Verify that pending and active prosumers cannot use reactivation and admin cannot edit their profile.
        var pending = await Seed(UserRole.PROSUMER, UserStatus.PENDING);
        using var invalid = await admin.PatchAsync($"/api/prosumers/{pending.NIC}/activate", null);
        Assert.Equal(HttpStatusCode.Conflict, invalid.StatusCode);
        using var edit = await admin.PutAsJsonAsync($"/api/users/{pending.Id}", Profile("Invalid Edit", pending.Email));
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        var active = await Seed(UserRole.PROSUMER);
        using var repeat = await admin.PatchAsync($"/api/prosumers/{active.NIC}/activate", null);
        Assert.Equal(HttpStatusCode.Conflict, repeat.StatusCode);
    }

    [MongoFact]
    public async Task Atomic_revision_checks_prevent_overwriting_concurrent_account_changes_and_support_legacy_documents()
    {
        // Verify that atomic revision checks prevent overwriting concurrent account changes and support legacy documents.
        var user = await Seed(UserRole.PROSUMER);
        await context.Users.UpdateOneAsync(x => x.Id == user.Id, Builders<User>.Update.Unset(x => x.Revision));
        var repository = new UserRepository(context);
        var first = await repository.UpdateProfileAsync(user, "Changed Name", user.Email, user.Phone, DateTime.UtcNow, CancellationToken.None);
        Assert.NotNull(first);
        Assert.Equal(1, first.Revision);
        var stale = await repository.ChangeStatusAsync(user, UserStatus.DEACTIVATED, true, DateTime.UtcNow, CancellationToken.None);
        Assert.Null(stale);
        var stored = await context.Users.Find(x => x.Id == user.Id).SingleAsync();
        Assert.Equal(UserStatus.ACTIVE, stored.Status);
        Assert.Equal("Changed Name", stored.FullName);
    }

    private async Task<User> Seed(UserRole role, UserStatus status = UserStatus.ACTIVE)
    {
        // Seed for User Management Integration Tests.
        var user = MongoModelTests.NewUser(role == UserRole.PROSUMER ? Random.Shared.NextInt64(100000000000, 999999999999).ToString() : null);
        user.Role = role; user.Status = status; user.PasswordHash = passwords.Hash(user, TestPassword);
        await context.Users.InsertOneAsync(user);
        return user;
    }

    private async Task<HttpClient> LoginClient(string email)
    {
        // Login Client for User Management Integration Tests.
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { identifier = email, password = TestPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    // Profile for User Management Integration Tests.
    private static UpdateProfileRequest Profile(string name, string email) => new()
    { FullName = name, Email = email, Phone = "0771234567" };

    private static async Task AssertSafe(HttpResponseMessage response)
    {
        // Assert Safe for User Management Integration Tests.
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tokenVersion", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("revision", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(TestPassword, json);
    }
}
