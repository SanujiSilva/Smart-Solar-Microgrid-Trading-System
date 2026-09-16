package com.smartsolar.microgrid

import okhttp3.MediaType.Companion.toMediaType
import okhttp3.ResponseBody.Companion.toResponseBody
import org.junit.Assert.*
import org.junit.Test
import retrofit2.HttpException
import retrofit2.Response

class AccountErrorTest {
    private fun failure(code: Int, body: String) = HttpException(
        Response.error<Any>(code, body.toResponseBody("application/problem+json".toMediaType())),
    )

    @Test fun displaysBackendConflictTitle() {
        assertEquals("Email is already registered.", accountError(failure(409,
            """{"title":"Email is already registered.","status":409}""")))
    }

    @Test fun displaysValidationMessagesAndHandlesMalformedResponse() {
        assertEquals("NIC is invalid.\nEmail is invalid.", accountError(failure(400,
            """{"errors":{"NIC":["NIC is invalid."],"Email":["Email is invalid."]}}""")))
        assertEquals("Check your details and try again.", accountError(failure(400, "<html>offline</html>")))
    }

    @Test fun hidesUnexpectedServerDetailsAndExplainsThrottling() {
        assertFalse(accountError(failure(500, """{"detail":"private diagnostics"}""")).contains("private"))
        assertTrue(accountError(failure(429, "")).contains("Wait a minute"))
    }
}
