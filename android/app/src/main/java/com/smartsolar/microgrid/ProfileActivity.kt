package com.smartsolar.microgrid

import android.os.Bundle
import android.view.View
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.smartsolar.microgrid.data.local.LocalUser
import com.smartsolar.microgrid.data.remote.UpdateProfileRequest
import com.smartsolar.microgrid.databinding.ActivityProfileBinding

class ProfileActivity : AccountActivity() {
    private lateinit var binding: ActivityProfileBinding
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (!protectSession()) return
        binding = ActivityProfileBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.root.applyAccountInsets()
        configurePrimaryNavigation(R.id.nav_profile)
        binding.logoutButton.setOnClickListener { confirmSignOut() }
        binding.backButton.setOnClickListener { finish() }
        binding.retryButton.setOnClickListener { load() }
        binding.saveButton.setOnClickListener { save() }
        binding.deactivateButton.setOnClickListener {
            MaterialAlertDialogBuilder(this)
                .setTitle(R.string.deactivate_title)
                .setMessage(R.string.deactivate_message)
                .setNegativeButton(R.string.keep_account, null)
                .setPositiveButton(R.string.send_request) { _, _ ->
                    runProfileRequest {
                        render(account.requestDeactivation())
                        binding.messageText.setText(R.string.deactivation_pending)
                    }
                }.show()
        }
        load()
    }

    private fun load() = runProfileRequest { render(account.currentUser()) }

    private fun save() {
        val values = listOf(binding.fullNameInput, binding.emailInput, binding.phoneInput)
            .map { it.text.toString().trim() }
        if (values.any { it.isBlank() }) {
            binding.messageText.setText(R.string.contact_required)
            return
        }
        runProfileRequest {
            render(account.updateProfile(UpdateProfileRequest(values[0], values[1], values[2])))
            binding.messageText.setText(R.string.profile_saved)
        }
    }

    private fun runProfileRequest(action: suspend () -> Unit) = request(
        binding.progressBar, binding.messageText,
        listOf(binding.saveButton, binding.deactivateButton, binding.retryButton,
            binding.fullNameInput, binding.emailInput, binding.phoneInput), action = action,
    )

    private fun render(user: LocalUser) {
        if (user.role != "PROSUMER") {
            binding.profileForm.visibility = View.GONE
            binding.messageText.setText(R.string.prosumer_profile_only)
            return
        }
        binding.profileForm.visibility = View.VISIBLE
        binding.avatarText.text = user.fullName.take(1).uppercase()
        binding.profileName.text = user.fullName
        binding.accountStatusChip.solarStatus(user.status)
        binding.identityText.text = getString(R.string.profile_identity, user.nic, user.role, user.status)
        binding.fullNameInput.setText(user.fullName)
        binding.emailInput.setText(user.email)
        binding.phoneInput.setText(user.phone)
        binding.deactivateButton.visibility =
            if (user.status == "DEACTIVATION_REQUESTED") View.GONE else View.VISIBLE
        binding.statusText.text = if (user.status == "DEACTIVATION_REQUESTED")
            getString(R.string.deactivation_pending) else ""
    }
}
