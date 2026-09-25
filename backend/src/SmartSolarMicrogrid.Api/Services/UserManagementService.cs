/*
 * File: src/SmartSolarMicrogrid.Api/Services/UserManagementService.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Server-side application rules and orchestration for User Management Service.
 */
using System.Text.RegularExpressions;
using MongoDB.Bson;
using SmartSolarMicrogrid.Api.DTOs.Users;
using SmartSolarMicrogrid.Api.DTOs.Auth;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Repositories;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class UserManagementService(IUserRepository users, CurrentUser currentUser,
    PasswordService passwords, TimeProvider clock)
{
    public async Task<UserDetailsResponse> CreateProsumerAsync(RegisterProsumerRequest request, CancellationToken cancellationToken)
    {
        currentUser.Require(UserRole.BACKOFFICE);
        var now = clock.GetUtcNow().UtcDateTime;
        var user = new User
        {
            NIC = request.NIC, FullName = request.FullName.Trim(), Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone.Trim(), Role = UserRole.PROSUMER, Status = UserStatus.ACTIVE,
            CreatedAt = now, UpdatedAt = now
        };
        user.PasswordHash = passwords.Hash(user, request.Password);
        await users.CreateAsync(user, cancellationToken);
        return UserDetailsResponse.From(user);
    }

    public async Task<UserDetailsResponse> UpdateProsumerAsync(string nic, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        currentUser.Require(UserRole.BACKOFFICE);
        return await UpdateProfile(await FindProsumer(nic, cancellationToken), request, cancellationToken);
    }
    public async Task<UserPageResponse> ListAsync(UserListQuery query, bool prosumersOnly, CancellationToken cancellationToken)
    {
        // List for User Management.
        currentUser.Require(UserRole.BACKOFFICE);
        if (prosumersOnly && query.Role is not null && query.Role != "PROSUMER")
            throw new ApiException(400, "The prosumer list only supports the PROSUMER role.");
        UserRole? role = prosumersOnly ? UserRole.PROSUMER : query.Role is null ? null : Enum.Parse<UserRole>(query.Role);
        UserStatus? status = query.Status is null ? null : Enum.Parse<UserStatus>(query.Status);
        var result = await users.SearchAsync(role, status, query.Search, query.Page, query.PageSize, cancellationToken);
        return new UserPageResponse(result.Items.Select(UserDetailsResponse.From).ToList(), result.TotalCount, query.Page, query.PageSize);
    }

    public async Task<UserDetailsResponse> GetAsync(string id, CancellationToken cancellationToken)
    {
        // Get for User Management.
        currentUser.Require(UserRole.BACKOFFICE);
        return UserDetailsResponse.From(await FindById(id, cancellationToken));
    }

    public async Task<UserDetailsResponse> GetProsumerAsync(string nic, CancellationToken cancellationToken)
    {
        // Get Prosumer for User Management.
        currentUser.Require(UserRole.BACKOFFICE);
        return UserDetailsResponse.From(await FindProsumer(nic, cancellationToken));
    }

    public async Task<UserDetailsResponse> CreateStaffAsync(CreateStaffRequest request, CancellationToken cancellationToken)
    {
        // Create Staff for User Management.
        currentUser.Require(UserRole.BACKOFFICE);
        if (!Enum.TryParse<UserRole>(request.Role, out var role) || role is not (UserRole.BACKOFFICE or UserRole.GRID_OPERATOR))
            throw new ApiException(400, "Staff role must be BACKOFFICE or GRID_OPERATOR.");
        var now = clock.GetUtcNow().UtcDateTime;
        var user = new User
        {
            FullName = request.FullName.Trim(), Email = request.Email.Trim().ToLowerInvariant(), Phone = request.Phone.Trim(),
            Role = role, Status = UserStatus.ACTIVE, CreatedAt = now, UpdatedAt = now
        };
        user.PasswordHash = passwords.Hash(user, request.Password);
        await users.CreateAsync(user, cancellationToken);
        return UserDetailsResponse.From(user);
    }

    public async Task<UserDetailsResponse> UpdateStaffAsync(string id, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        // Update Staff for User Management.
        currentUser.Require(UserRole.BACKOFFICE);
        var user = await FindById(id, cancellationToken);
        return await UpdateProfile(user, request, cancellationToken);
    }

    // Update My Profile for User Management.
    public Task<UserDetailsResponse> UpdateMyProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        UpdateProfile(currentUser.Require(UserRole.PROSUMER), request, cancellationToken);

    public async Task<UserDetailsResponse> ChangeStatusAsync(string id, ChangeUserStatusRequest request, CancellationToken cancellationToken)
    {
        // Change Status for User Management.
        var actor = currentUser.Require(UserRole.BACKOFFICE);
        var user = await FindById(id, cancellationToken);
        if (!Enum.TryParse<UserStatus>(request.Status, out var status) || status is not (UserStatus.ACTIVE or UserStatus.DEACTIVATED))
            throw new ApiException(400, "Status must be ACTIVE or DEACTIVATED.");
        if (actor.Id == user.Id && status == UserStatus.DEACTIVATED)
            throw new ApiException(409, "You cannot deactivate your own Backoffice account.");
        if (user.Status == status) return UserDetailsResponse.From(user);
        return await SaveStatus(user, status, revokeTokens: true, cancellationToken);
    }

    public async Task<UserDetailsResponse> ActivateProsumerAsync(string nic, CancellationToken cancellationToken)
    {
        // Activate Prosumer for User Management.
        currentUser.Require(UserRole.BACKOFFICE);
        var user = await FindProsumer(nic, cancellationToken);
        if (user.Status != UserStatus.DEACTIVATED)
            throw new ApiException(409, "Only a deactivated prosumer can be reactivated. Use the user status endpoint for pending approvals.");
        return await SaveStatus(user, UserStatus.ACTIVE, revokeTokens: true, cancellationToken);
    }

    public async Task<UserDetailsResponse> RequestDeactivationAsync(CancellationToken cancellationToken)
    {
        // Request Deactivation for User Management.
        var user = currentUser.Require(UserRole.PROSUMER);
        if (user.Status == UserStatus.DEACTIVATION_REQUESTED) return UserDetailsResponse.From(user);
        if (user.Status != UserStatus.ACTIVE) throw new ApiException(409, "Only an active prosumer can request deactivation.");
        return await SaveStatus(user, UserStatus.DEACTIVATION_REQUESTED, revokeTokens: false, cancellationToken);
    }

    private async Task<UserDetailsResponse> UpdateProfile(User user, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        // Update Profile for User Management.
        var updated = await users.UpdateProfileAsync(user, request.FullName.Trim(), request.Email.Trim().ToLowerInvariant(),
            request.Phone.Trim(), clock.GetUtcNow().UtcDateTime, cancellationToken);
        return UserDetailsResponse.From(RequireUnchanged(updated));
    }

    private async Task<UserDetailsResponse> SaveStatus(User user, UserStatus status, bool revokeTokens, CancellationToken cancellationToken)
    {
        // Save Status for User Management.
        var updated = await users.ChangeStatusAsync(user, status, revokeTokens, clock.GetUtcNow().UtcDateTime, cancellationToken);
        return UserDetailsResponse.From(RequireUnchanged(updated));
    }

    // Require Unchanged for User Management.
    private static User RequireUnchanged(User? user) => user ?? throw new ApiException(409, "The account changed during this request. Reload and try again.");

    private async Task<User> FindById(string id, CancellationToken cancellationToken)
    {
        // Find By Id for User Management.
        if (!ObjectId.TryParse(id, out var objectId)) throw new ApiException(400, "User ID must be a valid ObjectId.");
        return await users.FindByIdAsync(objectId, cancellationToken) ?? throw new ApiException(404, "User was not found.");
    }

    private async Task<User> FindProsumer(string nic, CancellationToken cancellationToken)
    {
        // Find Prosumer for User Management.
        var normalized = nic.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(normalized, @"\A(?:[0-9]{9}[VX]|[0-9]{12})\z")) throw new ApiException(400, "NIC format is invalid.");
        return await users.FindProsumerByNicAsync(normalized, cancellationToken) ?? throw new ApiException(404, "Prosumer was not found.");
    }
}
