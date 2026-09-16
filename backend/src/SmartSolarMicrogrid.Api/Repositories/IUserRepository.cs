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
}
