package com.smartsolar.microgrid.data.remote

import com.google.gson.JsonParser
import kotlinx.coroutines.runBlocking
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

class QrApiTest {
    private lateinit var server: MockWebServer
    private lateinit var api: QrApiService
    private val booking = """{"id":"r1","reservationCode":"RSV-1","prosumerNIC":"200012345678","stationId":"s1","slotId":"t1","energyAmount":2.5,"reservationDateTime":"2030-01-01T08:00:00Z","status":"APPROVED","createdAt":"2029-12-30T00:00:00Z","updatedAt":"2029-12-30T00:00:00Z","completedAt":null,"completedByOperatorId":null}"""

    @Before fun setUp() {
        server = MockWebServer()
        server.start()
        api = Retrofit.Builder()
            .baseUrl(server.url("/api/"))
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(QrApiService::class.java)
    }

    @After fun tearDown() = server.shutdown()

    @Test fun issueUsesReservationQrRouteAndReadsToken() = runBlocking {
        server.enqueue(MockResponse().setBody("""{"qrToken":"abcdefghijklmnopqrstuvwxyz","reservation":$booking}"""))

        val response = api.issue("r1")

        val request = server.takeRequest()
        assertEquals("GET", request.method)
        assertEquals("/api/reservations/r1/qr", request.path)
        assertEquals("abcdefghijklmnopqrstuvwxyz", response.qrToken)
        assertEquals("APPROVED", response.reservation.status)
    }

    @Test fun verifyPostsOnlyQrTokenToOperatorRoute() = runBlocking {
        server.enqueue(MockResponse().setBody("""{"valid":true,"reservation":$booking}"""))

        val response = api.verify(QrTokenRequest("abcdefghijklmnopqrstuvwxyz"))

        val request = server.takeRequest()
        assertEquals("POST", request.method)
        assertEquals("/api/operator/verify-qr", request.path)
        val body = JsonParser.parseString(request.body.readUtf8()).asJsonObject
        assertEquals(setOf("qrToken"), body.keySet())
        assertEquals("abcdefghijklmnopqrstuvwxyz", body["qrToken"].asString)
        assertTrue(response.valid)
    }

    @Test fun completePostsOnlyQrTokenAndReadsCompletedReservation() = runBlocking {
        server.enqueue(MockResponse().setBody(booking.replace("APPROVED", "COMPLETED")))

        val response = api.complete(QrTokenRequest("abcdefghijklmnopqrstuvwxyz"))

        val request = server.takeRequest()
        assertEquals("POST", request.method)
        assertEquals("/api/operator/complete-transfer", request.path)
        assertEquals(setOf("qrToken"), JsonParser.parseString(request.body.readUtf8()).asJsonObject.keySet())
        assertEquals("COMPLETED", response.status)
    }

    @Test fun invalidVerificationResponseStaysExplicit() = runBlocking {
        server.enqueue(MockResponse().setBody("""{"valid":false,"reservation":$booking}"""))

        assertFalse(api.verify(QrTokenRequest("abcdefghijklmnopqrstuvwxyz")).valid)
    }
}
