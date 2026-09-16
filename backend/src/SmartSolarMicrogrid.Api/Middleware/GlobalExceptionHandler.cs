using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SmartSolarMicrogrid.Api.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled API exception. TraceId: {TraceId}",
            httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = "Please try again. If the problem persists, provide the trace ID to support.",
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
