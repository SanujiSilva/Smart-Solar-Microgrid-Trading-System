using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class CurrentUser(IHttpContextAccessor accessor)
{
    public User Get() => accessor.HttpContext?.Items[JwtBearerEventsHandler.CurrentUserKey] as User
        ?? throw new ApiException(401, "Authentication is required.");

    public User Require(UserRole role)
    {
        var user = Get();
        if (user.Role != role) throw new ApiException(403, "You do not have permission to perform this action.");
        return user;
    }
}
