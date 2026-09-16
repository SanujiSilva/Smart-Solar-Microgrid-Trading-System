using SmartSolarMicrogrid.Api.Repositories;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Configuration;

public static class SlotServiceRegistration
{
    public static IServiceCollection AddSlotManagement(this IServiceCollection services)
    {
        services.AddScoped<ISlotRepository, SlotRepository>();
        services.AddScoped<SlotService>();
        return services;
    }
}
