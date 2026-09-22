/*
 * File: src/SmartSolarMicrogrid.Api/DTOs/Reservations/QrResponses.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: API request/response contracts and validation for Qr Responses.
 */
namespace SmartSolarMicrogrid.Api.DTOs.Reservations;

public sealed record QrTokenResponse(string QrToken, ReservationResponse Reservation);
public sealed record QrVerificationResponse(bool Valid, ReservationResponse Reservation);
