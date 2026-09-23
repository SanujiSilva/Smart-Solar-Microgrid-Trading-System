package com.smartsolar.microgrid.data.remote

import java.math.BigDecimal

data class StationPage(val items: List<BookingStation>, val totalCount: Long, val page: Int, val pageSize: Int)
data class BookingStation(val id: String, val stationCode: String, val name: String, val address: String,
    val latitude: Double, val longitude: Double, val capacityKWh: BigDecimal,
    val availableBatterySlots: Int, val status: String, val operatingSchedule: BookingSchedule)
data class BookingSchedule(val timeZoneId: String, val weeklyPeriods: List<BookingPeriod>)
data class BookingPeriod(val day: String, val openMinuteOfDay: Int, val closeMinuteOfDay: Int)
data class BookingSlots(val items: List<BookingSlot>)
data class BookingSlot(val id: String, val stationId: String, val startTime: String, val endTime: String,
    val capacity: BigDecimal, val availableCapacity: BigDecimal, val status: String)
data class CreateBookingRequest(val slotId: String, val energyAmount: BigDecimal)
data class UpdateBookingRequest(val energyAmount: BigDecimal, val slotId: String? = null)
data class Booking(val id: String, val reservationCode: String, val prosumerNIC: String, val stationId: String,
    val slotId: String, val energyAmount: BigDecimal, val reservationDateTime: String, val status: String,
    val createdAt: String, val updatedAt: String, val completedAt: String?, val completedByOperatorId: String?)
data class BookingList(val items: List<Booking>)
data class BookingPage(val items: List<Booking>, val totalCount: Long, val page: Int, val pageSize: Int)
