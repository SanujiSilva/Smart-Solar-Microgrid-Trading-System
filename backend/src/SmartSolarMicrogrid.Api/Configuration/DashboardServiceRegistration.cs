using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Configuration;

public static class DashboardServiceRegistration
{
    public static IServiceCollection AddDashboardServices(this IServiceCollection services)
    {
        services.AddScoped<DashboardService>();
        return services;
    }
}
