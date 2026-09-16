using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartSolarMicrogrid.Api.DTOs.Dashboards;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ReservationSearchQuery
{
    [StringLength(30)]
    public string? ReservationCode { get; init; }

    public string? StationId { get; init; }

    [RegularExpression("PENDING|APPROVED|CANCELLED|COMPLETED|REJECTED")]
    public string? Status { get; init; }

    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }

    [Range(1, 100000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
