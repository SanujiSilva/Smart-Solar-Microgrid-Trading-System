package com.smartsolar.microgrid.booking

import android.content.Intent
import android.os.Bundle
import android.view.View
import com.google.android.material.button.MaterialButton
import com.smartsolar.microgrid.AccountActivity
import com.smartsolar.microgrid.R
import com.smartsolar.microgrid.applyAccountInsets
import com.smartsolar.microgrid.data.remote.ApiClient
import com.smartsolar.microgrid.databinding.ActivityStationDirectoryBinding

class StationDirectoryActivity : AccountActivity() {
    private lateinit var binding: ActivityStationDirectoryBinding
    private val api by lazy { ApiClient.bookingService(applicationContext) }
    private var page = 1

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (!protectSession()) return
        page = savedInstanceState?.getInt("page") ?: 1
        binding = ActivityStationDirectoryBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.root.applyAccountInsets()
        binding.searchInput.setText(savedInstanceState?.getString("search").orEmpty())
        binding.searchButton.setOnClickListener { page = 1; load() }
        binding.previousButton.setOnClickListener { if (page > 1) { page--; load() } }
        binding.nextButton.setOnClickListener { page++; load() }
        binding.backButton.setOnClickListener { finish() }
        load()
    }

    override fun onSaveInstanceState(outState: Bundle) {
        if (::binding.isInitialized) { outState.putInt("page", page); outState.putString("search", binding.searchInput.text.toString()) }
        super.onSaveInstanceState(outState)
    }

    private fun load() {
        binding.results.removeAllViews()
        request(binding.progressBar, binding.messageText,
            listOf(binding.searchButton, binding.searchInput, binding.previousButton, binding.nextButton)) {
            val result = api.stations(page, binding.searchInput.text.toString().trim().ifBlank { null },
                status = if (intent.getBooleanExtra(PICK_STATION, false)) null else "ACTIVE")
            page = result.page
            binding.pageText.text = getString(R.string.page_records, page, result.totalCount)
            binding.previousButton.visibility = if (page > 1) View.VISIBLE else View.GONE
            binding.nextButton.visibility = if (page.toLong() * result.pageSize < result.totalCount) View.VISIBLE else View.GONE
            if (result.items.isEmpty()) binding.messageText.setText(R.string.no_stations)
            result.items.forEach { station ->
                binding.results.addView(MaterialButton(this).apply {
                    text = getString(R.string.station_list_row, station.name, station.address, station.capacityKWh.toPlainString())
                    setOnClickListener {
                        if (intent.getBooleanExtra(PICK_STATION, false)) {
                            setResult(RESULT_OK, Intent().putExtra(STATION_ID, station.id).putExtra(STATION_NAME, station.name))
                            finish()
                        } else {
                            startActivity(Intent(this@StationDirectoryActivity, StationDetailsActivity::class.java).putExtra(STATION_ID, station.id))
                        }
                    }
                })
            }
        }
    }

    companion object {
        const val STATION_ID = "station_id"
        const val STATION_NAME = "station_name"
        const val PICK_STATION = "pick_station"
    }
}
