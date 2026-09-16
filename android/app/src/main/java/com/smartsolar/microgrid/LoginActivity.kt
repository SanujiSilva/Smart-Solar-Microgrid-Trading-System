package com.smartsolar.microgrid

import android.content.Intent
import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.google.gson.JsonParseException
import com.smartsolar.microgrid.data.auth.AuthRepository
import com.smartsolar.microgrid.databinding.ActivityLoginBinding
import kotlinx.coroutines.launch
import retrofit2.HttpException
import java.io.IOException

class LoginActivity : AppCompatActivity() {
    private lateinit var binding: ActivityLoginBinding
    private lateinit var authRepository: AuthRepository

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityLoginBinding.inflate(layoutInflater)
        setContentView(binding.root)
        authRepository = AuthRepository(this)
        binding.loginButton.setOnClickListener { submit() }
        restoreExistingSession()
    }

    private fun restoreExistingSession() {
        if (!authRepository.hasSession()) return
        setLoading(true)
        lifecycleScope.launch {
            if (authRepository.restoreSession() != null) openMain() else setLoading(false)
        }
    }

    private fun submit() {
        val identifier = binding.identifierInput.text?.toString()?.trim().orEmpty()
        val password = binding.passwordInput.text?.toString().orEmpty()
        if (identifier.isBlank() || password.isBlank()) {
            showError("Enter your email or NIC and password.")
            return
        }
        setLoading(true)
        lifecycleScope.launch {
            try {
                authRepository.login(identifier, password)
                openMain()
            } catch (error: Throwable) {
                setLoading(false)
                showError(errorMessage(error))
            }
        }
    }

    private fun setLoading(loading: Boolean) {
        binding.loginButton.isEnabled = !loading
        binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
        if (loading) binding.errorText.visibility = View.GONE
    }

    private fun showError(message: String) {
        binding.errorText.text = message
        binding.errorText.visibility = View.VISIBLE
    }

    private fun errorMessage(error: Throwable): String = when (error) {
        is HttpException -> when (error.code()) {
            401 -> "Invalid identifier or password."
            403 -> "This account is not active. Contact Backoffice."
            else -> "The server could not sign you in. Try again."
        }
        is IOException -> "The API is unavailable. Check the connection and try again."
        is JsonParseException -> "The API returned an unexpected response."
        else -> "Sign-in failed. Try again."
    }

    private fun openMain() {
        startActivity(Intent(this, MainActivity::class.java))
        finish()
    }
}
