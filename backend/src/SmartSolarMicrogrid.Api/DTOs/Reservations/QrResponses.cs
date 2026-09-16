namespace SmartSolarMicrogrid.Api.DTOs.Reservations;

public sealed record QrTokenResponse(string QrToken, ReservationResponse Reservation);
public sealed record QrVerificationResponse(bool Valid, ReservationResponse Reservation);
