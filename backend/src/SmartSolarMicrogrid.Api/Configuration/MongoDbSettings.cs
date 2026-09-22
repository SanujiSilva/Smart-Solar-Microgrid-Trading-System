/*
 * File: src/SmartSolarMicrogrid.Api/Configuration/MongoDbSettings.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Service setup and configuration for Mongo Db Settings.
 */
using System.Text.RegularExpressions;
using MongoDB.Driver;

namespace SmartSolarMicrogrid.Api.Configuration;

public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDb";
    public string ConnectionString { get; init; } = "";
    public string DatabaseName { get; init; } = "SmartSolarMicrogrid";
    public int TimeoutSeconds { get; init; } = 5;
    public int InitializationTimeoutSeconds { get; init; } = 30;

    public static bool IsValidConnectionString(string? value)
    {
        // Is Valid Connection String for Mongo Db Settings.
        if (string.IsNullOrWhiteSpace(value) ||
            !(value.StartsWith("mongodb://", StringComparison.Ordinal) ||
              value.StartsWith("mongodb+srv://", StringComparison.Ordinal)))
            return false;

        try { _ = MongoUrl.Create(value); return true; }
        catch (Exception ex) when (ex is ArgumentException or FormatException or MongoConfigurationException)
        { return false; }
    }

    // Is Valid Database Name for Mongo Db Settings.
    public static bool IsValidDatabaseName(string? value) =>
        value is not null && Regex.IsMatch(value, "\\A[A-Za-z0-9][A-Za-z0-9_-]{0,62}\\z") &&
        !new[] { "admin", "local", "config" }.Contains(value, StringComparer.OrdinalIgnoreCase);
}
