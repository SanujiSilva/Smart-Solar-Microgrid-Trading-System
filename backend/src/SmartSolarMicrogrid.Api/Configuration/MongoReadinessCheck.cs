/*
 * File: src/SmartSolarMicrogrid.Api/Configuration/MongoReadinessCheck.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Service setup and configuration for Mongo Readiness Check.
 */
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using SmartSolarMicrogrid.Api.Repositories;

namespace SmartSolarMicrogrid.Api.Configuration;

public sealed class MongoReadinessCheck(MongoDbContext context) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext healthCheckContext, CancellationToken cancellationToken = default)
    {
        // Check Health for Mongo Readiness Check.
        try
        {
            await context.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1),
                cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("MongoDB is unavailable.");
        }
    }
}
