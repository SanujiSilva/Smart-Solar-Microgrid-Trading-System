using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.GeoJsonObjectModel;
using SmartSolarMicrogrid.Api.Models;
using Xunit;

namespace SmartSolarMicrogrid.Api.Tests;

public sealed class MongoModelTests
{
    [Fact]
    public void User_stores_ObjectId_string_enums_and_normalized_NIC_but_never_JSON_password_hash()
    {
        var user = NewUser(" 991234567v ");
        var bson = user.ToBsonDocument();
        Assert.Equal(BsonType.ObjectId, bson["_id"].BsonType);
        Assert.Equal("991234567V", bson["NIC"].AsString);
        Assert.Equal("PROSUMER", bson["Role"].AsString);
        Assert.Equal("PENDING", bson["Status"].AsString);
        Assert.Equal("test-hash-not-a-password", bson["PasswordHash"].AsString);
        Assert.DoesNotContain("test-hash-not-a-password", JsonSerializer.Serialize(user));
        Assert.False(NewUser(null).ToBsonDocument().Contains("NIC"));
        Assert.False(NewUser("  ").ToBsonDocument().Contains("NIC"));
    }

    [Fact]
    public void Reservation_roundtrip_preserves_decimal_references_and_UTC_without_JSON_token_hash()
    {
        var time = new DateTime(2026, 9, 16, 10, 30, 0, DateTimeKind.Utc);
        var reservation = new EnergyReservation
        {
            ReservationCode = "TEST-R1", ProsumerNIC = " 991234567v ",
            StationId = ObjectId.GenerateNewId(), SlotId = ObjectId.GenerateNewId(),
            EnergyAmount = 12.34567890123456789m, ReservationDateTime = time,
            Status = ReservationStatus.COMPLETED, CompletedAt = time,
            CompletedByOperatorId = ObjectId.GenerateNewId(), QrTokenHash = "test-token-hash"
        };
        var bson = reservation.ToBsonDocument();
        Assert.Equal(BsonType.Decimal128, bson["EnergyAmount"].BsonType);
        Assert.Equal(BsonType.ObjectId, bson["StationId"].BsonType);
        Assert.Equal(BsonType.ObjectId, bson["SlotId"].BsonType);
        Assert.Equal(BsonType.ObjectId, bson["CompletedByOperatorId"].BsonType);
        Assert.Equal(BsonType.DateTime, bson["ReservationDateTime"].BsonType);
        Assert.Equal("COMPLETED", bson["Status"].AsString);
        var result = BsonSerializer.Deserialize<EnergyReservation>(bson);
        Assert.Equal(reservation.EnergyAmount, result.EnergyAmount);
        Assert.Equal(time, result.ReservationDateTime);
        Assert.Equal(DateTimeKind.Utc, result.ReservationDateTime.Kind);
        Assert.Equal(DateTimeKind.Utc, result.CompletedAt!.Value.Kind);
        Assert.Equal("991234567V", result.ProsumerNIC);
        Assert.DoesNotContain("test-token-hash", JsonSerializer.Serialize(result));
    }

    [Fact]
    public void Station_has_one_GeoJSON_coordinate_source_and_roundtrips_schedule()
    {
        var station = NewStation("TEST-S1");
        station.OperatingSchedule.WeeklyPeriods.Add(new OperatingPeriod
            { Day = DayOfWeek.Monday, OpenMinuteOfDay = 480, CloseMinuteOfDay = 1020 });
        var bson = station.ToBsonDocument();
        Assert.False(bson.Contains("Latitude"));
        Assert.False(bson.Contains("Longitude"));
        Assert.Equal("Point", bson["Location"]["type"].AsString);
        Assert.Equal(79.8612, bson["Location"]["coordinates"][0].AsDouble);
        Assert.Equal(6.9271, bson["Location"]["coordinates"][1].AsDouble);
        Assert.Equal(BsonType.Decimal128, bson["CapacityKWh"].BsonType);
        var result = BsonSerializer.Deserialize<SolarStationInfo>(bson);
        Assert.Equal(station.Latitude, result.Latitude);
        Assert.Equal(station.Longitude, result.Longitude);
        Assert.Equal(DayOfWeek.Monday, Assert.Single(result.OperatingSchedule.WeeklyPeriods).Day);
    }

    [Fact]
    public void Slot_capacity_is_decimal_and_times_are_UTC()
    {
        var slot = new EnergyBookingSlot
        {
            StationId = ObjectId.GenerateNewId(), Capacity = 20.123456789m,
            AvailableCapacity = 19.987654321m, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1)
        };
        var bson = slot.ToBsonDocument();
        Assert.Equal(BsonType.Decimal128, bson["Capacity"].BsonType);
        Assert.Equal(BsonType.Decimal128, bson["AvailableCapacity"].BsonType);
        Assert.Equal("CLOSED", bson["Status"].AsString);
        var result = BsonSerializer.Deserialize<EnergyBookingSlot>(bson);
        Assert.Equal(slot.AvailableCapacity, result.AvailableCapacity);
        Assert.Equal(DateTimeKind.Utc, result.StartTime.Kind);
    }

    internal static User NewUser(string? nic) => new()
    {
        NIC = nic, FullName = "Test User", Email = "test@example.invalid", Phone = "0000000000",
        PasswordHash = "test-hash-not-a-password"
    };

    internal static SolarStationInfo NewStation(string code) => new()
    {
        StationCode = code, Name = "Test Station", Address = "Test Address",
        Location = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new(79.8612, 6.9271)), CapacityKWh = 50m
    };
}
