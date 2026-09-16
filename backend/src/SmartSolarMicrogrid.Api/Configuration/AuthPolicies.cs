using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Configuration;

public static class AuthPolicies
{
    public const string BackofficeOnly = "BackofficeOnly";
    public const string OperatorOnly = "OperatorOnly";
    public const string ProsumerOnly = "ProsumerOnly";
    public const string Staff = "Staff";

    public static bool CanSignIn(User user) =>
        Enum.IsDefined(user.Role) && user.Status is UserStatus.ACTIVE or UserStatus.DEACTIVATION_REQUESTED;
}
