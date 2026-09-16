package com.smartsolar.microgrid

import android.content.Intent
import android.os.Bundle
import android.view.View
import com.smartsolar.microgrid.databinding.ActivityMainBinding

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
    }

    override fun onResume() {
        super.onResume()
        if (::binding.isInitialized) refresh()
    }

    private fun refresh() {
        binding.profileButton.visibility = View.GONE
        binding.summaryText.text = ""
        binding.activityText.text = ""
        request(binding.progressBar, binding.messageText,
            listOf(binding.refreshButton, binding.profileButton, binding.logoutButton)) {
            val user = account.currentUser()
            binding.welcomeText.text = getString(R.string.welcome_user, user.fullName)
            binding.accountText.text = getString(R.string.account_status, user.role, user.status)
            if (user.role != "PROSUMER") {
                binding.summaryText.setText(R.string.staff_home)
                return@request
            }
            binding.profileButton.visibility = View.VISIBLE
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
