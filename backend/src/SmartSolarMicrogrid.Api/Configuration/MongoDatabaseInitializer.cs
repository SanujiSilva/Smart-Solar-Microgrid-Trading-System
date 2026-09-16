using Microsoft.Extensions.Options;
using MongoDB.Bson;
using SmartSolarMicrogrid.Api.Repositories;

namespace SmartSolarMicrogrid.Api.Configuration;

public sealed class MongoDatabaseInitializer(
    MongoDbContext context, MongoIndexInitializer indexes, IOptions<MongoDbSettings> options,
    ILogger<MongoDatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.InitializationTimeoutSeconds));
        try
        {
            await context.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1),
                cancellationToken: timeout.Token);
            await indexes.InitializeAsync(timeout.Token);
            logger.LogInformation("MongoDB connection and required collection indexes are ready.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            // Driver errors may contain server addresses or credentials. Do not forward their text.
            logger.LogError("MongoDB initialization failed ({ErrorType}). Verify connectivity, permissions, and existing indexes/data.",
                exception.GetType().Name);
            throw new InvalidOperationException(
                "MongoDB initialization failed. Check configuration, server availability, index permissions, and duplicate data. No database details are exposed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
