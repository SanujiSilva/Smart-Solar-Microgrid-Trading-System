package com.smartsolar.microgrid.maps

import com.smartsolar.microgrid.data.remote.StationSummary

data class CoordinateInput(val latitude: Double, val longitude: Double, val radiusKm: Double)

data class StationMarkerPresentation(
    val id: String,
    val latitude: Double,
    val longitude: Double,
    val title: String,
    val snippet: String,
)

object StationMapPresentation {
    fun parseCoordinates(latitude: String, longitude: String, radiusKm: String): CoordinateInput? {
        val lat = latitude.trim().toDoubleOrNull()
        val lng = longitude.trim().toDoubleOrNull()
        val radius = radiusKm.trim().toDoubleOrNull()
        if (lat == null || lng == null || radius == null) return null
        if (!lat.isFinite() || !lng.isFinite() || !radius.isFinite()) return null
        if (lat !in -90.0..90.0 || lng !in -180.0..180.0 || radius <= 0.0) return null
        return CoordinateInput(lat, lng, radius)
    }

    fun markerFor(station: StationSummary): StationMarkerPresentation = StationMarkerPresentation(
        id = station.id,
        latitude = station.latitude,
        longitude = station.longitude,
        title = station.name,
        snippet = "${station.stationCode} - ${station.availableBatterySlots} slots",
    )
}
