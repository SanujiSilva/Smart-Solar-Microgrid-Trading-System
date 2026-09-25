package com.smartsolar.microgrid

import android.content.Intent
import android.os.Bundle
import android.view.View
import androidx.lifecycle.lifecycleScope
import kotlinx.coroutines.launch
import com.smartsolar.microgrid.databinding.ActivityMainBinding
import com.smartsolar.microgrid.databinding.ActivityProsumerHomeBinding
import com.smartsolar.microgrid.databinding.ItemHomeActivityBinding
import com.smartsolar.microgrid.qr.OperatorQrActivity
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.time.format.FormatStyle

class MainActivity : AccountActivity() {
    private lateinit var binding: ActivityMainBinding
    private var prosumerBinding: ActivityProsumerHomeBinding? = null
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (!protectSession()) return
        lifecycleScope.launch {
            val layout = if (account.cachedUser()?.role == "PROSUMER") {
                R.layout.activity_prosumer_home
            } else {
                R.layout.activity_main
            }
            initializeHome(layout)
            refresh()
        }
    }

    private fun initializeHome(layout: Int) {
        val root = layoutInflater.inflate(layout, null, false)
        binding = ActivityMainBinding.bind(root)
        prosumerBinding = if (layout == R.layout.activity_prosumer_home) {
            ActivityProsumerHomeBinding.bind(root)
        } else {
            null
        }
        setContentView(root)
        binding.root.applyAccountInsets()
        configurePrimaryNavigation(R.id.nav_home)
        binding.solarToolbar.menu.clear()
        binding.solarToolbar.menu.add(R.string.sign_out).apply {
            setShowAsAction(android.view.MenuItem.SHOW_AS_ACTION_ALWAYS)
            setOnMenuItemClickListener {
                if (!requestBusy) confirmSignOut()
                true
            }
        }
        binding.refreshButton.setOnClickListener { refresh() }
        binding.profileButton.setOnClickListener { startActivity(Intent(this, ProfileActivity::class.java)) }
        binding.logoutButton.setOnClickListener { confirmSignOut() }
        binding.stationsButton.setOnClickListener { startActivity(Intent(this, NearbyStationsActivity::class.java)) }
        binding.bookingsButton.setOnClickListener { startActivity(Intent(this, com.smartsolar.microgrid.booking.BookingsActivity::class.java)) }
        binding.newBookingButton.setOnClickListener { startActivity(Intent(this, com.smartsolar.microgrid.booking.StationDirectoryActivity::class.java)) }
        binding.operatorQrButton.setOnClickListener { startActivity(Intent(this, OperatorQrActivity::class.java)) }
    }

    override fun onResume() {
        super.onResume()
        if (::binding.isInitialized) refresh()
    }

    private fun refresh() {
        binding.profileButton.visibility = View.GONE
        binding.bookingsButton.visibility = View.GONE
        binding.newBookingButton.visibility = View.GONE
        binding.operatorQrButton.visibility = View.GONE
        binding.summaryText.text = ""
        binding.metricsPanel.visibility = View.GONE
        binding.activityText.text = ""
        prosumerBinding?.apply {
            activeStationsText.text = ""
            openSlotsText.text = ""
            todayReservationsText.text = ""
            activeBookingsText.text = ""
            recentActivityList.removeAllViews()
        }
        request(binding.progressBar, binding.messageText,
            listOf(binding.refreshButton, binding.profileButton, binding.logoutButton)) {
            val user = account.currentUser()
            configurePrimaryNavigation(R.id.nav_home)
            binding.welcomeText.text = getString(R.string.welcome_user, user.fullName)
            binding.accountText.text = getString(R.string.account_status, user.role, user.status)
            if (user.role == "GRID_OPERATOR") {
                binding.operatorQrButton.visibility = View.VISIBLE
                binding.summaryText.setText(R.string.operator_home)
                return@request
            }
            if (user.role != "PROSUMER") {
                binding.summaryText.setText(R.string.staff_home)
                return@request
            }
            binding.profileButton.visibility = View.VISIBLE
            binding.bookingsButton.visibility = View.VISIBLE
            binding.newBookingButton.visibility = View.VISIBLE
            val dashboard = account.dashboard()
            binding.energyMetric.text = getString(R.string.metric_energy, dashboard.availableSlotCapacity.toPlainString())
            binding.pendingMetric.text = getString(R.string.metric_pending, dashboard.pendingReservations)
            binding.approvedMetric.text = getString(R.string.metric_approved, dashboard.approvedFutureReservations)
            binding.completedMetric.text = getString(R.string.metric_completed, dashboard.completedTransfers)
            binding.metricsPanel.visibility = View.VISIBLE
            binding.summaryText.text = getString(R.string.dashboard_availability,
                dashboard.activeStations, dashboard.openSlots, dashboard.todayReservations)
            val recent = dashboard.recentReservations.takeIf { it.isNotEmpty() }?.joinToString("\n\n") {
                getString(R.string.recent_booking, it.reservationCode, it.status,
                    OffsetDateTime.parse(it.reservationDateTime).atZoneSameInstant(ZoneId.systemDefault())
                        .format(DateTimeFormatter.ofLocalizedDateTime(FormatStyle.MEDIUM)),
                    it.energyAmount.toPlainString())
            } ?: getString(R.string.no_recent_bookings)
            binding.activityText.text = getString(R.string.home_activity_summary,
                getString(R.string.active_bookings, dashboard.activeReservations), recent)
            prosumerBinding?.apply {
                activeStationsText.text = dashboard.activeStations.toString()
                openSlotsText.text = dashboard.openSlots.toString()
                todayReservationsText.text = dashboard.todayReservations.toString()
                activeBookingsText.text = getString(R.string.recent_active_label, dashboard.activeReservations)
                recentActivityList.removeAllViews()
                if (dashboard.recentReservations.isEmpty()) {
                    val emptyState = layoutInflater.inflate(
                        R.layout.item_home_activity,
                        recentActivityList,
                        false,
                    )
                    ItemHomeActivityBinding.bind(emptyState).apply {
                        reservationCodeText.setText(R.string.no_recent_bookings)
                        statusChip.visibility = View.GONE
                        reservationTimeText.text = ""
                        energyText.visibility = View.GONE
                    }
                    recentActivityList.addView(emptyState)
                } else {
                    dashboard.recentReservations.forEach { reservation ->
                        val item = layoutInflater.inflate(
                            R.layout.item_home_activity,
                            recentActivityList,
                            false,
                        )
                        ItemHomeActivityBinding.bind(item).apply {
                            reservationCodeText.text = reservation.reservationCode
                            statusChip.solarStatus(reservation.status)
                            reservationTimeText.text = OffsetDateTime.parse(reservation.reservationDateTime)
                                .atZoneSameInstant(ZoneId.systemDefault())
                                .format(DateTimeFormatter.ofLocalizedDateTime(FormatStyle.MEDIUM))
                            energyText.text = getString(
                                R.string.recent_energy_label,
                                reservation.energyAmount.toPlainString(),
                            )
                        }
                        recentActivityList.addView(item)
                    }
                }
            }
        }
    }
}
