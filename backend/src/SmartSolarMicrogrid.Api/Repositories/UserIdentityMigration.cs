using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SmartSolarMicrogrid.Api.Repositories;

/// <summary>Copies legacy users without modifying the original collection or authentication references.</summary>
public sealed class UserIdentityMigration(MongoDbContext context)
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        var legacy = context.Database.GetCollection<BsonDocument>("Users");
        var migrations = context.Database.GetCollection<BsonDocument>("SchemaMigrations");
        var marker = new BsonDocument("_id", "users-nic-primary-key-v1");
        if (await migrations.Find(marker).AnyAsync(cancellationToken) ||
            !await legacy.Find(FilterDefinition<BsonDocument>.Empty).AnyAsync(cancellationToken)) return;

        // Create the collection before opening a transaction (also supported by older MongoDB servers).
        await migrations.UpdateOneAsync(new BsonDocument("_id", "identity-migration-lock"),
            new BsonDocument("$setOnInsert", new BsonDocument("revision", 0)),
            new UpdateOptions { IsUpsert = true }, cancellationToken);
        using var session = await context.Database.Client.StartSessionAsync(cancellationToken: cancellationToken);
        await session.WithTransactionAsync(async (transaction, token) =>
        {
            await migrations.UpdateOneAsync(transaction, new BsonDocument("_id", "identity-migration-lock"),
                new BsonDocument("$inc", new BsonDocument("revision", 1)), cancellationToken: token);
            if (await migrations.Find(transaction, marker).AnyAsync(token)) return true;
            var documents = await legacy.Find(transaction, FilterDefinition<BsonDocument>.Empty).ToListAsync(token);
            var destination = context.Database.GetCollection<BsonDocument>("UsersByIdentity");
            foreach (var document in documents)
            {
                var originalId = document["_id"].AsObjectId;
                document["UserId"] = originalId;
                if (document["Role"].AsString == "PROSUMER")
                {
                    var nic = document.GetValue("NIC", "").AsString.Trim().ToUpperInvariant();
                    if (!Regex.IsMatch(nic, @"\A(?:[0-9]{9}[VX]|[0-9]{12})\z"))
                        throw new InvalidOperationException("Legacy prosumer has an invalid NIC; migration was not committed.");
                    document["NIC"] = nic;
                    document["_id"] = nic;
                }
                await destination.InsertOneAsync(transaction, document, cancellationToken: token);
            }
            await migrations.InsertOneAsync(transaction, new BsonDocument
            {
                { "_id", marker["_id"] }, { "CompletedAt", DateTime.UtcNow }, { "UserCount", documents.Count }
            }, cancellationToken: token);
            return true;
        }, new TransactionOptions(readConcern: ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority), cancellationToken);
    }
}
