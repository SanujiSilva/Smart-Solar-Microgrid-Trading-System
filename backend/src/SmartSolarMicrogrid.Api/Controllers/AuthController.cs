using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs.Auth;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController]
[Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [AllowAnonymous, HttpPost("login"), EnableRateLimiting(AuthServiceRegistration.AuthRateLimit)]
    [ProducesResponseType<LoginResponse>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await auth.LoginAsync(request, cancellationToken));

    [AllowAnonymous, HttpPost("prosumer/register"), EnableRateLimiting(AuthServiceRegistration.AuthRateLimit)]
    [ProducesResponseType<AuthUserResponse>(201)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<ActionResult<AuthUserResponse>> Register(RegisterProsumerRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await auth.RegisterAsync(request, cancellationToken));

    [Authorize, HttpGet("me")]
    [ProducesResponseType<AuthUserResponse>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    public ActionResult<AuthUserResponse> Me() =>
        Ok(AuthUserResponse.From((User)HttpContext.Items[JwtBearerEventsHandler.CurrentUserKey]!));
}
