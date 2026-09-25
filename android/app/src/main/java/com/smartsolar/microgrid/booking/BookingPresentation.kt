package com.smartsolar.microgrid.booking

import java.math.BigDecimal
import java.time.LocalDate
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.time.format.FormatStyle

object BookingPresentation {
    fun time(value: String): String = OffsetDateTime.parse(value).atZoneSameInstant(ZoneId.systemDefault())
        .format(DateTimeFormatter.ofLocalizedDateTime(FormatStyle.MEDIUM))

    fun energy(value: String): BigDecimal? {
        val normalized = value.trim().replace(',', '.')
        if (!Regex("[0-9]+(?:\\.[0-9]+)?").matches(normalized)) return null
        return normalized.toBigDecimalOrNull()?.takeIf { it > BigDecimal.ZERO }
    }

    // Search dates represent complete local calendar days, converted to the API's inclusive/exclusive range.
    fun fromDate(value: String, zone: ZoneId = ZoneId.systemDefault()): String? = value.takeIf { it.isNotBlank() }
        ?.let { LocalDate.parse(it).atStartOfDay(zone).toOffsetDateTime().toString() }

    fun throughDate(value: String, zone: ZoneId = ZoneId.systemDefault()): String? = value.takeIf { it.isNotBlank() }
        ?.let { LocalDate.parse(it).plusDays(1).atStartOfDay(zone).toOffsetDateTime().toString() }
}
