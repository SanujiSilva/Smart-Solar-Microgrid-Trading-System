package com.smartsolar.microgrid

import android.content.Intent
import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.smartsolar.microgrid.data.auth.AuthRepository
import com.smartsolar.microgrid.databinding.ActivityLoginBinding
import kotlinx.coroutines.launch
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Job
import retrofit2.HttpException

class LoginActivity : AppCompatActivity() {
    private lateinit var binding: ActivityLoginBinding
    private lateinit var authRepository: AuthRepository

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityLoginBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.root.applyAccountInsets()
        authRepository = AuthRepository(this)
        lifecycleScope.coroutineContext[Job]?.invokeOnCompletion { authRepository.close() }
        binding.loginButton.setOnClickListener { submit() }
        binding.registerButton.setOnClickListener {
            startActivity(Intent(this, RegisterActivity::class.java))
        }
        binding.passwordInput.imeOptions = android.view.inputmethod.EditorInfo.IME_ACTION_DONE
        binding.passwordInput.setOnEditorActionListener { _, action, _ ->
            if (action == android.view.inputmethod.EditorInfo.IME_ACTION_DONE) { submit(); true } else false
        }
        restoreExistingSession()
    }

    private fun restoreExistingSession() {
        if (!authRepository.hasSession()) return
        setLoading(true)
        lifecycleScope.launch {
            try {
                if (authRepository.restoreSession() != null) openMain()
            } catch (error: CancellationException) {
                throw error
            } catch (error: Exception) {
                showError(accountError(error))
            } finally {
                setLoading(false)
            }
        }
    }

    private fun submit() {
        if (!binding.loginButton.isEnabled) return
        binding.identifierLayout.error = null
        binding.passwordLayout.error = null
        val identifier = binding.identifierInput.text?.toString()?.trim().orEmpty()
        val password = binding.passwordInput.text?.toString().orEmpty()
        if (identifier.isBlank() || password.isBlank()) {
            if (identifier.isBlank()) binding.identifierLayout.error = getString(R.string.field_required)
            if (password.isBlank()) binding.passwordLayout.error = getString(R.string.field_required)
            showError(getString(R.string.login_fields_required))
            return
        }
        setLoading(true)
        lifecycleScope.launch {
            try {
                authRepository.login(identifier, password)
                openMain()
            } catch (error: CancellationException) {
                throw error
            } catch (error: Exception) {
                setLoading(false)
                showError(errorMessage(error))
            }
        }
    }

    private fun setLoading(loading: Boolean) {
        binding.identifierInput.isEnabled = !loading
        binding.passwordInput.isEnabled = !loading
        binding.loginButton.isEnabled = !loading
        binding.registerButton.isEnabled = !loading
        binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
        if (loading) binding.errorText.visibility = View.GONE
    }

    private fun showError(message: String) {
        binding.errorText.text = message
        binding.errorText.visibility = View.VISIBLE
    }

    private fun errorMessage(error: Throwable): String = when (error) {
        is HttpException -> when (error.code()) {
            401 -> getString(R.string.invalid_login)
            403 -> getString(R.string.inactive_login)
            else -> accountError(error)
        }
        is Exception -> accountError(error)
        else -> getString(R.string.invalid_login)
    }

    private fun openMain() {
        startActivity(Intent(this, MainActivity::class.java))
        finish()
    }
}
