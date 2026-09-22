/*
 * File: src/SmartSolarMicrogrid.Api/DTOs/Reservations/QrRequests.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: API request/response contracts and validation for Qr Requests.
 */
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartSolarMicrogrid.Api.DTOs.Reservations;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class QrTokenRequest
{
    [Required, StringLength(200, MinimumLength = 20)]
    public string QrToken { get; init; } = "";
}
