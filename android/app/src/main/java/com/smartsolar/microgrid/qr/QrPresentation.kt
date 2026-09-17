package com.smartsolar.microgrid.qr

import android.graphics.Bitmap
import com.google.zxing.BarcodeFormat
import com.google.zxing.EncodeHintType
import com.google.zxing.qrcode.QRCodeWriter
import com.google.zxing.qrcode.decoder.ErrorCorrectionLevel
import com.smartsolar.microgrid.booking.BookingPresentation
import com.smartsolar.microgrid.data.remote.Booking
import java.util.EnumMap

object QrPresentation {
    fun normalizeToken(value: String): String? = value.trim().takeIf { it.length >= 20 }

    fun reservationSummary(booking: Booking): String =
        "Code: ${booking.reservationCode}\nStatus: ${booking.status}\nScheduled: ${BookingPresentation.time(booking.reservationDateTime)}\nEnergy: ${booking.energyAmount.toPlainString()} kWh\nNIC: ${booking.prosumerNIC}"

    fun qrBitmap(token: String, size: Int = 720): Bitmap {
        val hints = EnumMap<EncodeHintType, Any>(EncodeHintType::class.java).apply {
            put(EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.M)
            put(EncodeHintType.MARGIN, 2)
        }
        val matrix = QRCodeWriter().encode(token, BarcodeFormat.QR_CODE, size, size, hints)
        val pixels = IntArray(size * size)
        for (y in 0 until size) {
            val offset = y * size
            for (x in 0 until size) {
                pixels[offset + x] = if (matrix[x, y]) 0xFF000000.toInt() else 0xFFFFFFFF.toInt()
            }
        }
        return Bitmap.createBitmap(size, size, Bitmap.Config.ARGB_8888).apply {
            setPixels(pixels, 0, size, 0, 0, size, size)
        }
    }
}
