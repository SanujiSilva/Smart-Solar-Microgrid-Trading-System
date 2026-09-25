package com.smartsolar.microgrid.data.local

/** Server-returned display/session reference data. Passwords and bearer tokens are never stored here. */
data class LocalUser(
    val userId: String,
    val nic: String?,
    val fullName: String,
    val email: String,
    val phone: String,
    val role: String,
    val status: String,
)

data class CachedStation(
    val stationId: String,
    val stationCode: String,
    val name: String,
    val address: String,
    val latitude: Double,
    val longitude: Double,
    val capacityKWh: Double,
    val availableBatterySlots: Int,
    val status: String,
    val fetchedAtEpochMillis: Long,
)
