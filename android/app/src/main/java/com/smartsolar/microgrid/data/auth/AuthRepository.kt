package com.smartsolar.microgrid.data.auth

import android.content.Context
import com.smartsolar.microgrid.data.local.LocalDatabaseHelper
import com.smartsolar.microgrid.data.local.LocalRepository
import com.smartsolar.microgrid.data.local.LocalUser
import com.smartsolar.microgrid.data.remote.ApiClient
import com.smartsolar.microgrid.data.remote.AuthUserResponse
import com.smartsolar.microgrid.data.remote.LoginRequest
import com.smartsolar.microgrid.data.security.SecureTokenStore

class AuthRepository(context: Context) {
    private val applicationContext = context.applicationContext
    private val tokenStore = SecureTokenStore(applicationContext)
    private val localDatabase = LocalDatabaseHelper(applicationContext)
    private val localRepository = LocalRepository(localDatabase)
    private val api = ApiClient.authService(applicationContext)

    suspend fun login(identifier: String, password: String): LocalUser {
        val response = api.login(LoginRequest(identifier, password))
        tokenStore.saveToken(response.accessToken)
        val user = response.user.toLocalUser()
        localRepository.saveUser(user)
        return user
    }

    suspend fun restoreSession(): LocalUser? {
        if (tokenStore.readToken() == null) return null
        return runCatching {
            val user = api.me().toLocalUser()
            localRepository.saveUser(user)
            user
        }.getOrElse {
            logout()
            null
        }
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
