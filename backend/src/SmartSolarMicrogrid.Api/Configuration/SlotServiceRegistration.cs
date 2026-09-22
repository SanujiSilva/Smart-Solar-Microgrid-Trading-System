/*
 * File: src/SmartSolarMicrogrid.Api/Configuration/SlotServiceRegistration.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Service setup and configuration for Slot Service Registration.
 */
using SmartSolarMicrogrid.Api.Repositories;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Configuration;

public static class SlotServiceRegistration
{
    public static IServiceCollection AddSlotManagement(this IServiceCollection services)
    {
        // Add Slot Management for Slot Registration.
        services.AddScoped<ISlotRepository, SlotRepository>();
        services.AddScoped<SlotService>();
        return services;
    }
}
