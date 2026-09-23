package com.smartsolar.microgrid.booking

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.smartsolar.microgrid.data.remote.*
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import retrofit2.HttpException

enum class BookingNotice { CREATED, UPDATED, CANCELLED }
data class BookingEditorState(
    val busy: Boolean = false,
    val booking: Booking? = null,
    val station: BookingStation? = null,
    val slot: BookingSlot? = null,
    val draft: String = "",
    val error: Exception? = null,
    val uncertain: Boolean = false,
    val notice: BookingNotice? = null,
)

class BookingEditorModel(private val api: BookingApiService, private val saved: SavedStateHandle) : ViewModel() {
    private val mutable = MutableStateFlow(BookingEditorState(
        draft = saved["energy_draft"] ?: "", uncertain = saved["mutation_pending"] ?: false))
    val state = mutable.asStateFlow()

    init { refresh() }

    fun draft(value: String) {
        saved["energy_draft"] = value
        mutable.value = mutable.value.copy(draft = value)
    }

    fun selectSlot(slotId: String) {
        if (mutable.value.busy || mutable.value.uncertain) return
        mutable.value = mutable.value.copy(busy = true, error = null, notice = null)
        viewModelScope.launch {
            try {
                val slot = api.slot(slotId)
                val station = api.station(slot.stationId)
                saved["reschedule_slot_id"] = slotId
                mutable.value = mutable.value.copy(slot = slot, station = station)
            } catch (error: CancellationException) { throw error
            } catch (error: Exception) { mutable.value = mutable.value.copy(error = error)
            } finally { mutable.value = mutable.value.copy(busy = false) }
        }
    }

    fun refresh() {
        if (mutable.value.busy) return
        mutable.value = mutable.value.copy(busy = true, error = null, notice = null)
        viewModelScope.launch {
            try {
                val bookingId: String? = saved[BOOKING_ID]
                val booking = bookingId?.let { api.booking(it) }
                if (booking != null) {
                    if (mutable.value.uncertain) saved.remove<String>("reschedule_slot_id")
                    saved["mutation_pending"] = false
                    mutable.value = mutable.value.copy(booking = booking, uncertain = false)
                    draft(booking.energyAmount.toPlainString())
                }
                val slotId = saved.get<String>("reschedule_slot_id") ?: booking?.slotId ?: saved.get<String>(SLOT_ID)
                    ?: throw IllegalArgumentException("A slot or booking is required.")
                val slot = api.slot(slotId)
                val station = api.station(slot.stationId)
                mutable.value = mutable.value.copy(slot = slot, station = station)
            } catch (error: CancellationException) { throw error
            } catch (error: Exception) { mutable.value = mutable.value.copy(error = error)
            } finally { mutable.value = mutable.value.copy(busy = false) }
        }
    }

    fun submit() {
        val amount = BookingPresentation.energy(mutable.value.draft) ?: return
        val slot = mutable.value.slot ?: return
        val booking = mutable.value.booking
        mutate(if (booking == null) BookingNotice.CREATED else BookingNotice.UPDATED) {
            if (booking == null) api.create(CreateBookingRequest(slot.id, amount))
            else api.update(booking.id, UpdateBookingRequest(amount, slot.id))
        }
    }

    fun cancel() {
        val booking = mutable.value.booking ?: return
        mutate(BookingNotice.CANCELLED) { api.cancel(booking.id) }
    }

    private fun mutate(notice: BookingNotice, action: suspend () -> Booking) {
        if (mutable.value.busy || mutable.value.uncertain) return
        saved["mutation_pending"] = true
        mutable.value = mutable.value.copy(busy = true, error = null, notice = null)
        viewModelScope.launch {
            try {
                val booking = action()
                saved[BOOKING_ID] = booking.id
                saved.remove<String>("reschedule_slot_id")
                saved["mutation_pending"] = false
                draft(booking.energyAmount.toPlainString())
                mutable.value = mutable.value.copy(booking = booking, uncertain = false, notice = notice)
            } catch (error: CancellationException) { throw error
            } catch (error: Exception) {
                // Never retry a write automatically: a lost response may follow a committed operation.
                val uncertain = error !is HttpException || error.code() >= 500 || error.code() == 408
                saved["mutation_pending"] = uncertain
                mutable.value = mutable.value.copy(error = error, uncertain = uncertain)
            } finally { mutable.value = mutable.value.copy(busy = false) }
        }
    }

    companion object {
        const val BOOKING_ID = "booking_id"
        const val SLOT_ID = "slot_id"
    }
}
