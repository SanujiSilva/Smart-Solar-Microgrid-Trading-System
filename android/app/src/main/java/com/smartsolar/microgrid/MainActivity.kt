package com.smartsolar.microgrid

import android.content.Intent
import android.os.Bundle
import android.view.View
import com.smartsolar.microgrid.databinding.ActivityMainBinding
import com.smartsolar.microgrid.qr.OperatorQrActivity

class MainActivity : AccountActivity() {
    private lateinit var binding: ActivityMainBinding
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (!protectSession()) return
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.root.applyAccountInsets()
        binding.refreshButton.setOnClickListener { refresh() }
        binding.profileButton.setOnClickListener { startActivity(Intent(this, ProfileActivity::class.java)) }
        binding.logoutButton.setOnClickListener { signOut() }
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
        binding.activityText.text = ""
        request(binding.progressBar, binding.messageText,
            listOf(binding.refreshButton, binding.profileButton, binding.logoutButton)) {
            val user = account.currentUser()
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
            binding.summaryText.text = getString(R.string.dashboard_summary,
                dashboard.pendingReservations, dashboard.approvedFutureReservations,
                dashboard.todayReservations, dashboard.completedTransfers, dashboard.activeStations,
                dashboard.openSlots, dashboard.availableSlotCapacity.toPlainString())
            binding.activityText.text = getString(R.string.active_bookings, dashboard.activeReservations) + "\n\n" +
                (dashboard.recentReservations.takeIf { it.isNotEmpty() }?.joinToString("\n\n") {
                    getString(R.string.recent_booking, it.reservationCode, it.status,
                        java.time.OffsetDateTime.parse(it.reservationDateTime).atZoneSameInstant(java.time.ZoneId.systemDefault())
                            .format(java.time.format.DateTimeFormatter.ofLocalizedDateTime(java.time.format.FormatStyle.MEDIUM)),
                        it.energyAmount.toPlainString())
                } ?: getString(R.string.no_recent_bookings))
        }
    }
}
