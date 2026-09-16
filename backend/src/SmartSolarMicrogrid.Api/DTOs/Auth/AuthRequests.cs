using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartSolarMicrogrid.Api.DTOs.Auth;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class LoginRequest
{
    [Required, StringLength(254)]
    public string Identifier { get; init; } = "";
    [Required, StringLength(128)]
    public string Password { get; init; } = "";
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RegisterProsumerRequest : AccountCredentialsRequest
{
    [Required, RegularExpression(@"(?:[0-9]{9}[vVxX]|[0-9]{12})", ErrorMessage = "NIC must contain 12 digits or 9 digits followed by V/X.")]
    public string NIC { get; init; } = "";
}

public class AccountCredentialsRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string FullName { get; init; } = "";
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = "";
    [Required, Phone, StringLength(25, MinimumLength = 7)]
    public string Phone { get; init; } = "";
    [Required, StringLength(128, MinimumLength = 12)]
    public string Password { get; init; } = "";
}
