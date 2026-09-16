using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolarMicrogrid.Api.Models;

public sealed class EnergyReservation : MongoDocument
{
    public required string ReservationCode { get; set; }
    private string prosumerNIC = "";
    public required string ProsumerNIC
    {
        get => prosumerNIC;
        set => prosumerNIC = value.Trim().ToUpperInvariant();
    }
    public ObjectId StationId { get; set; }
    public ObjectId SlotId { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal EnergyAmount { get; set; }
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ReservationDateTime { get; set; }

    [BsonRepresentation(BsonType.String)]
    public ReservationStatus Status { get; set; } = ReservationStatus.PENDING;

    // Phase 9 will define token generation/reissue; persist a hash, not the bearer token.
    [JsonIgnore, BsonIgnoreIfNull]
    public string? QrTokenHash { get; set; }

    [BsonIgnoreIfNull, BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CompletedAt { get; set; }
    [BsonIgnoreIfNull]
    public ObjectId? CompletedByOperatorId { get; set; }
}
