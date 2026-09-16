using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public sealed class UserRepository(MongoDbContext context) : IUserRepository
{
    public static readonly Collation EmailCollation = new("en", strength: CollationStrength.Secondary);

    public async Task<User?> FindByIdAsync(ObjectId id, CancellationToken cancellationToken) =>
        await context.Users.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task<User?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken)
    {
        var normalized = identifier.Trim();
        if (normalized.Contains('@'))
            return await context.Users.Find(Builders<User>.Filter.Eq(x => x.Email, normalized.ToLowerInvariant()),
                new FindOptions { Collation = EmailCollation }).FirstOrDefaultAsync(cancellationToken);
        return await context.Users.Find(x => x.NIC == normalized.ToUpperInvariant() && x.Role == UserRole.PROSUMER)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasBackofficeAsync(CancellationToken cancellationToken) =>
        await context.Users.Find(x => x.Role == UserRole.BACKOFFICE).AnyAsync(cancellationToken);

    public async Task CreateAsync(User user, CancellationToken cancellationToken)
    {
        try { await context.Users.InsertOneAsync(user, cancellationToken: cancellationToken); }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        { throw new ApiException(StatusCodes.Status409Conflict, "An account with this NIC or email already exists."); }
    }

    public async Task<bool> RehashPasswordAsync(User user, string hash, DateTime updatedAt, CancellationToken cancellationToken)
    {
        var result = await context.Users.UpdateOneAsync(x => x.Id == user.Id && x.PasswordHash == user.PasswordHash,
            Builders<User>.Update.Set(x => x.PasswordHash, hash).Set(x => x.UpdatedAt, updatedAt),
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }
}
