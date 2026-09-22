/*
 * File: src/SmartSolarMicrogrid.Api/Configuration/MongoServiceRegistration.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Service setup and configuration for Mongo Service Registration.
 */
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Repositories;

namespace SmartSolarMicrogrid.Api.Configuration;

public static class MongoServiceRegistration
{
    public static IServiceCollection AddMongoDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Mongo Database for Mongo Registration.
        services.AddOptions<MongoDbSettings>()
            .Bind(configuration.GetSection(MongoDbSettings.SectionName))
            .Validate(x => MongoDbSettings.IsValidConnectionString(x.ConnectionString),
                "MongoDb:ConnectionString must be a valid MongoDB URI. Supply it through configuration.")
            .Validate(x => MongoDbSettings.IsValidDatabaseName(x.DatabaseName),
                "MongoDb:DatabaseName must be 1-63 ASCII letters/digits/underscores/hyphens, start with a letter/digit, and not be admin, local, or config.")
            .Validate(x => x.TimeoutSeconds is >= 1 and <= 60,
                "MongoDb:TimeoutSeconds must be between 1 and 60.")
            .Validate(x => x.InitializationTimeoutSeconds is >= 1 and <= 300,
                "MongoDb:InitializationTimeoutSeconds must be between 1 and 300.")
            .ValidateOnStart();

        services.AddSingleton<IMongoClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            settings.ConnectTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            settings.SocketTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            return new MongoClient(settings);
        });
        services.AddSingleton<MongoDbContext>();
        services.AddSingleton<MongoIndexInitializer>();
        services.AddHostedService<MongoDatabaseInitializer>();
        services.AddHealthChecks().AddCheck<MongoReadinessCheck>("mongodb", tags: ["ready"]);
        return services;
    }
}
