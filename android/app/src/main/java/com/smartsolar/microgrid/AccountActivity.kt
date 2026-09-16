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
    private var busy = false
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        account = AuthRepository(this)
        lifecycleScope.coroutineContext[Job]?.invokeOnCompletion { account.close() }
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
        authenticated: Boolean = true, action: suspend () -> Unit,
    ) {
        if (busy) return
        busy = true
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
                }
            } finally {
                busy = false
                progress.visibility = View.GONE
                controls.forEach { it.isEnabled = true }
            }
        }
    }
}

fun View.applyAccountInsets() {
    val left = paddingLeft
    val top = paddingTop
    val right = paddingRight
    val bottom = paddingBottom
    ViewCompat.setOnApplyWindowInsetsListener(this) { view, insets ->
        val bars = insets.getInsets(WindowInsetsCompat.Type.systemBars() or WindowInsetsCompat.Type.ime())
        view.setPadding(left + bars.left, top + bars.top, right + bars.right, bottom + bars.bottom)
        insets
    }
    ViewCompat.requestApplyInsets(this)
}

private data class ApiProblem(val title: String?, val detail: String?, val errors: Map<String, List<String>>?)

fun accountError(error: Exception): String {
    if (error is IOException) return "Cannot reach the server. Check your connection and try again."
    if (error !is HttpException) return "The request could not be completed. Please try again."
    if (error.code() == 429) return "Too many attempts. Wait a minute and try again."
    if (error.code() == 403) return "Your account cannot perform this action. Contact Backoffice."
    if (error.code() in listOf(400, 409)) {
        val problem = runCatching {
            Gson().fromJson(error.response()?.errorBody()?.string(), ApiProblem::class.java)
        }.getOrNull()
        return problem?.errors?.values?.flatten()?.joinToString("\n")?.takeIf { it.isNotBlank() }
            ?: problem?.detail ?: problem?.title ?: "Check your details and try again."
    }
    return "The server could not complete the request. Please try again."
}
