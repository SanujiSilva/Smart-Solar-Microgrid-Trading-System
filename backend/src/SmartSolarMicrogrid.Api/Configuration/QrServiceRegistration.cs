/*
 * File: src/SmartSolarMicrogrid.Api/Configuration/QrServiceRegistration.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Service setup and configuration for Qr Service Registration.
 */
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Configuration;

public static class QrServiceRegistration
{
    public static IServiceCollection AddQrTransactions(this IServiceCollection services)
    {
        // Add Qr Transactions for Qr Registration.
        services.AddScoped<QrTransactionService>();
        return services;
    }
}
