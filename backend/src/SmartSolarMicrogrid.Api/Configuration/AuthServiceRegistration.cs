using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Repositories;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Configuration;

public static class AuthServiceRegistration
{
    public const string AuthRateLimit = "Authentication";

    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>().Bind(configuration.GetSection("Jwt"))
            .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer) && !string.IsNullOrWhiteSpace(x.Audience), "Jwt:Issuer and Jwt:Audience are required.")
            .Validate(x => JwtSettings.HasValidKey(x.SigningKey), "Jwt:SigningKey must be a Base64-encoded random key of at least 32 bytes. Configure it outside source control.")
            .Validate(x => x.AccessTokenMinutes is >= 1 and <= 60, "Jwt:AccessTokenMinutes must be between 1 and 60.")
            .ValidateOnStart();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<PasswordService>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<AuthService>();
        services.AddHttpContextAccessor();
        services.AddScoped<CurrentUser>();
        services.AddScoped<UserManagementService>();
        services.AddScoped<BackofficeBootstrapService>();
        services.AddScoped<JwtBearerEventsHandler>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, settings) =>
            {
                var jwt = settings.Value;
                options.MapInboundClaims = false;
                options.IncludeErrorDetails = false;
                options.EventsType = typeof(JwtBearerEventsHandler);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = jwt.Issuer,
                    ValidateAudience = true, ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwt.SigningKey)),
                    ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.Zero,
                    NameClaimType = "sub", RoleClaimType = "role"
                };
            });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.BackofficeOnly, p => p.RequireRole(nameof(UserRole.BACKOFFICE)));
            options.AddPolicy(AuthPolicies.OperatorOnly, p => p.RequireRole(nameof(UserRole.GRID_OPERATOR)));
            options.AddPolicy(AuthPolicies.ProsumerOnly, p => p.RequireRole(nameof(UserRole.PROSUMER)));
            options.AddPolicy(AuthPolicies.Staff, p => p.RequireRole(nameof(UserRole.BACKOFFICE), nameof(UserRole.GRID_OPERATOR)));
        });
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                return ValueTask.CompletedTask;
            };
            options.AddPolicy(AuthRateLimit, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        return services;
    }
}
