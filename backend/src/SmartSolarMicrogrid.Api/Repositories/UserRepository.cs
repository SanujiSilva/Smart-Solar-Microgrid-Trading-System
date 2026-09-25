/*
 * File: src/SmartSolarMicrogrid.Api/Repositories/UserRepository.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: MongoDB persistence and query operations for User Repository.
 */
using MongoDB.Bson;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Repositories;

public sealed class UserRepository(MongoDbContext context) : IUserRepository
{
    public static readonly Collation EmailCollation = new("en", strength: CollationStrength.Secondary);

    // Find By Id for User.
    public async Task<User?> FindByIdAsync(ObjectId id, CancellationToken cancellationToken) =>
        await context.Users.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task<User?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken)
    {
        // Find By Identifier for User.
        var normalized = identifier.Trim();
        if (normalized.Contains('@'))
            return await context.Users.Find(Builders<User>.Filter.Eq(x => x.Email, normalized.ToLowerInvariant()),
                new FindOptions { Collation = EmailCollation }).FirstOrDefaultAsync(cancellationToken);
        return await context.Users.Find(x => x.NIC == normalized.ToUpperInvariant() && x.Role == UserRole.PROSUMER)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Has Backoffice for User.
    public async Task<bool> HasBackofficeAsync(CancellationToken cancellationToken) =>
        await context.Users.Find(x => x.Role == UserRole.BACKOFFICE).AnyAsync(cancellationToken);

    public async Task CreateAsync(User user, CancellationToken cancellationToken)
    {
        // Create for User.
        try { await context.Users.InsertOneAsync(user, cancellationToken: cancellationToken); }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        { throw new ApiException(StatusCodes.Status409Conflict, "An account with this NIC or email already exists."); }
    }

    public async Task<bool> RehashPasswordAsync(User user, string hash, DateTime updatedAt, CancellationToken cancellationToken)
    {
        // Rehash Password for User.
        var result = await context.Users.UpdateOneAsync(x => x.Id == user.Id && x.PasswordHash == user.PasswordHash,
            Builders<User>.Update.Set(x => x.PasswordHash, hash).Set(x => x.UpdatedAt, updatedAt).Inc(x => x.Revision, 1),
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    // Find Prosumer By Nic for User.
    public async Task<User?> FindProsumerByNicAsync(string nic, CancellationToken cancellationToken) =>
        await context.Users.Find(x => x.NIC == nic && x.Role == UserRole.PROSUMER).FirstOrDefaultAsync(cancellationToken);

    public async Task<(List<User> Items, long TotalCount)> SearchAsync(UserRole? role, UserStatus? status,
        string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        // Search for User.
        var filters = Builders<User>.Filter;
        var filter = filters.Empty;
        if (role.HasValue) filter &= filters.Eq(x => x.Role, role.Value);
        if (status.HasValue) filter &= filters.Eq(x => x.Status, status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = new BsonRegularExpression(Regex.Escape(search.Trim()), "i");
            filter &= filters.Or(filters.Regex(x => x.FullName, text), filters.Regex(x => x.Email, text), filters.Regex(x => x.NIC, text));
        }
        var count = await context.Users.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var items = await context.Users.Find(filter).Sort(Builders<User>.Sort.Descending(x => x.CreatedAt).Ascending(x => x.Id))
            .Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync(cancellationToken);
        return (items, count);
    }

    public async Task<User?> UpdateProfileAsync(User expected, string fullName, string email, string phone,
        DateTime updatedAt, CancellationToken cancellationToken)
    {
        // Update Profile for User.
        try
        {
            return await context.Users.FindOneAndUpdateAsync(AtRevision(expected), Builders<User>.Update
                .Set(x => x.FullName, fullName).Set(x => x.Email, email).Set(x => x.Phone, phone)
                .Set(x => x.UpdatedAt, updatedAt).Inc(x => x.Revision, 1),
                new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After }, cancellationToken);
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        { throw new ApiException(409, "An account with this email already exists."); }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        { throw new ApiException(409, "An account with this email already exists."); }
    }

    public async Task<User?> ChangeStatusAsync(User expected, UserStatus status, bool revokeTokens,
        DateTime updatedAt, CancellationToken cancellationToken)
    {
        // Change Status for User.
        var update = Builders<User>.Update.Set(x => x.Status, status).Set(x => x.UpdatedAt, updatedAt).Inc(x => x.Revision, 1);
        if (revokeTokens) update = update.Inc(x => x.TokenVersion, 1);
        return await context.Users.FindOneAndUpdateAsync(AtRevision(expected), update,
            new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After }, cancellationToken);
    }

    private static FilterDefinition<User> AtRevision(User user)
    {
        // At Revision for User.
        var f = Builders<User>.Filter;
        var revision = f.Eq(x => x.Revision, user.Revision);
        // Phase 1-4 records predate Revision and are treated as version zero.
        if (user.Revision == 0) revision |= f.Exists(x => x.Revision, false);
        return f.Eq(x => x.Id, user.Id) & revision;
    }
}
