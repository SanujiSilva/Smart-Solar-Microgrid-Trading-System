using MongoDB.Bson;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(ObjectId id, CancellationToken cancellationToken);
    Task<User?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken);
    Task<bool> HasBackofficeAsync(CancellationToken cancellationToken);
    Task CreateAsync(User user, CancellationToken cancellationToken);
    Task<bool> RehashPasswordAsync(User user, string hash, DateTime updatedAt, CancellationToken cancellationToken);
    Task<User?> FindProsumerByNicAsync(string nic, CancellationToken cancellationToken);
    Task<(List<User> Items, long TotalCount)> SearchAsync(UserRole? role, UserStatus? status,
        string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<User?> UpdateProfileAsync(User expected, string fullName, string email, string phone,
        DateTime updatedAt, CancellationToken cancellationToken);
    Task<User?> ChangeStatusAsync(User expected, UserStatus status, bool revokeTokens,
        DateTime updatedAt, CancellationToken cancellationToken);
}
