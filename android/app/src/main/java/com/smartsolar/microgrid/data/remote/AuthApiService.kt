package com.smartsolar.microgrid.data.remote

import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Query

interface AuthApiService {
    @GET("stations/nearby")
    suspend fun nearby(@Query("latitude") latitude: Double, @Query("longitude") longitude: Double,
        @Query("radiusKm") radiusKm: Double): NearbyStationsResponse
    @POST("auth/prosumer/register")
    suspend fun register(@Body request: RegisterProsumerRequest): AuthUserResponse

    @PUT("prosumers/me")
    suspend fun updateProfile(@Body request: UpdateProfileRequest): AuthUserResponse

    @POST("prosumers/me/deactivation-request")
    suspend fun requestDeactivation(): AuthUserResponse

    @GET("reservations/dashboard")
    suspend fun dashboard(): DashboardResponse

    @POST("auth/login")
    suspend fun login(@Body request: LoginRequest): LoginResponse

    @GET("auth/me")
    suspend fun me(): AuthUserResponse
}
