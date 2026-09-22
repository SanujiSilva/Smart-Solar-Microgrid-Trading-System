/*
 * File: src/SmartSolarMicrogrid.Api/Models/SolarStationInfo.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Persistence model definitions for Solar Station Info.
 */
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;
using System.Text.Json.Serialization;

namespace SmartSolarMicrogrid.Api.Models;

public sealed class SolarStationInfo : MongoDocument
{
    [JsonIgnore]
    public long Revision { get; set; }
    public required string StationCode { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public required GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; }

    // One persisted coordinate pair avoids latitude/longitude drifting from the geo index.
    [BsonIgnore]
    public double Latitude => Location.Coordinates.Latitude;
    [BsonIgnore]
    public double Longitude => Location.Coordinates.Longitude;

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal CapacityKWh { get; set; }
    public int AvailableBatterySlots { get; set; }

    [BsonRepresentation(BsonType.String)]
    public StationStatus Status { get; set; } = StationStatus.INACTIVE;
    public OperatingSchedule OperatingSchedule { get; set; } = new();
}

public sealed class OperatingSchedule
{
    public string TimeZoneId { get; set; } = "Asia/Colombo";
    public List<OperatingPeriod> WeeklyPeriods { get; set; } = [];
}

public sealed class OperatingPeriod
{
    [BsonRepresentation(BsonType.String)]
    public DayOfWeek Day { get; set; }
    // Local minutes after midnight; close may equal 1440. Split overnight periods by day.
    public int OpenMinuteOfDay { get; set; }
    public int CloseMinuteOfDay { get; set; }
}
