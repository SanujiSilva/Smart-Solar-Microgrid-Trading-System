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
        attachLiveContactValidation(binding.fullNameInput, binding.emailInput, binding.phoneInput)
        attachLiveNicValidation(binding.nicInput)
        binding.backButton.setOnClickListener { finish() }
        binding.registerButton.setOnClickListener { register() }
    }

    private fun register() {
        if (requestBusy) return
        val inputs = listOf(binding.nicInput to binding.nicLayout, binding.fullNameInput to binding.nameLayout,
            binding.emailInput to binding.emailLayout, binding.phoneInput to binding.phoneLayout,
            binding.passwordInput to binding.passwordLayout, binding.confirmPasswordInput to binding.confirmLayout)
        inputs.forEach { (_, layout) -> layout.error = null }
        val missing = inputs.filter { (input, _) -> input.text.isNullOrBlank() }
        if (missing.isNotEmpty()) {
            missing.forEach { (_, layout) -> layout.error = getString(R.string.field_required) }
            missing.first().first.requestFocus()
            return
        }
        if (!validateContactInputs(binding.fullNameInput, binding.emailInput, binding.phoneInput)) return
        if (!validateNicInput(binding.nicInput)) return
        if (!android.util.Patterns.EMAIL_ADDRESS.matcher(binding.emailInput.text.toString().trim()).matches()) {
            binding.emailLayout.error = getString(R.string.email_format_error); return
        }
        if (binding.passwordInput.text.toString().length !in 12..128) {
            binding.passwordLayout.error = getString(R.string.password_length_error); return
        }
        val password = binding.passwordInput.text.toString()
        val values = listOf(binding.nicInput, binding.fullNameInput, binding.emailInput, binding.phoneInput)
            .map { it.text.toString().trim() }
        if (values.any { it.isBlank() } || password.isBlank()) {
            binding.messageText.setText(R.string.all_fields_required)
            return
        }
        if (password != binding.confirmPasswordInput.text.toString()) {
            binding.confirmLayout.error = getString(R.string.password_mismatch)
            return
        }
        request(binding.progressBar, binding.messageText,
            listOf(binding.registerButton, binding.backButton, binding.nicInput, binding.fullNameInput, binding.emailInput, binding.phoneInput, binding.passwordInput, binding.confirmPasswordInput), authenticated = false) {
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
