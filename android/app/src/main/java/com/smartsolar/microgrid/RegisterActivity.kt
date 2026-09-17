package com.smartsolar.microgrid

import android.os.Bundle
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.smartsolar.microgrid.data.remote.RegisterProsumerRequest
import com.smartsolar.microgrid.databinding.ActivityRegisterBinding

class RegisterActivity : AccountActivity() {
    private lateinit var binding: ActivityRegisterBinding
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityRegisterBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.root.applyAccountInsets()
        binding.backButton.setOnClickListener { finish() }
        binding.registerButton.setOnClickListener { register() }
    }

    private fun register() {
        val password = binding.passwordInput.text.toString()
        val values = listOf(binding.nicInput, binding.fullNameInput, binding.emailInput, binding.phoneInput)
            .map { it.text.toString().trim() }
        if (values.any { it.isBlank() } || password.isBlank()) {
            binding.messageText.setText(R.string.all_fields_required)
            return
        }
        if (password != binding.confirmPasswordInput.text.toString()) {
            binding.messageText.setText(R.string.password_mismatch)
            return
        }
        request(binding.progressBar, binding.messageText,
            listOf(binding.registerButton, binding.backButton), authenticated = false) {
            account.register(RegisterProsumerRequest(values[0], values[1], values[2], values[3], password))
            binding.passwordInput.text?.clear()
            binding.confirmPasswordInput.text?.clear()
            MaterialAlertDialogBuilder(this)
                .setTitle(R.string.registration_submitted)
                .setMessage(R.string.registration_submitted_body)
                .setCancelable(false)
                .setPositiveButton(R.string.return_to_sign_in) { _, _ -> finish() }
                .show()
        }
    }
}
