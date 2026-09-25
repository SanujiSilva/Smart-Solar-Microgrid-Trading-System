/*
 * File: src/SmartSolarMicrogrid.Api/Repositories/IUserRepository.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: MongoDB persistence and query operations for IUser Repository.
 */
using MongoDB.Bson;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public interface IUserRepository
{
    // Find By Id for IUser.
    Task<User?> FindByIdAsync(ObjectId id, CancellationToken cancellationToken);
    // Find By Identifier for IUser.
    Task<User?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken);
    // Has Backoffice for IUser.
    Task<bool> HasBackofficeAsync(CancellationToken cancellationToken);
    // Create for IUser.
    Task CreateAsync(User user, CancellationToken cancellationToken);
    // Rehash Password for IUser.
    Task<bool> RehashPasswordAsync(User user, string hash, DateTime updatedAt, CancellationToken cancellationToken);
    // Find Prosumer By Nic for IUser.
    Task<User?> FindProsumerByNicAsync(string nic, CancellationToken cancellationToken);
    // Search for IUser.
    Task<(List<User> Items, long TotalCount)> SearchAsync(UserRole? role, UserStatus? status,
        string? search, int page, int pageSize, CancellationToken cancellationToken);
    // Update Profile for IUser.
    Task<User?> UpdateProfileAsync(User expected, string fullName, string email, string phone,
        DateTime updatedAt, CancellationToken cancellationToken);
    // Change Status for IUser.
    Task<User?> ChangeStatusAsync(User expected, UserStatus status, bool revokeTokens,
        DateTime updatedAt, CancellationToken cancellationToken);
}
