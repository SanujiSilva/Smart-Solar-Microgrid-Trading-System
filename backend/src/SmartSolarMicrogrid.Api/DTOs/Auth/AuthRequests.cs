/*
 * File: src/SmartSolarMicrogrid.Api/DTOs/Auth/AuthRequests.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: API request/response contracts and validation for Auth Requests.
 */
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

public class AccountCredentialsRequest : ContactDetailsRequest
{
    [Required, StringLength(128, MinimumLength = 12)]
    public string Password { get; init; } = "";
}

public class ContactDetailsRequest
{
    private string fullName = "";
    private string email = "";
    private string phone = "";
    [Required, StringLength(150, MinimumLength = 2)]
    public string FullName { get => fullName; init => fullName = value?.Trim() ?? ""; }
    [Required, EmailAddress, StringLength(254)]
    public string Email { get => email; init => email = value?.Trim() ?? ""; }
    [Required, RegularExpression(@"(?=.{1,25}\z)(?=(?:\D*\d){10}\D*\z)\+?[0-9() .-]+", ErrorMessage = "Phone must contain exactly 10 digits.")]
    public string Phone { get => phone; init => phone = value?.Trim() ?? ""; }
}
