package com.smartsolar.microgrid.data.remote

data class RegisterProsumerRequest(
    val nic: String, val fullName: String, val email: String, val phone: String, val password: String,
)

data class UpdateProfileRequest(val fullName: String, val email: String, val phone: String)

data class DashboardResponse(
    val role: String,
    val pendingReservations: Long,
    val approvedFutureReservations: Long,
    val todayReservations: Long,
    val completedTransfers: Long,
    val activeStations: Long,
    val openSlots: Long,
    val availableSlotCapacity: java.math.BigDecimal,
    val activeReservations: Long,
    val recentReservations: List<ReservationSummary>,
)

data class ReservationSummary(val reservationCode: String, val reservationDateTime: String,
    val energyAmount: java.math.BigDecimal, val status: String)

data class StationSummary(val id: String, val stationCode: String, val name: String, val address: String,
    val latitude: Double, val longitude: Double, val capacityKWh: Double,
    val availableBatterySlots: Int, val status: String)

data class NearbyStationsResponse(val items: List<StationSummary>)

data class LoginRequest(
    val identifier: String,
    val password: String,
)

data class AuthUserResponse(
    val id: String,
    val nic: String?,
    val fullName: String,
    val email: String,
    val phone: String,
    val role: String,
    val status: String,
)

data class LoginResponse(
    val accessToken: String,
    val tokenType: String,
    val expiresAtUtc: String,
    val user: AuthUserResponse,
)
