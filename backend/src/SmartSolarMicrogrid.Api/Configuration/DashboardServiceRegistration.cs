/*
 * File: src/SmartSolarMicrogrid.Api/Configuration/DashboardServiceRegistration.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Service setup and configuration for Dashboard Service Registration.
 */
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Configuration;

public static class DashboardServiceRegistration
{
    public static IServiceCollection AddDashboardServices(this IServiceCollection services)
    {
        // Add Dashboard Services for Dashboard Registration.
        services.AddScoped<DashboardService>();
        return services;
    }
}
