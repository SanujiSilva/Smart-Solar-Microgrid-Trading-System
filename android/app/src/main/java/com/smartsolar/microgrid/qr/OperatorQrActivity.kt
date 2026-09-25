package com.smartsolar.microgrid.qr

import android.os.Bundle
import android.view.View
import com.smartsolar.microgrid.solarBanner
import com.smartsolar.microgrid.SolarTone
import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import androidx.core.widget.doAfterTextChanged
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.google.zxing.client.android.Intents
import com.journeyapps.barcodescanner.ScanContract
import com.journeyapps.barcodescanner.ScanOptions
import com.smartsolar.microgrid.AccountActivity
import com.smartsolar.microgrid.R
import com.smartsolar.microgrid.applyAccountInsets
import com.smartsolar.microgrid.data.remote.ApiClient
import com.smartsolar.microgrid.data.remote.QrTokenRequest
import com.smartsolar.microgrid.databinding.ActivityOperatorQrBinding

class OperatorQrActivity : AccountActivity() {
    private lateinit var binding: ActivityOperatorQrBinding
    private val api by lazy { ApiClient.qrService(applicationContext) }
    private var verifiedToken: String? = null
    private var completed = false
    private val scanner = registerForActivityResult(ScanContract()) { result ->
        val contents = result.contents
        if (contents.isNullOrBlank()) {
            binding.messageText.setText(R.string.scan_cancelled)
        } else {
            binding.tokenInput.setText(contents)
            verify()
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (!protectSession()) return
        binding = ActivityOperatorQrBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.root.applyAccountInsets()
        verifiedToken = null // Reverify with the API after recreation.
        binding.summaryText.text = savedInstanceState?.getString("summary").orEmpty()
        binding.completeButton.visibility = View.VISIBLE
        updateTransferUi()
        binding.tokenInput.doAfterTextChanged {
            verifiedToken = null
            completed = false
            binding.confirmCheck.isEnabled = false
            binding.confirmCheck.isChecked = false
            binding.summaryText.text = ""
            binding.messageText.text = ""
            updateTransferUi()
        }
        binding.confirmCheck.setOnCheckedChangeListener { _, _ -> updateTransferUi() }
        binding.copyTokenButton.setOnClickListener {
            val token = binding.tokenInput.text.toString()
            if (token.isNotBlank()) {
                (getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager).setPrimaryClip(ClipData.newPlainText(getString(R.string.qr_token), token))
                android.widget.Toast.makeText(this, R.string.token_copied, android.widget.Toast.LENGTH_SHORT).show()
            }
        }
        binding.scanButton.setOnClickListener { scan() }
        binding.verifyButton.setOnClickListener { verify() }
        binding.completeButton.setOnClickListener { confirmComplete() }
        binding.backButton.setOnClickListener { finish() }
    }

    override fun onSaveInstanceState(outState: Bundle) {
        if (::binding.isInitialized) {
            outState.putBoolean("completed", completed)
            outState.putString("summary", binding.summaryText.text.toString())
        }
        super.onSaveInstanceState(outState)
    }

    private fun scan() {
        scanner.launch(ScanOptions()
            .setDesiredBarcodeFormats(ScanOptions.QR_CODE)
            .setPrompt(getString(R.string.scan_qr_prompt))
            .setBeepEnabled(false)
            .addExtra(Intents.Scan.SCAN_TYPE, Intents.Scan.MIXED_SCAN))
    }

    private fun verify() {
        val token = QrPresentation.normalizeToken(binding.tokenInput.text.toString())
        if (token == null) {
            binding.messageText.setText(R.string.qr_token_required)
            return
        }
        verifiedToken = null
        completed = false
        binding.confirmCheck.isChecked = false
        updateTransferUi()
        request(binding.progressBar, binding.messageText,
            listOf(binding.scanButton, binding.verifyButton, binding.completeButton, binding.backButton, binding.tokenInput, binding.confirmCheck, binding.copyTokenButton), onFinished = { updateTransferUi() }) {
            val response = api.verify(QrTokenRequest(token))
            binding.summaryText.text = getString(R.string.reservation_summary, response.reservation.reservationCode, response.reservation.status, com.smartsolar.microgrid.booking.BookingPresentation.time(response.reservation.reservationDateTime), response.reservation.energyAmount.toPlainString(), response.reservation.prosumerNIC) + "\n" + getString(R.string.qr_station_reference, response.reservation.stationId)
            verifiedToken = if (response.valid) token else null
            binding.completeButton.visibility = View.VISIBLE
            binding.confirmCheck.visibility = View.VISIBLE
            binding.confirmCheck.isChecked = false
            binding.stepText.setText(if (response.valid) R.string.qr_step_verified else R.string.qr_step_scan)
            binding.messageText.setText(if (response.valid) R.string.qr_verified else R.string.qr_invalid)
            binding.messageText.solarBanner(if (response.valid) SolarTone.SUCCESS else SolarTone.ERROR)

        }
    }

    private fun confirmComplete() {
        if (requestBusy || verifiedToken == null || !binding.confirmCheck.isChecked) {
            binding.messageText.setText(R.string.transfer_confirmation_required)
            return
        }
        MaterialAlertDialogBuilder(this)
            .setTitle(R.string.complete_transfer)
            .setMessage(R.string.confirm_complete_transfer)
            .setNegativeButton(android.R.string.cancel, null)
            .setPositiveButton(R.string.complete_transfer) { _, _ -> complete() }
            .show()
    }

    private fun complete() {
        if (requestBusy || !binding.confirmCheck.isChecked) return
        val token = verifiedToken
        if (token == null) {
            binding.messageText.setText(R.string.qr_token_required)
            return
        }
        request(binding.progressBar, binding.messageText,
            listOf(binding.scanButton, binding.verifyButton, binding.completeButton, binding.backButton, binding.tokenInput, binding.confirmCheck, binding.copyTokenButton), onFinished = { updateTransferUi() }) {
            verifiedToken = null // A failed response requires a fresh verification before retrying.
            val booking = api.complete(QrTokenRequest(token))
            verifiedToken = null
            binding.summaryText.text = getString(R.string.reservation_summary, booking.reservationCode, booking.status, com.smartsolar.microgrid.booking.BookingPresentation.time(booking.reservationDateTime), booking.energyAmount.toPlainString(), booking.prosumerNIC)
            completed = true
            binding.confirmCheck.isChecked = false
            binding.stepText.setText(R.string.qr_step_completed)
            binding.messageText.setText(R.string.transfer_completed)
            binding.messageText.solarBanner(SolarTone.SUCCESS)
        }
    }
    override fun onRestoreInstanceState(savedInstanceState: Bundle) {
        super.onRestoreInstanceState(savedInstanceState)
        verifiedToken = null
        completed = savedInstanceState.getBoolean("completed")
        binding.summaryText.text = savedInstanceState.getString("summary").orEmpty()
        binding.confirmCheck.isChecked = false
        if (!completed && binding.tokenInput.text?.isNotBlank() == true) binding.messageText.setText(R.string.qr_stale_notice)
        updateTransferUi()
    }

    private fun updateTransferUi() {
        val verified = verifiedToken != null
        binding.completeButton.isEnabled = !requestBusy && verified && binding.confirmCheck.isChecked
        binding.confirmCheck.isEnabled = !requestBusy && verified
        binding.completeButton.visibility = View.VISIBLE
        binding.confirmCheck.visibility = View.VISIBLE
        binding.stepText.setText(when { completed -> R.string.qr_step_completed; verified -> R.string.qr_step_verified; else -> R.string.qr_step_scan })
        binding.scanStep.solarBanner(if (verified || completed) SolarTone.SUCCESS else SolarTone.INFO)
        binding.verifyStep.solarBanner(if (verified || completed) SolarTone.SUCCESS else SolarTone.INFO)
        binding.completeStep.solarBanner(if (completed) SolarTone.SUCCESS else SolarTone.INFO)
    }

}
