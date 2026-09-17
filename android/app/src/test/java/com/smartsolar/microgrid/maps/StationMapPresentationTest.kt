package com.smartsolar.microgrid.maps

import com.smartsolar.microgrid.data.remote.StationSummary
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class StationMapPresentationTest {
    @Test
    fun parseCoordinatesAcceptsValidInput() {
        val input = StationMapPresentation.parseCoordinates(" 6.9271 ", "79.8612", "10")

        assertEquals(6.9271, input?.latitude ?: 0.0, 0.0001)
        assertEquals(79.8612, input?.longitude ?: 0.0, 0.0001)
        assertEquals(10.0, input?.radiusKm ?: 0.0, 0.0001)
    }

    @Test
    fun parseCoordinatesRejectsImpossibleValues() {
        assertNull(StationMapPresentation.parseCoordinates("91", "79", "5"))
        assertNull(StationMapPresentation.parseCoordinates("6", "181", "5"))
        assertNull(StationMapPresentation.parseCoordinates("6", "79", "0"))
        assertNull(StationMapPresentation.parseCoordinates("abc", "79", "5"))
    }

    @Test
    fun markerPresentationKeepsStationIdentityAndAvailability() {
        val marker = StationMapPresentation.markerFor(
            StationSummary(
                id = "station-1",
                stationCode = "COL-01",
                name = "Colombo Central",
                address = "Colombo",
                latitude = 6.9271,
                longitude = 79.8612,
                capacityKWh = 100.0,
                availableBatterySlots = 3,
                status = "ACTIVE",
            ),
        )

        assertEquals("station-1", marker.id)
        assertEquals("Colombo Central", marker.title)
        assertEquals("COL-01 - 3 slots", marker.snippet)
    }
}
