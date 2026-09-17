package com.smartsolar.microgrid.data.remote

import android.content.Context
import com.smartsolar.microgrid.data.security.SecureTokenStore
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object ApiClient {
    fun authService(context: Context): AuthApiService = retrofit(context).create(AuthApiService::class.java)

    fun bookingService(context: Context): BookingApiService = retrofit(context, false).create(BookingApiService::class.java)

    fun qrService(context: Context): QrApiService = retrofit(context, false).create(QrApiService::class.java)

    private fun retrofit(context: Context, retryConnections: Boolean = true): Retrofit {
        val tokenStore = SecureTokenStore(context.applicationContext)
        val logging = HttpLoggingInterceptor().apply { level = HttpLoggingInterceptor.Level.BASIC }
        val client = OkHttpClient.Builder()
            .retryOnConnectionFailure(retryConnections)
            .addInterceptor { chain ->
                val request = chain.request().newBuilder()
                tokenStore.readToken()?.let { request.header("Authorization", "Bearer $it") }
                chain.proceed(request.build())
            }
            .addInterceptor(logging)
            .build()
        return Retrofit.Builder()
            .baseUrl(ApiConfig.BASE_URL)
            .client(client)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
    }
}
