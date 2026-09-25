/*
 * File: src/SmartSolarMicrogrid.Api/DTOs/HealthResponse.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: API request/response contracts and validation for Health Response.
 */
namespace SmartSolarMicrogrid.Api.DTOs;

public sealed record HealthResponse(string Status, DateTimeOffset TimestampUtc);
