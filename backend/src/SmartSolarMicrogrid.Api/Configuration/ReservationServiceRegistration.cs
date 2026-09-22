/*
 * File: src/SmartSolarMicrogrid.Api/Configuration/ReservationServiceRegistration.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Service setup and configuration for Reservation Service Registration.
 */
using SmartSolarMicrogrid.Api.Repositories;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Configuration;

public static class ReservationServiceRegistration
{
    public static IServiceCollection AddReservationManagement(this IServiceCollection services)
    {
        // Add Reservation Management for Reservation Registration.
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<ReservationService>();
        return services;
    }
}
