package com.smartsolar.microgrid.qr

import android.os.Bundle
import android.view.View
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
        verifiedToken = savedInstanceState?.getString("verified_token")
        binding.summaryText.text = savedInstanceState?.getString("summary").orEmpty()
        binding.completeButton.visibility = if (verifiedToken == null) View.GONE else View.VISIBLE
        binding.scanButton.setOnClickListener { scan() }
        binding.verifyButton.setOnClickListener { verify() }
        binding.completeButton.setOnClickListener { confirmComplete() }
        binding.backButton.setOnClickListener { finish() }
    }

    override fun onSaveInstanceState(outState: Bundle) {
        if (::binding.isInitialized) {
            outState.putString("verified_token", verifiedToken)
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
        binding.completeButton.visibility = View.GONE
        request(binding.progressBar, binding.messageText,
            listOf(binding.scanButton, binding.verifyButton, binding.completeButton, binding.backButton)) {
            val response = api.verify(QrTokenRequest(token))
            binding.summaryText.text = QrPresentation.reservationSummary(response.reservation)
            verifiedToken = token
            binding.completeButton.visibility = View.VISIBLE
            binding.messageText.setText(if (response.valid) R.string.qr_verified else R.string.qr_invalid)
        }
    }

    private fun confirmComplete() {
        MaterialAlertDialogBuilder(this)
            .setTitle(R.string.complete_transfer)
            .setMessage(R.string.confirm_complete_transfer)
            .setNegativeButton(android.R.string.cancel, null)
            .setPositiveButton(R.string.complete_transfer) { _, _ -> complete() }
            .show()
    }

    private fun complete() {
        val token = verifiedToken ?: QrPresentation.normalizeToken(binding.tokenInput.text.toString())
        if (token == null) {
            binding.messageText.setText(R.string.qr_token_required)
            return
        }
        request(binding.progressBar, binding.messageText,
            listOf(binding.scanButton, binding.verifyButton, binding.completeButton, binding.backButton)) {
            val booking = api.complete(QrTokenRequest(token))
            verifiedToken = null
            binding.summaryText.text = QrPresentation.reservationSummary(booking)
            binding.completeButton.visibility = View.GONE
            binding.messageText.setText(R.string.transfer_completed)
        }
    }
}
