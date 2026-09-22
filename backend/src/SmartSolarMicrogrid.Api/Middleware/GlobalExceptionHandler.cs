/*
 * File: src/SmartSolarMicrogrid.Api/Middleware/GlobalExceptionHandler.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Request-pipeline behavior for Global Exception Handler.
 */
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Helpers;

namespace SmartSolarMicrogrid.Api.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Try Handle for Global Exception Handler.
        var expected = exception as ApiException;
        if (expected is null)
            logger.LogError(exception, "Unhandled API exception. TraceId: {TraceId}", httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = expected?.StatusCode ?? StatusCodes.Status500InternalServerError;
        if (httpContext.Response.StatusCode == StatusCodes.Status401Unauthorized)
            httpContext.Response.Headers.WWWAuthenticate = "Bearer";
        var problem = new ProblemDetails
        {
            Status = httpContext.Response.StatusCode,
            Title = expected?.Message ?? "An unexpected error occurred.",
            Detail = expected is null ? "Please try again. If the problem persists, provide the trace ID to support." : null,
            Instance = httpContext.Request.Path,
            Extensions = { ["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier }
        };

        if (!await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            { HttpContext = httpContext, ProblemDetails = problem }))
        {
            // Keep errors safe JSON even when a caller requests an unsupported media type.
            await httpContext.Response.WriteAsJsonAsync(problem,
                options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        }

        return true;
    }
}
