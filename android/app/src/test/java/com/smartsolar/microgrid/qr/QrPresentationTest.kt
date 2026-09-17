package com.smartsolar.microgrid.qr

import com.smartsolar.microgrid.data.remote.Booking
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.math.BigDecimal

class QrPresentationTest {
    @Test
    fun normalizeTokenTrimsAndRejectsShortValues() {
        assertEquals("abcdefghijklmnopqrstuvwxyz", QrPresentation.normalizeToken("  abcdefghijklmnopqrstuvwxyz  "))
        assertNull(QrPresentation.normalizeToken("too-short"))
    }

    @Test
    fun reservationSummaryIncludesTransferFields() {
        val summary = QrPresentation.reservationSummary(booking())

        assertTrue(summary.contains("RSV-1"))
        assertTrue(summary.contains("APPROVED"))
        assertTrue(summary.contains("2.5 kWh"))
        assertTrue(summary.contains("200012345678"))
    }

    private fun booking() = Booking(
        id = "r1",
        reservationCode = "RSV-1",
        prosumerNIC = "200012345678",
        stationId = "s1",
        slotId = "t1",
        energyAmount = BigDecimal("2.5"),
        reservationDateTime = "2030-01-01T08:00:00Z",
        status = "APPROVED",
        createdAt = "2029-12-30T00:00:00Z",
        updatedAt = "2029-12-30T00:00:00Z",
        completedAt = null,
        completedByOperatorId = null,
    )
}
