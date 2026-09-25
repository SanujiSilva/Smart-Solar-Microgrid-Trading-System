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
import java.math.BigDecimal

class BookingApiTest {
    private lateinit var server: MockWebServer
    private lateinit var api: BookingApiService
    private val booking = """{"id":"r1","reservationCode":"RSV-1","prosumerNIC":"200012345678","stationId":"s1","slotId":"t1","energyAmount":2.123456,"reservationDateTime":"2030-01-01T08:00:00Z","status":"PENDING","createdAt":"2029-12-30T00:00:00Z","updatedAt":"2029-12-30T00:00:00Z","completedAt":null,"completedByOperatorId":null}"""

    @Before fun setUp() {
        server = MockWebServer(); server.start()
        api = Retrofit.Builder().baseUrl(server.url("/api/")).addConverterFactory(GsonConverterFactory.create())
            .build().create(BookingApiService::class.java)
    }
    @After fun tearDown() = server.shutdown()

    @Test fun createSendsOnlySlotAndExactEnergyAndReadsPendingConfirmation() = runBlocking {
        server.enqueue(MockResponse().setResponseCode(201).setBody(booking))
        val result = api.create(CreateBookingRequest("t1", BigDecimal("2.123456")))
        val request = server.takeRequest()
        assertEquals("POST", request.method)
        assertEquals("/api/reservations", request.path)
        val body = JsonParser.parseString(request.body.readUtf8()).asJsonObject
        assertEquals(setOf("slotId", "energyAmount"), body.keySet())
        assertEquals(BigDecimal("2.123456"), body["energyAmount"].asBigDecimal)
        assertEquals("PENDING", result.status)
        assertEquals("RSV-1", result.reservationCode)
    }

    @Test fun detailsModificationAndCancellationUseReservationRoutes() = runBlocking {
        server.enqueue(MockResponse().setBody(booking))
        api.booking("r1")
        assertEquals("/api/reservations/r1", server.takeRequest().path)
        server.enqueue(MockResponse().setBody(booking))
        api.update("r1", UpdateBookingRequest(BigDecimal("3.5")))
        val update = server.takeRequest()
        assertEquals("PUT", update.method)
        assertEquals(setOf("energyAmount"), JsonParser.parseString(update.body.readUtf8()).asJsonObject.keySet())
        server.enqueue(MockResponse().setBody(booking.replace("PENDING", "CANCELLED")))
        assertEquals("CANCELLED", api.cancel("r1").status)
        val cancel = server.takeRequest()
        assertEquals("DELETE", cancel.method)
        assertEquals(0L, cancel.bodySize)
    }

    @Test fun currentHistoryAndSearchNeverSupplyProsumerIdentity() = runBlocking {
        for (history in listOf(false, true)) {
            server.enqueue(MockResponse().setBody("""{"items":[$booking]}"""))
            if (history) api.history() else api.current()
            assertEquals(if (history) "/api/reservations/history" else "/api/reservations/my", server.takeRequest().path)
        }
        server.enqueue(MockResponse().setBody("""{"items":[$booking],"totalCount":21,"page":2,"pageSize":20}"""))
        val response = api.search(2, "RSV-1", "s1", "PENDING", "2030-01-01T00:00+05:30", "2030-01-02T00:00+05:30")
        val url = server.takeRequest().requestUrl!!
        assertEquals(setOf("page", "reservationCode", "stationId", "status", "from", "to", "pageSize"), url.queryParameterNames)
        assertEquals("2030-01-01T00:00+05:30", url.queryParameter("from"))
        assertEquals(21L, response.totalCount)
    }

    @Test fun stationDirectoryAndSlotsUseServerAvailability() = runBlocking {
        server.enqueue(MockResponse().setBody("""{"items":[],"totalCount":0,"page":1,"pageSize":20}"""))
        assertTrue(api.stations(1, "Colombo").items.isEmpty())
        assertEquals("ACTIVE", server.takeRequest().requestUrl!!.queryParameter("status"))
        server.enqueue(MockResponse().setBody("""{"items":[{"id":"t1","stationId":"s1","startTime":"2030-01-01T08:00:00Z","endTime":"2030-01-01T09:00:00Z","capacity":50,"availableCapacity":2.123456,"status":"OPEN"}]}"""))
        assertEquals(BigDecimal("2.123456"), api.slots("s1").items.single().availableCapacity)
        assertEquals("/api/stations/s1/slots", server.takeRequest().path)
    }

    @Test fun ruleAndSessionFailuresAreNotTreatedAsSuccessfulBookings() = runBlocking {
        for (code in listOf(400, 401, 403, 404, 409, 500, 503)) {
            server.enqueue(MockResponse().setResponseCode(code).setBody("""{"title":"Rejected"}"""))
            try { api.create(CreateBookingRequest("t1", BigDecimal.TEN)); fail("Expected HTTP $code") }
            catch (error: HttpException) { assertEquals(code, error.code()) }
            server.takeRequest()
        }
    }
}
