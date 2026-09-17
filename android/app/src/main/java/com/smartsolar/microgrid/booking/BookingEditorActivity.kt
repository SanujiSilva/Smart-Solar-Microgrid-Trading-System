package com.smartsolar.microgrid.booking

import android.content.Intent
import android.os.Bundle
import android.view.View
import androidx.activity.viewModels
import androidx.core.widget.doAfterTextChanged
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.createSavedStateHandle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.smartsolar.microgrid.AccountActivity
import com.smartsolar.microgrid.R
import com.smartsolar.microgrid.accountError
import com.smartsolar.microgrid.applyAccountInsets
import com.smartsolar.microgrid.data.remote.ApiClient
import com.smartsolar.microgrid.databinding.ActivityBookingEditorBinding
import com.smartsolar.microgrid.qr.ProsumerQrActivity
import kotlinx.coroutines.launch
import retrofit2.HttpException

class BookingEditorActivity : AccountActivity() {
    private lateinit var binding: ActivityBookingEditorBinding
    private val model: BookingEditorModel by viewModels {
        viewModelFactory { initializer { BookingEditorModel(ApiClient.bookingService(applicationContext), createSavedStateHandle()) } }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (!protectSession()) return
        binding = ActivityBookingEditorBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.root.applyAccountInsets()
        binding.amountInput.doAfterTextChanged {
            if (model.state.value.draft != it.toString()) model.draft(it.toString())
        }
        binding.refreshButton.setOnClickListener { model.refresh() }
        binding.backButton.setOnClickListener { finish() }
        binding.bookingsButton.setOnClickListener {
            startActivity(Intent(this, BookingsActivity::class.java))
            finish()
        }
        binding.qrButton.setOnClickListener {
            model.state.value.booking?.let { booking ->
                startActivity(Intent(this, ProsumerQrActivity::class.java)
                    .putExtra(ProsumerQrActivity.RESERVATION_ID, booking.id))
            }
        }
        binding.saveButton.setOnClickListener {
            val amount = BookingPresentation.energy(binding.amountInput.text.toString())
            if (amount == null) {
                binding.messageText.setText(R.string.valid_energy_required)
                return@setOnClickListener
            }
            MaterialAlertDialogBuilder(this)
                .setTitle(if (model.state.value.booking == null) R.string.confirm_booking else R.string.confirm_modification)
                .setMessage(getString(R.string.confirm_booking_body, binding.summaryText.text, amount.toPlainString()))
                .setNegativeButton(android.R.string.cancel, null)
                .setPositiveButton(R.string.confirm_action) { _, _ -> model.submit() }.show()
        }
        binding.cancelButton.setOnClickListener {
            MaterialAlertDialogBuilder(this).setTitle(R.string.cancel_booking)
                .setMessage(R.string.confirm_cancel_booking)
                .setNegativeButton(R.string.keep_booking, null)
                .setPositiveButton(R.string.cancel_booking) { _, _ -> model.cancel() }.show()
        }
        lifecycleScope.launch {
            repeatOnLifecycle(Lifecycle.State.STARTED) { model.state.collect { render(it) } }
        }
    }

    private fun render(state: BookingEditorState) {
        if (state.error is HttpException && state.error.code() == 401) { signOut(); return }
        binding.progressBar.visibility = if (state.busy) View.VISIBLE else View.GONE
        binding.refreshButton.isEnabled = !state.busy
        binding.amountInput.isEnabled = !state.busy && !state.uncertain
        binding.backButton.isEnabled = !state.busy
        binding.bookingsButton.isEnabled = !state.busy
        if (binding.amountInput.text.toString() != state.draft) binding.amountInput.setText(state.draft)
        val booking = state.booking
        binding.titleText.setText(if (booking == null) R.string.create_booking else R.string.booking_details)
        val slot = state.slot
        val station = state.station
        binding.summaryText.text = when {
            booking != null -> getString(R.string.booking_detail_summary, booking.reservationCode, booking.status,
                station?.name ?: booking.stationId, BookingPresentation.time(booking.reservationDateTime),
                booking.energyAmount.toPlainString(), booking.prosumerNIC,
                BookingPresentation.time(booking.createdAt), BookingPresentation.time(booking.updatedAt),
                booking.completedAt?.let { BookingPresentation.time(it) } ?: getString(R.string.not_completed))
            slot != null && station != null -> getString(R.string.booking_slot_summary, station.name, station.address,
                BookingPresentation.time(slot.startTime), BookingPresentation.time(slot.endTime),
                slot.availableCapacity.toPlainString(), slot.status)
            else -> ""
        }
        // Status-based presentation only; all eligibility and notice decisions remain with the API.
        val editableStatus = booking == null || booking.status in listOf("PENDING", "APPROVED")
        binding.saveButton.visibility = if (editableStatus) View.VISIBLE else View.GONE
        binding.amountLayout.visibility = if (editableStatus) View.VISIBLE else View.GONE
        binding.saveButton.setText(if (booking == null) R.string.review_booking else R.string.modify_booking)
        binding.saveButton.isEnabled = !state.busy && !state.uncertain && slot != null && station != null
        binding.cancelButton.visibility = if (booking != null && editableStatus) View.VISIBLE else View.GONE
        binding.cancelButton.isEnabled = !state.busy && !state.uncertain
        binding.qrButton.visibility = if (booking?.status == "APPROVED") View.VISIBLE else View.GONE
        binding.qrButton.isEnabled = !state.busy && !state.uncertain
        binding.messageText.text = when {
            state.uncertain && booking == null -> getString(R.string.create_outcome_unknown)
            state.uncertain -> getString(R.string.change_outcome_unknown)
            state.error is HttpException && state.error.code() == 404 -> getString(R.string.booking_not_found)
            state.error != null -> accountError(state.error)
            state.notice == BookingNotice.CREATED -> getString(R.string.booking_created)
            state.notice == BookingNotice.UPDATED -> getString(R.string.booking_updated)
            state.notice == BookingNotice.CANCELLED -> getString(R.string.booking_cancelled)
            else -> ""
        }
    }
}
