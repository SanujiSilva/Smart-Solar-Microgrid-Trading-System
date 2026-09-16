using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Configuration;

public static class QrServiceRegistration
{
    public static IServiceCollection AddQrTransactions(this IServiceCollection services)
    {
        services.AddScoped<QrTransactionService>();
        return services;
    }
}
