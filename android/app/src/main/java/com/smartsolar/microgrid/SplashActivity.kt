package com.smartsolar.microgrid

import android.content.Intent
import android.os.Bundle
import com.smartsolar.microgrid.databinding.ActivitySplashBinding

class SplashActivity : AccountActivity() {
    private lateinit var binding: ActivitySplashBinding
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivitySplashBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.root.applyAccountInsets()
        binding.retryButton.setOnClickListener { restore() }
        binding.signInButton.setOnClickListener { openLogin() }
        restore()
    }

    private fun restore() {
        if (!account.hasSession()) { openLogin(); return }
        binding.retryButton.visibility = android.view.View.GONE
        binding.signInButton.visibility = android.view.View.GONE
        request(binding.progressBar, binding.messageText, listOf(binding.retryButton, binding.signInButton), onFinished = {
            if (binding.messageText.text.isNotBlank()) {
                binding.retryButton.visibility = android.view.View.VISIBLE
                binding.signInButton.visibility = android.view.View.VISIBLE
            }
        }) {
            account.currentUser()
            startActivity(Intent(this, MainActivity::class.java))
            finish()
        }
    }
}
