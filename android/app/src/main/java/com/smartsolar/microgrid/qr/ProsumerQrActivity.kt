package com.smartsolar.microgrid.qr

import android.os.Bundle
import com.smartsolar.microgrid.AccountActivity
import com.smartsolar.microgrid.R
import com.smartsolar.microgrid.applyAccountInsets
import com.smartsolar.microgrid.data.remote.ApiClient
import com.smartsolar.microgrid.databinding.ActivityProsumerQrBinding

class ProsumerQrActivity : AccountActivity() {
    private lateinit var binding: ActivityProsumerQrBinding
    private val api by lazy { ApiClient.qrService(applicationContext) }
    private val reservationId by lazy { intent.getStringExtra(RESERVATION_ID).orEmpty() }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (!protectSession()) return
        binding = ActivityProsumerQrBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.root.applyAccountInsets()
        binding.refreshButton.setOnClickListener { loadQr() }
        binding.backButton.setOnClickListener { finish() }
        loadQr()
    }

    private fun loadQr() {
        if (reservationId.isBlank()) {
            binding.messageText.setText(R.string.booking_not_found)
            return
        }
        request(binding.progressBar, binding.messageText, listOf(binding.refreshButton, binding.backButton)) {
            val response = api.issue(reservationId)
            binding.summaryText.text = QrPresentation.reservationSummary(response.reservation)
            binding.qrImage.setImageBitmap(QrPresentation.qrBitmap(response.qrToken))
            binding.messageText.setText(R.string.qr_ready)
        }
    }

    companion object {
        const val RESERVATION_ID = "reservation_id"
    }
}
