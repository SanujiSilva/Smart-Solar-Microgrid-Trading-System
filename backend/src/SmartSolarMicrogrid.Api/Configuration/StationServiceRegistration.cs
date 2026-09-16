using SmartSolarMicrogrid.Api.Repositories;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Configuration;

public static class StationServiceRegistration
{
    public static IServiceCollection AddStationManagement(this IServiceCollection services)
    {
        services.AddScoped<IStationRepository, StationRepository>();
        services.AddScoped<StationService>();
        return services;
    }
}
