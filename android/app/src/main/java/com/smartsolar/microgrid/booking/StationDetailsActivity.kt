package com.smartsolar.microgrid.booking

import android.content.Intent
import android.os.Bundle
import com.google.android.material.button.MaterialButton
import com.smartsolar.microgrid.SolarRecord
import com.smartsolar.microgrid.bindSolarRecord
import com.smartsolar.microgrid.databinding.ItemSolarRecordBinding
import com.smartsolar.microgrid.AccountActivity
import com.smartsolar.microgrid.R
import com.smartsolar.microgrid.applyAccountInsets
import com.smartsolar.microgrid.data.remote.ApiClient
import com.smartsolar.microgrid.databinding.ActivityStationDetailsBinding
import java.util.Locale

class StationDetailsActivity : AccountActivity() {
    private lateinit var binding: ActivityStationDetailsBinding
    private val api by lazy { ApiClient.bookingService(applicationContext) }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (!protectSession()) return
        binding = ActivityStationDetailsBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.root.applyAccountInsets()
        binding.refreshButton.setOnClickListener { load() }
        binding.backButton.setOnClickListener { finish() }
    }

    override fun onResume() {
        super.onResume()
        if (::binding.isInitialized) load()
    }

    private fun load() {
        val id = intent.getStringExtra(StationDirectoryActivity.STATION_ID)
        if (id.isNullOrBlank()) { binding.messageText.setText(R.string.station_not_found); return }
        binding.slots.removeAllViews()
        request(binding.progressBar, binding.messageText, listOf(binding.refreshButton)) {
            val station = api.station(id)
            binding.detailsText.text = getString(R.string.station_details, station.stationCode, station.address,
                station.status, station.capacityKWh.toPlainString(), station.availableBatterySlots,
                station.latitude.toString(), station.longitude.toString())
            binding.titleText.text = station.name
            binding.scheduleText.text = getString(R.string.schedule_zone, station.operatingSchedule.timeZoneId,
                station.operatingSchedule.weeklyPeriods.joinToString("\n") {
                    getString(R.string.schedule_period, it.day, minute(it.openMinuteOfDay), minute(it.closeMinuteOfDay))
                })
            val slots = api.slots(id).items
            val available = slots.filter { it.status == "OPEN" && it.availableCapacity.signum() > 0 }
            binding.slotSummaryText.text = getString(R.string.slot_capacity_summary, available.fold(java.math.BigDecimal.ZERO) { total, slot -> total + slot.availableCapacity }.toPlainString(), available.size)
            if (available.isEmpty()) binding.messageText.setText(R.string.no_open_slots)
            slots.forEach { slot ->
                val canBook = station.status == "ACTIVE" && slot.status == "OPEN" && slot.availableCapacity.signum() > 0
                val row = ItemSolarRecordBinding.inflate(layoutInflater, binding.slots, false)
                row.bindSolarRecord(SolarRecord(slot.id, getString(R.string.available_slots),
                    getString(R.string.slot_card_body, BookingPresentation.time(slot.startTime), BookingPresentation.time(slot.endTime), slot.availableCapacity.toPlainString()),
                    slot.status, getString(if (canBook) R.string.book_slot_action else R.string.slot_unavailable), canBook) {
                    if (intent.getBooleanExtra(PICK_SLOT, false)) {
                        setResult(RESULT_OK, Intent().putExtra(BookingEditorModel.SLOT_ID, slot.id))
                        finish()
                    } else startActivity(Intent(this, BookingEditorActivity::class.java).putExtra(BookingEditorModel.SLOT_ID, slot.id))
                })
                binding.slots.addView(row.root)
            }
        }
    }

    private fun minute(value: Int) = String.format(Locale.ROOT, "%02d:%02d", value / 60, value % 60)

    companion object { const val PICK_SLOT = "pick_slot" }
}
