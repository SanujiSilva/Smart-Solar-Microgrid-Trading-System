/*
 * File: src/SmartSolarMicrogrid.Api/Models/User.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Persistence model definitions for User.
 */
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolarMicrogrid.Api.Models;

public sealed class User
{
    // Stable authentication reference, retained across the NIC primary-key migration.
    [BsonElement("UserId")]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    [BsonId, JsonIgnore]
    public BsonValue PrimaryKey
    {
        get => Role == UserRole.PROSUMER
            ? new BsonString(NIC ?? throw new InvalidOperationException("A prosumer requires a NIC primary key."))
            : new BsonObjectId(Id);
        set
        {
            if (value.IsString) NIC = value.AsString;
            else Id = value.AsObjectId;
        }
    }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    private string? nic;

    [BsonIgnoreIfNull]
    public string? NIC
    {
        get => nic;
        set => nic = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }

    [JsonIgnore]
    public string PasswordHash { get; set; } = "";

    // Increment when future password/session revocation operations invalidate existing tokens.
    [JsonIgnore]
    public int TokenVersion { get; set; }

    [JsonIgnore]
    public long Revision { get; set; }

    [BsonRepresentation(BsonType.String)]
    public UserRole Role { get; set; } = UserRole.PROSUMER;

    [BsonRepresentation(BsonType.String)]
    public UserStatus Status { get; set; } = UserStatus.PENDING;
}
