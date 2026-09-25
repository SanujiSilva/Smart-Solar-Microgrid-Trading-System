/*
 * File: src/SmartSolarMicrogrid.Api/Configuration/StationServiceRegistration.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Service setup and configuration for Station Service Registration.
 */
using SmartSolarMicrogrid.Api.Repositories;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Configuration;

public static class StationServiceRegistration
{
    public static IServiceCollection AddStationManagement(this IServiceCollection services)
    {
        // Add Station Management for Station Registration.
        services.AddScoped<IStationRepository, StationRepository>();
        services.AddScoped<StationService>();
        return services;
    }
}
