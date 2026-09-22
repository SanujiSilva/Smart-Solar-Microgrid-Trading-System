/*
 * File: src/SmartSolarMicrogrid.Api/Services/BackofficeBootstrapService.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Server-side application rules and orchestration for Backoffice Bootstrap Service.
 */
using System.ComponentModel.DataAnnotations;
using SmartSolarMicrogrid.Api.DTOs.Auth;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Repositories;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class BackofficeBootstrapService(IUserRepository users, PasswordService passwords,
    IConfiguration configuration, TimeProvider clock)
{
    public async Task CreateAsync(CancellationToken cancellationToken)
    {
        // Create for Backoffice Bootstrap.
        var request = new AccountCredentialsRequest
        {
            FullName = configuration["Bootstrap:FullName"] ?? "",
            Email = configuration["Bootstrap:Email"] ?? "", Phone = configuration["Bootstrap:Phone"] ?? "",
            Password = configuration["Bootstrap:Password"] ?? ""
        };
        if (!Validator.TryValidateObject(request, new ValidationContext(request), [], true))
            throw new ApiException(400, "Supply valid Bootstrap:FullName, Email, Phone, and Password (12-128 characters) through secrets or environment variables.");
        if (await users.HasBackofficeAsync(cancellationToken))
            throw new ApiException(409, "A Backoffice account already exists. Bootstrap will not replace or reactivate it.");
        var now = clock.GetUtcNow().UtcDateTime;
        var user = new User
        {
            FullName = request.FullName.Trim(), Email = request.Email.Trim().ToLowerInvariant(), Phone = request.Phone.Trim(),
            Role = UserRole.BACKOFFICE, Status = UserStatus.ACTIVE, CreatedAt = now, UpdatedAt = now
        };
        user.PasswordHash = passwords.Hash(user, request.Password);
        await users.CreateAsync(user, cancellationToken);
    }
}
