using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolarMicrogrid.Api.Models;

public sealed class User : MongoDocument
{
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
