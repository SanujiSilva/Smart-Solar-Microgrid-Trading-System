package com.smartsolar.microgrid.data.remote

data class LoginRequest(
    val identifier: String,
    val password: String,
)

data class AuthUserResponse(
    val id: String,
    val nic: String?,
    val fullName: String,
    val email: String,
    val phone: String,
    val role: String,
    val status: String,
)

data class LoginResponse(
    val accessToken: String,
    val tokenType: String,
    val expiresAtUtc: String,
    val user: AuthUserResponse,
)