package com.smartsolar.microgrid.booking

import androidx.lifecycle.SavedStateHandle
import com.smartsolar.microgrid.data.remote.*
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.*
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.ResponseBody.Companion.toResponseBody
import org.junit.After
import org.junit.Assert.*
import org.junit.Before
import org.junit.Test
import retrofit2.HttpException
import retrofit2.Response
import java.io.IOException
import java.math.BigDecimal

@OptIn(ExperimentalCoroutinesApi::class)
class BookingEditorModelTest {
    private val dispatcher = StandardTestDispatcher()
    @Before fun setup() { Dispatchers.setMain(dispatcher) }
    @After fun cleanup() { Dispatchers.resetMain() }

    @Test fun repeatedTapsSendOneCreateAndRetainReturnedSummary() = runTest(dispatcher) {
        val api = FakeBookingApi()
        api.waitForCreate = CompletableDeferred()
        val saved = SavedStateHandle(mapOf(BookingEditorModel.SLOT_ID to "slot"))
        val model = BookingEditorModel(api, saved)
        advanceUntilIdle()
        model.draft("2.5")
        model.submit(); model.submit()
        runCurrent()
        assertTrue(model.state.value.busy)
        assertEquals(1, api.creates)
        api.waitForCreate!!.complete(Unit)
        advanceUntilIdle()
        assertEquals("reservation", model.state.value.booking?.id)
        assertEquals(BookingNotice.CREATED, model.state.value.notice)
        assertEquals("reservation", saved.get<String>(BookingEditorModel.BOOKING_ID))
        assertFalse(saved.get<Boolean>("mutation_pending")!!)
    }

    @Test fun lostCreateResponseBlocksResubmissionEvenAfterStateRestoration() = runTest(dispatcher) {
        val api = FakeBookingApi().apply { writeFailure = IOException("Connection lost") }
        val saved = SavedStateHandle(mapOf(BookingEditorModel.SLOT_ID to "slot"))
        val model = BookingEditorModel(api, saved)
        advanceUntilIdle(); model.draft("2"); model.submit(); advanceUntilIdle()
        assertTrue(model.state.value.uncertain)
        model.submit(); advanceUntilIdle()
        assertEquals(1, api.creates)
        val restored = BookingEditorModel(api, saved)
        advanceUntilIdle(); restored.submit(); advanceUntilIdle()
        assertTrue(restored.state.value.uncertain)
        assertEquals(1, api.creates)
    }

    @Test fun serverRuleRejectionKeepsDraftWithoutInventingApproval() = runTest(dispatcher) {
        val api = FakeBookingApi().apply { writeFailure = HttpException(Response.error<Any>(409,
            """{"title":"Not enough capacity"}""".toResponseBody("application/problem+json".toMediaType()))) }
        val model = BookingEditorModel(api, SavedStateHandle(mapOf(BookingEditorModel.SLOT_ID to "slot")))
        advanceUntilIdle(); model.draft("250"); model.submit(); advanceUntilIdle()
        assertNull(model.state.value.booking)
        assertEquals("250", model.state.value.draft)
        assertFalse(model.state.value.uncertain)
        assertEquals(409, (model.state.value.error as HttpException).code())
    }

    @Test fun lostUpdateResponseRequiresReloadBeforeAnotherMutation() = runTest(dispatcher) {
        val api = FakeBookingApi().apply { writeFailure = IOException("Connection lost") }
        val model = BookingEditorModel(api, SavedStateHandle(mapOf(BookingEditorModel.BOOKING_ID to "reservation")))
        advanceUntilIdle(); model.draft("3"); model.submit(); advanceUntilIdle()
        assertTrue(model.state.value.uncertain)
        model.cancel(); advanceUntilIdle(); assertEquals(0, api.cancels)
        api.writeFailure = null
        model.refresh(); advanceUntilIdle(); assertFalse(model.state.value.uncertain)
        model.cancel(); advanceUntilIdle()
        assertEquals(1, api.cancels)
        assertEquals("CANCELLED", model.state.value.booking?.status)
    }

    @Test fun invalidInputDoesNotSendRequestAndValidInputDoesNotApplyClientTimeRules() = runTest(dispatcher) {
        val api = FakeBookingApi()
        val model = BookingEditorModel(api, SavedStateHandle(mapOf(BookingEditorModel.SLOT_ID to "slot")))
        advanceUntilIdle(); model.draft("-2"); model.submit(); advanceUntilIdle()
        assertEquals(0, api.creates)
        // The fake slot is in the past; the client still delegates eligibility to the server.
        model.draft("2"); model.submit(); advanceUntilIdle(); assertEquals(1, api.creates)
    }

    private class FakeBookingApi : BookingApiService {
        var creates = 0
        var cancels = 0
        var waitForCreate: CompletableDeferred<Unit>? = null
        var writeFailure: Exception? = null
        private val booking = Booking("reservation", "RSV-1", "200012345678", "station", "slot",
            BigDecimal.TEN, "2020-01-01T08:00:00Z", "PENDING", "2020-01-01T00:00:00Z",
            "2020-01-01T00:00:00Z", null, null)
        override suspend fun station(id: String) = BookingStation(id, "ST-1", "Station", "Address", 6.9, 79.8,
            BigDecimal.TEN, 1, "ACTIVE", BookingSchedule("UTC", emptyList()))
        override suspend fun slot(id: String) = BookingSlot(id, "station", "2020-01-01T08:00:00Z",
            "2020-01-01T09:00:00Z", BigDecimal.TEN, BigDecimal.TEN, "OPEN")
        override suspend fun booking(id: String) = booking
        override suspend fun create(request: CreateBookingRequest): Booking {
            creates++; waitForCreate?.await(); writeFailure?.let { throw it }
            return booking.copy(energyAmount = request.energyAmount)
        }
        override suspend fun update(id: String, request: UpdateBookingRequest): Booking {
            writeFailure?.let { throw it }; return booking.copy(energyAmount = request.energyAmount)
        }
        override suspend fun cancel(id: String): Booking {
            cancels++; writeFailure?.let { throw it }; return booking.copy(status = "CANCELLED")
        }
        override suspend fun stations(page: Int, search: String?, pageSize: Int, status: String?) = error("Unused")
        override suspend fun slots(stationId: String) = error("Unused")
        override suspend fun current() = error("Unused")
        override suspend fun history() = error("Unused")
        override suspend fun search(page: Int, code: String?, stationId: String?, status: String?, from: String?, to: String?, pageSize: Int) = error("Unused")
    }
}
