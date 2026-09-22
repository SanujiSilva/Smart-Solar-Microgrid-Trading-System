/*
 * File: src/SmartSolarMicrogrid.Api/Services/AuthService.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Server-side application rules and orchestration for Auth Service.
 */
using Microsoft.AspNetCore.Identity;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs.Auth;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Repositories;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class AuthService(IUserRepository users, PasswordService passwords, JwtTokenService tokens, TimeProvider clock)
{
    public async Task<AuthUserResponse> RegisterAsync(RegisterProsumerRequest request, CancellationToken cancellationToken)
    {
        // Normalize the prosumer identity, hash the password, and create a pending account.
        var now = clock.GetUtcNow().UtcDateTime;
        var user = new User
        {
            NIC = request.NIC, FullName = request.FullName.Trim(), Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone.Trim(), Role = UserRole.PROSUMER, Status = UserStatus.PENDING,
            CreatedAt = now, UpdatedAt = now
        };
        user.PasswordHash = passwords.Hash(user, request.Password);
        // Database uniqueness, rather than a preflight lookup, also handles concurrent registration.
        await users.CreateAsync(user, cancellationToken);
        return AuthUserResponse.From(user);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        // Validate account credentials and status before issuing an access token.
        var user = await users.FindByIdentifierAsync(request.Identifier, cancellationToken);
        var result = passwords.Verify(user, request.Password);
        if (user is null || result == PasswordVerificationResult.Failed)
            throw new ApiException(401, "Invalid identifier or password.");
        if (!AuthPolicies.CanSignIn(user))
            throw new ApiException(403, "Your account is not active. Contact Backoffice for assistance.");
        if (result == PasswordVerificationResult.SuccessRehashNeeded &&
            !await users.RehashPasswordAsync(user, passwords.Hash(user, request.Password), clock.GetUtcNow().UtcDateTime, cancellationToken))
            throw new ApiException(401, "Your credentials changed. Please sign in again.");
        return tokens.Issue(user);
    }
}
