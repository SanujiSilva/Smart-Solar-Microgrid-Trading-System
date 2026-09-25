package com.smartsolar.microgrid.data.remote

import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path

interface QrApiService {
    @GET("reservations/{id}/qr")
    suspend fun issue(@Path("id") id: String): QrTokenResponse

    @POST("operator/verify-qr")
    suspend fun verify(@Body request: QrTokenRequest): QrVerificationResponse

    @POST("operator/complete-transfer")
    suspend fun complete(@Body request: QrTokenRequest): Booking
}

data class QrTokenRequest(val qrToken: String)
data class QrTokenResponse(val qrToken: String, val reservation: Booking)
data class QrVerificationResponse(val valid: Boolean, val reservation: Booking)
