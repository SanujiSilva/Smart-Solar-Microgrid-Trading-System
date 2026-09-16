using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartSolarMicrogrid.Api.DTOs.Reservations;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class QrTokenRequest
{
    [Required, StringLength(200, MinimumLength = 20)]
    public string QrToken { get; init; } = "";
}
