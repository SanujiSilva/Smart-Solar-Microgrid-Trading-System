package com.smartsolar.microgrid.booking

import org.junit.Assert.*
import org.junit.Test
import java.time.ZoneId

class BookingPresentationTest {
    @Test fun energyKeepsDecimalPrecisionAndAcceptsDecimalComma() {
        assertEquals("123.456789", BookingPresentation.energy("123.456789")?.toPlainString())
        assertEquals("2.5", BookingPresentation.energy(" 2,5 ")?.toPlainString())
        for (value in listOf("", "0", "-1", "NaN", "1e4", "1,000,000", "2.3.4"))
            assertNull(BookingPresentation.energy(value))
    }

    @Test fun dateFiltersIncludeTheEntireSelectedLocalDay() {
        val zone = ZoneId.of("Asia/Colombo")
        assertEquals("2030-01-01T00:00+05:30", BookingPresentation.fromDate("2030-01-01", zone))
        assertEquals("2030-01-02T00:00+05:30", BookingPresentation.throughDate("2030-01-01", zone))
        assertNull(BookingPresentation.fromDate("", zone))
    }

    @Test fun dateRangeFollowsDaylightSavingCalendarRatherThanTwentyFourHours() {
        val zone = ZoneId.of("America/New_York")
        assertEquals("2026-03-08T00:00-05:00", BookingPresentation.fromDate("2026-03-08", zone))
        assertEquals("2026-03-09T00:00-04:00", BookingPresentation.throughDate("2026-03-08", zone))
    }
}
