package com.smartsolar.microgrid.booking

import android.app.DatePickerDialog
import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import androidx.activity.result.contract.ActivityResultContracts
import com.google.android.material.button.MaterialButton
import com.smartsolar.microgrid.AccountActivity
import com.smartsolar.microgrid.R
import com.smartsolar.microgrid.applyAccountInsets
import com.smartsolar.microgrid.data.remote.ApiClient
import com.smartsolar.microgrid.databinding.ActivityBookingsBinding
import java.time.LocalDate

class BookingsActivity : AccountActivity() {
    private lateinit var binding: ActivityBookingsBinding
    private val api by lazy { ApiClient.bookingService(applicationContext) }
    private var page = 1
    private var mode = 0
    private var stationId: String? = null
    private var stationName: String? = null
    private var from = ""
    private var through = ""
    private val statuses = listOf(null, "PENDING", "APPROVED", "CANCELLED", "COMPLETED", "REJECTED")
    private val stationPicker = registerForActivityResult(ActivityResultContracts.StartActivityForResult()) { result ->
        if (result.resultCode == RESULT_OK) {
            stationId = result.data?.getStringExtra(StationDirectoryActivity.STATION_ID)
            stationName = result.data?.getStringExtra(StationDirectoryActivity.STATION_NAME)
            binding.stationButton.text = stationName
            page = 1
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (!protectSession()) return
        binding = ActivityBookingsBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.root.applyAccountInsets()
        page = savedInstanceState?.getInt("page") ?: 1
        mode = savedInstanceState?.getInt("mode") ?: 0
        stationId = savedInstanceState?.getString("station_id")
        stationName = savedInstanceState?.getString("station_name")
        from = savedInstanceState?.getString("from").orEmpty()
        through = savedInstanceState?.getString("through").orEmpty()
        binding.codeInput.setText(savedInstanceState?.getString("code").orEmpty())
        binding.stationButton.text = stationName ?: getString(R.string.filter_station)
        binding.modeSpinner.adapter = ArrayAdapter.createFromResource(this, R.array.booking_views, android.R.layout.simple_spinner_dropdown_item)
        binding.statusSpinner.adapter = ArrayAdapter.createFromResource(this, R.array.booking_statuses, android.R.layout.simple_spinner_dropdown_item)
        binding.statusSpinner.setSelection(savedInstanceState?.getInt("status") ?: 0)
        binding.modeSpinner.setSelection(mode)
        binding.filters.visibility = if (mode == 3) View.VISIBLE else View.GONE
        binding.modeSpinner.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onNothingSelected(parent: AdapterView<*>?) = Unit
            override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
                if (mode != position) { mode = position; page = 1; binding.filters.visibility = if (mode == 3) View.VISIBLE else View.GONE; load() }
            }
        }
        binding.fromButton.setOnClickListener { pickDate(true) }
        binding.throughButton.setOnClickListener { pickDate(false) }
        binding.stationButton.setOnClickListener {
            startStationPicker()
        }
        binding.clearButton.setOnClickListener {
            from = ""; through = ""; stationId = null; stationName = null
            binding.codeInput.text?.clear()
            binding.statusSpinner.setSelection(0)
            binding.stationButton.setText(R.string.filter_station)
            renderDates()
            page = 1
            load()
        }
        binding.refreshButton.setOnClickListener { page = 1; load() }
        binding.previousButton.setOnClickListener { if (page > 1) { page--; load() } }
        binding.nextButton.setOnClickListener { page++; load() }
        binding.newButton.setOnClickListener { startActivity(Intent(this, StationDirectoryActivity::class.java)) }
        binding.backButton.setOnClickListener { finish() }
        renderDates()
    }

    private fun startStationPicker() {
        stationPicker.launch(Intent(this, StationDirectoryActivity::class.java).putExtra(StationDirectoryActivity.PICK_STATION, true))
    }

    override fun onResume() {
        super.onResume()
        if (::binding.isInitialized) load()
    }

    override fun onSaveInstanceState(outState: Bundle) {
        if (::binding.isInitialized) {
            outState.putInt("page", page); outState.putInt("mode", mode)
            outState.putString("station_id", stationId); outState.putString("station_name", stationName)
            outState.putString("from", from); outState.putString("through", through)
            outState.putString("code", binding.codeInput.text.toString())
            outState.putInt("status", binding.statusSpinner.selectedItemPosition)
        }
        super.onSaveInstanceState(outState)
    }

    private fun pickDate(isFrom: Boolean) {
        val initial = (if (isFrom) from else through).takeIf { it.isNotBlank() }?.let { LocalDate.parse(it) } ?: LocalDate.now()
        DatePickerDialog(this, { _, year, month, day ->
            val date = LocalDate.of(year, month + 1, day).toString()
            if (isFrom) from = date else through = date
            renderDates()
        }, initial.year, initial.monthValue - 1, initial.dayOfMonth).show()
    }

    private fun renderDates() {
        binding.fromButton.text = getString(R.string.from_date, from.ifBlank { getString(R.string.any_date) })
        binding.throughButton.text = getString(R.string.through_date, through.ifBlank { getString(R.string.any_date) })
    }

    private fun load() {
        if (mode == 3 && from.isNotEmpty() && through.isNotEmpty() && LocalDate.parse(from) > LocalDate.parse(through)) {
            binding.messageText.setText(R.string.invalid_date_range)
            return
        }
        binding.results.removeAllViews()
        request(binding.progressBar, binding.messageText,
            listOf(binding.refreshButton, binding.modeSpinner, binding.statusSpinner, binding.codeInput,
                binding.stationButton, binding.fromButton, binding.throughButton, binding.clearButton,
                binding.previousButton, binding.nextButton)) {
            val items: List<com.smartsolar.microgrid.data.remote.Booking>
            val total: Long
            if (mode == 0 || mode == 2) {
                val all = if (mode == 2) api.history().items
                    else api.current().items.filter { it.status == "PENDING" || it.status == "APPROVED" }
                total = all.size.toLong()
                page = page.coerceAtMost(((total + 19) / 20).toInt().coerceAtLeast(1))
                items = all.drop((page - 1) * 20).take(20)
            } else {
                val result = api.search(page,
                    if (mode == 3) binding.codeInput.text.toString().trim().ifBlank { null } else null,
                    if (mode == 3) stationId else null,
                    if (mode == 1) "PENDING" else statuses[binding.statusSpinner.selectedItemPosition],
                    if (mode == 3) BookingPresentation.fromDate(from) else null,
                    if (mode == 3) BookingPresentation.throughDate(through) else null)
                total = result.totalCount; items = result.items; page = result.page
            }
            binding.pageText.text = getString(R.string.page_records, page, total)
            binding.previousButton.visibility = if (page > 1) View.VISIBLE else View.GONE
            binding.nextButton.visibility = if (page * 20L < total) View.VISIBLE else View.GONE
            if (items.isEmpty()) binding.messageText.setText(R.string.no_bookings)
            items.forEach { booking ->
                binding.results.addView(MaterialButton(this).apply {
                    text = getString(R.string.booking_list_row, booking.reservationCode, booking.status,
                        BookingPresentation.time(booking.reservationDateTime), booking.energyAmount.toPlainString())
                    setOnClickListener {
                        startActivity(Intent(this@BookingsActivity, BookingEditorActivity::class.java)
                            .putExtra(BookingEditorModel.BOOKING_ID, booking.id))
                    }
                })
            }
        }
    }
}
