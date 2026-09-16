package com.smartsolar.microgrid

import android.os.Bundle
import com.google.android.material.button.MaterialButton
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.smartsolar.microgrid.databinding.ActivityNearbyStationsBinding

class NearbyStationsActivity : AccountActivity() {
    private lateinit var binding: ActivityNearbyStationsBinding
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (!protectSession()) return
        binding = ActivityNearbyStationsBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.root.applyAccountInsets()
        binding.backButton.setOnClickListener { finish() }
        binding.searchButton.setOnClickListener { search() }
    }

    private fun search() {
        val latitude = binding.latitudeInput.text.toString().toDoubleOrNull()
        val longitude = binding.longitudeInput.text.toString().toDoubleOrNull()
        val radius = binding.radiusInput.text.toString().toDoubleOrNull()
        if (latitude == null || longitude == null || radius == null ||
            !latitude.isFinite() || !longitude.isFinite() || !radius.isFinite()) {
            binding.messageText.setText(R.string.coordinates_required)
            return
        }
        binding.results.removeAllViews()
        request(binding.progressBar, binding.messageText, listOf(binding.searchButton)) {
            val response = account.nearby(latitude, longitude, radius)
            if (response.items.isEmpty()) binding.messageText.setText(R.string.no_stations)
            response.items.forEach { station ->
                binding.results.addView(MaterialButton(this).apply {
                    text = station.name
                    setOnClickListener {
                        MaterialAlertDialogBuilder(this@NearbyStationsActivity)
                            .setTitle(station.name)
                            .setMessage(getString(R.string.station_details, station.stationCode, station.address,
                                station.status, station.capacityKWh.toString(), station.availableBatterySlots,
                                station.latitude.toString(), station.longitude.toString()))
                            .setPositiveButton(android.R.string.ok, null).show()
                    }
                })
            }
        }
    }
}
