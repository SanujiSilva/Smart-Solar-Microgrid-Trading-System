using System.Diagnostics;
using Microsoft.Extensions.Options;
using SmartSolarMicrogrid.Api.Middleware;

namespace SmartSolarMicrogrid.Api.Configuration;

public static class ApiServiceRegistration
{
    public const string BrowserCorsPolicy = "BrowserClients";

    public static IServiceCollection AddApiFoundation(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi(options => options.AddDocumentTransformer<BearerOpenApiTransformer>());
        services.AddHealthChecks();
        services.AddSingleton(TimeProvider.System);
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance = context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions["traceId"] =
                    Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            };
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddOptions<CorsSettings>()
            .Bind(configuration.GetSection(CorsSettings.SectionName))
            .Validate(settings => settings.AllowedOrigins is not null &&
                settings.AllowedOrigins.All(CorsSettings.IsValidOrigin),
                "Cors:AllowedOrigins must contain HTTP(S) origins without paths, queries, or wildcards.")
            .ValidateOnStart();

        services.AddCors();
        services.AddOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>()
            .Configure<IOptions<CorsSettings>>((options, settings) =>
            {
                options.AddPolicy(BrowserCorsPolicy, policy =>
                {
                    var origins = settings.Value.AllowedOrigins;
                    if (origins.Length > 0)
                    {
                        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
                    }
                });
            });

        return services;
    }
}
