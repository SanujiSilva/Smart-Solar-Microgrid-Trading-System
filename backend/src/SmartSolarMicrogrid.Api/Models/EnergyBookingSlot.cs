using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolarMicrogrid.Api.Models;

public sealed class EnergyBookingSlot : MongoDocument
{
    public ObjectId StationId { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime StartTime { get; set; }
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime EndTime { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Capacity { get; set; }
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal AvailableCapacity { get; set; }

    [BsonRepresentation(BsonType.String)]
    public SlotStatus Status { get; set; } = SlotStatus.CLOSED;
}
