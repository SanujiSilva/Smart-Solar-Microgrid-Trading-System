package com.smartsolar.microgrid.booking

import android.content.Intent
import android.os.Bundle
import com.google.android.material.button.MaterialButton
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
            val slots = api.slots(id).items.filter { it.status == "OPEN" }
            if (slots.isEmpty()) binding.messageText.setText(R.string.no_open_slots)
            slots.forEach { slot ->
                binding.slots.addView(MaterialButton(this).apply {
                    text = getString(R.string.slot_list_row, BookingPresentation.time(slot.startTime),
                        BookingPresentation.time(slot.endTime), slot.availableCapacity.toPlainString())
                    setOnClickListener {
                        startActivity(Intent(this@StationDetailsActivity, BookingEditorActivity::class.java)
                            .putExtra(BookingEditorModel.SLOT_ID, slot.id))
                    }
                })
            }
        }
    }

    private fun minute(value: Int) = String.format(Locale.ROOT, "%02d:%02d", value / 60, value % 60)
}
