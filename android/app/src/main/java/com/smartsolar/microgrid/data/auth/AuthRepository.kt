package com.smartsolar.microgrid.data.auth

import android.content.Context
import com.smartsolar.microgrid.data.local.LocalDatabaseHelper
import com.smartsolar.microgrid.data.local.LocalRepository
import com.smartsolar.microgrid.data.local.LocalUser
import com.smartsolar.microgrid.data.remote.ApiClient
import com.smartsolar.microgrid.data.remote.AuthUserResponse
import com.smartsolar.microgrid.data.remote.LoginRequest
import com.smartsolar.microgrid.data.remote.RegisterProsumerRequest
import com.smartsolar.microgrid.data.remote.UpdateProfileRequest
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import retrofit2.HttpException
import com.smartsolar.microgrid.data.security.SecureTokenStore

class AuthRepository(context: Context) {
    private val applicationContext = context.applicationContext
    private val tokenStore = SecureTokenStore(applicationContext)
    private val localDatabase = LocalDatabaseHelper(applicationContext)
    private val localRepository = LocalRepository(localDatabase)
    private val api = ApiClient.authService(applicationContext)

    suspend fun login(identifier: String, password: String): LocalUser = withContext(Dispatchers.IO) {
        val response = api.login(LoginRequest(identifier, password))
        tokenStore.saveToken(response.accessToken)
        val user = response.user.toLocalUser()
        localRepository.saveUser(user)
        user
    }

    suspend fun restoreSession(): LocalUser? {
        if (tokenStore.readToken() == null) return null
        return try {
            currentUser()
        } catch (error: HttpException) {
            if (error.code() != 401) throw error
            withContext(Dispatchers.IO) { logout() }
            null
        }
    }

    suspend fun register(request: RegisterProsumerRequest) = api.register(request)

    suspend fun currentUser(): LocalUser = withContext(Dispatchers.IO) {
        cacheUser(api.me())
    }

    suspend fun updateProfile(request: UpdateProfileRequest): LocalUser = withContext(Dispatchers.IO) {
        cacheUser(api.updateProfile(request))
    }

    suspend fun requestDeactivation(): LocalUser = withContext(Dispatchers.IO) {
        cacheUser(api.requestDeactivation())
    }

    suspend fun dashboard() = api.dashboard()

    suspend fun nearby(latitude: Double, longitude: Double, radiusKm: Double) = withContext(Dispatchers.IO) {
        api.nearby(latitude, longitude, radiusKm).also { response ->
            localRepository.replaceStationCache(response.items.map {
                com.smartsolar.microgrid.data.local.CachedStation(it.id, it.stationCode, it.name, it.address,
                    it.latitude, it.longitude, it.capacityKWh, it.availableBatterySlots, it.status, System.currentTimeMillis())
            })
        }
    }

    private fun cacheUser(response: AuthUserResponse): LocalUser = response.toLocalUser().also {
        localRepository.saveUser(it)
    }

    fun logout() {
        tokenStore.clear()
        localRepository.clearUser()
    }

    fun hasSession(): Boolean = tokenStore.readToken() != null

    fun close() = localRepository.close()

    private fun AuthUserResponse.toLocalUser() = LocalUser(
        userId = id,
        nic = nic,
        fullName = fullName,
        email = email,
        phone = phone,
        role = role,
        status = status,
    )
}
