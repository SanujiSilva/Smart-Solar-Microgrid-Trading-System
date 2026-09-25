package com.smartsolar.microgrid.data.remote

import com.google.gson.JsonParser
import kotlinx.coroutines.runBlocking
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import org.junit.After
import org.junit.Assert.*
import org.junit.Before
import org.junit.Test
import retrofit2.HttpException
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

class ProsumerApiTest {
    private lateinit var server: MockWebServer
    private lateinit var api: AuthApiService
    private val user = """{"id":"u1","nic":"200012345678","fullName":"Test User","email":"user@example.test","phone":"0771234567","role":"PROSUMER","status":"ACTIVE"}"""

    @Before fun setUp() {
        server = MockWebServer()
        server.start()
        api = Retrofit.Builder().baseUrl(server.url("/api/"))
            .addConverterFactory(GsonConverterFactory.create()).build().create(AuthApiService::class.java)
    }

    @After fun tearDown() = server.shutdown()

    @Test fun registrationUsesPublicContractAndReturnsPendingUser() = runBlocking {
        server.enqueue(MockResponse().setResponseCode(201).setBody(user.replace("ACTIVE", "PENDING")))
        val result = api.register(RegisterProsumerRequest("200012345678", "Test User",
            "user@example.test", "0771234567", "test-password-123"))
        val request = server.takeRequest()
        assertEquals("POST", request.method)
        assertEquals("/api/auth/prosumer/register", request.path)
        val body = JsonParser.parseString(request.body.readUtf8()).asJsonObject
        assertEquals(setOf("nic", "fullName", "email", "phone", "password"), body.keySet())
        assertEquals("PENDING", result.status)
    }

    @Test fun profileCannotSendIdentityRoleOrStatusChanges() = runBlocking {
        server.enqueue(MockResponse().setBody(user))
        assertEquals("u1", api.updateProfile(UpdateProfileRequest("Test User",
            "user@example.test", "0771234567")).id)
        val request = server.takeRequest()
        assertEquals("PUT", request.method)
        assertEquals("/api/prosumers/me", request.path)
        assertEquals(setOf("fullName", "email", "phone"),
            JsonParser.parseString(request.body.readUtf8()).asJsonObject.keySet())
    }

    @Test fun deactivationUsesAuthenticatedSelfRouteWithNoIdentityBody() = runBlocking {
        server.enqueue(MockResponse().setBody(user.replace("ACTIVE", "DEACTIVATION_REQUESTED")))
        assertEquals("DEACTIVATION_REQUESTED", api.requestDeactivation().status)
        val request = server.takeRequest()
        assertEquals("POST", request.method)
        assertEquals("/api/prosumers/me/deactivation-request", request.path)
        assertEquals(0L, request.bodySize)
    }

    @Test fun dashboardPreservesServerCountsAndDecimalCapacity() = runBlocking {
        server.enqueue(MockResponse().setBody("""{"role":"PROSUMER","pendingReservations":2,"approvedFutureReservations":3,"todayReservations":4,"completedTransfers":1,"activeStations":5,"openSlots":7,"availableSlotCapacity":123.45,"recentReservations":[]}"""))
        val result = api.dashboard()
        assertEquals("/api/reservations/dashboard", server.takeRequest().path)
        assertEquals(2L, result.pendingReservations)
        assertEquals(1L, result.completedTransfers)
        assertEquals("123.45", result.availableSlotCapacity.toPlainString())
    }

    @Test fun validationAndExpiredSessionRemainFailuresForUiHandling() = runBlocking {
        for (status in listOf(400, 401, 403, 409, 429, 500)) {
            server.enqueue(MockResponse().setResponseCode(status).setBody("""{"detail":"Rejected"}"""))
            try {
                api.updateProfile(UpdateProfileRequest("Test User", "user@example.test", "0771234567"))
                fail("Expected HTTP $status")
            } catch (error: HttpException) {
                assertEquals(status, error.code())
            }
            server.takeRequest()
        }
    }

    @Test fun nearbyStationsUsesCoordinatesAndServerResults() = runBlocking {
        server.enqueue(MockResponse().setBody("""{"items":[{"id":"s1","stationCode":"ST-01","name":"Station","address":"Colombo","latitude":6.9,"longitude":79.8,"capacityKWh":50,"availableBatterySlots":3,"status":"ACTIVE"}]}"""))
        val response = api.nearby(6.9, 79.8, 10.0)
        assertEquals("/api/stations/nearby?latitude=6.9&longitude=79.8&radiusKm=10.0", server.takeRequest().path)
        assertEquals("s1", response.items.single().id)
        assertEquals(3, response.items.single().availableBatterySlots)
    }
}
