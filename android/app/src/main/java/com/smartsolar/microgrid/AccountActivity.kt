package com.smartsolar.microgrid

import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat
import androidx.lifecycle.lifecycleScope
import com.google.gson.Gson
import com.smartsolar.microgrid.data.auth.AuthRepository
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import retrofit2.HttpException
import java.io.IOException

abstract class AccountActivity : AppCompatActivity() {
    protected lateinit var account: AuthRepository
    protected var requestBusy = false
        private set
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        account = AuthRepository(this)
        lifecycleScope.coroutineContext[Job]?.invokeOnCompletion { account.close() }
    }

    override fun onPostCreate(savedInstanceState: Bundle?) {
        super.onPostCreate(savedInstanceState)
        if (!account.hasSession() || this is RegisterActivity) return
        findViewById<com.google.android.material.appbar.MaterialToolbar>(R.id.solarToolbar)?.let { toolbar ->
            toolbar.menu.add(getString(R.string.sign_out)).apply {
                setShowAsAction(android.view.MenuItem.SHOW_AS_ACTION_ALWAYS)
                setOnMenuItemClickListener {
                    if (!requestBusy) confirmSignOut()
                    true
                }
            }
        }
    }

    protected fun confirmSignOut() {
        com.google.android.material.dialog.MaterialAlertDialogBuilder(this)
            .setTitle(R.string.sign_out_title).setMessage(R.string.sign_out_message)
            .setNegativeButton(android.R.string.cancel, null)
            .setPositiveButton(R.string.sign_out) { _, _ -> signOut() }.show()
    }

    protected fun configurePrimaryNavigation(selected: Int) {
        val navigation = findViewById<com.google.android.material.bottomnavigation.BottomNavigationView>(R.id.bottomNavigation) ?: return
        lifecycleScope.launch {
            val role = account.cachedUser()?.role ?: return@launch
            if (role !in listOf("PROSUMER", "GRID_OPERATOR")) return@launch
            navigation.menu.findItem(R.id.nav_bookings).isVisible = role == "PROSUMER"
            navigation.menu.findItem(R.id.nav_profile).isVisible = role == "PROSUMER"
            navigation.menu.findItem(R.id.nav_scan).isVisible = role == "GRID_OPERATOR"
            navigation.selectedItemId = selected
            navigation.visibility = View.VISIBLE
            navigation.setOnItemSelectedListener { item ->
                if (requestBusy) return@setOnItemSelectedListener false
                if (item.itemId != selected) {
                    val target = when (item.itemId) {
                        R.id.nav_home -> MainActivity::class.java
                        R.id.nav_stations -> NearbyStationsActivity::class.java
                        R.id.nav_bookings -> com.smartsolar.microgrid.booking.BookingsActivity::class.java
                        R.id.nav_profile -> ProfileActivity::class.java
                        else -> com.smartsolar.microgrid.qr.OperatorQrActivity::class.java
                    }
                    startActivity(Intent(this@AccountActivity, target).addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP))
                    if (this@AccountActivity !is MainActivity) finish()
                }
                true
            }
        }
    }

    protected fun protectSession(): Boolean {
        if (account.hasSession()) return true
        openLogin()
        return false
    }

    protected fun openLogin() {
        startActivity(Intent(this, LoginActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        })
        finish()
    }

    protected fun signOut() {
        lifecycleScope.launch {
            withContext(Dispatchers.IO) { account.logout() }
            openLogin()
        }
    }

    protected fun request(
        progress: View, message: TextView, controls: List<View>,
        authenticated: Boolean = true, onFinished: () -> Unit = {}, action: suspend () -> Unit,
    ) {
        if (requestBusy) return
        requestBusy = true
        controls.forEach { it.isEnabled = false }
        progress.visibility = View.VISIBLE
        message.text = ""
        lifecycleScope.launch {
            try {
                action()
            } catch (error: CancellationException) {
                throw error
            } catch (error: Exception) {
                if (authenticated && error is HttpException && error.code() == 401) {
                    withContext(Dispatchers.IO) { account.logout() }
                    openLogin()
                } else {
                    message.text = accountError(error)
                    message.solarBanner(SolarTone.ERROR)
                }
            } finally {
                requestBusy = false
                progress.visibility = View.GONE
                controls.forEach { it.isEnabled = true }
                onFinished()
            }
        }
    }
}

fun View.applyAccountInsets() {
    polishSolarScreen()
    val left = paddingLeft
    val top = paddingTop
    val right = paddingRight
    val bottom = paddingBottom
    ViewCompat.setOnApplyWindowInsetsListener(this) { view, insets ->
        val bars = insets.getInsets(WindowInsetsCompat.Type.systemBars() or WindowInsetsCompat.Type.ime() or WindowInsetsCompat.Type.displayCutout())
        view.setPadding(left + bars.left, top + bars.top, right + bars.right, bottom + bars.bottom)
        insets
    }
    ViewCompat.requestApplyInsets(this)
}

private data class ApiProblem(val title: String?, val detail: String?, val errors: Map<String, List<String>>?)

fun accountError(error: Exception): String {
    if (error is IOException) return "Cannot reach the server. Check your connection and try again."
    if (error !is HttpException) return "The request could not be completed. Please try again."
    if (error.code() == 401) return "Your session expired. Sign in again."
    if (error.code() == 408) return "The request timed out. Check your connection and try again."
    if (error.code() == 429) return "Too many attempts. Wait a minute and try again."
    if (error.code() == 404) return "The requested record was not found. Refresh and try again."
    if (error.code() == 403) return "Your account cannot perform this action. Contact Backoffice."
    if (error.code() in listOf(400, 409)) {
        val problem = runCatching {
            val body = error.response()?.errorBody()?.source()?.peek()?.use { it.readUtf8() }
            Gson().fromJson(body, ApiProblem::class.java)
        }.getOrNull()
        return problem?.errors?.values?.flatten()?.joinToString("\n")?.takeIf { it.isNotBlank() }
            ?: problem?.detail ?: problem?.title ?: "Check your details and try again."
    }
    return "The server could not complete the request. Please try again."
}
