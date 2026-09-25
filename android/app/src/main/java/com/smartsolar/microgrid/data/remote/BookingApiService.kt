package com.smartsolar.microgrid.data.remote

import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

interface BookingApiService {
    @GET("stations")
    suspend fun stations(@Query("page") page: Int, @Query("search") search: String?,
        @Query("pageSize") pageSize: Int = 20, @Query("status") status: String? = "ACTIVE"): StationPage

    @GET("stations/{id}")
    suspend fun station(@Path("id") id: String): BookingStation

    @GET("stations/{id}/slots")
    suspend fun slots(@Path("id") stationId: String): BookingSlots

    @GET("slots/{id}")
    suspend fun slot(@Path("id") id: String): BookingSlot

    @POST("reservations")
    suspend fun create(@Body request: CreateBookingRequest): Booking

    @GET("reservations/{id}")
    suspend fun booking(@Path("id") id: String): Booking

    @PUT("reservations/{id}")
    suspend fun update(@Path("id") id: String, @Body request: UpdateBookingRequest): Booking

    @DELETE("reservations/{id}")
    suspend fun cancel(@Path("id") id: String): Booking

    @GET("reservations/my")
    suspend fun current(): BookingList

    @GET("reservations/history")
    suspend fun history(): BookingList

    @GET("reservations/search")
    suspend fun search(@Query("page") page: Int, @Query("reservationCode") code: String?,
        @Query("stationId") stationId: String?, @Query("status") status: String?,
        @Query("from") from: String?, @Query("to") to: String?,
        @Query("pageSize") pageSize: Int = 20): BookingPage
}
