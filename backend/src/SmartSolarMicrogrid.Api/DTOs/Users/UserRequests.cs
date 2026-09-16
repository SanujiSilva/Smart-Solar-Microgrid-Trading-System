using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SmartSolarMicrogrid.Api.DTOs.Auth;

namespace SmartSolarMicrogrid.Api.DTOs.Users;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateStaffRequest : AccountCredentialsRequest
{
    [Required, RegularExpression("BACKOFFICE|GRID_OPERATOR")]
    public string Role { get; init; } = "";
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateProfileRequest : ContactDetailsRequest;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChangeUserStatusRequest
{
    [Required, RegularExpression("ACTIVE|DEACTIVATED")]
    public string Status { get; init; } = "";
}

public sealed class UserListQuery
{
    [Range(1, 100000)]
    public int Page { get; init; } = 1;
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
    [StringLength(100)]
    public string? Search { get; init; }
    [RegularExpression("BACKOFFICE|GRID_OPERATOR|PROSUMER")]
    public string? Role { get; init; }
    [RegularExpression("PENDING|ACTIVE|DEACTIVATION_REQUESTED|DEACTIVATED")]
    public string? Status { get; init; }
}
