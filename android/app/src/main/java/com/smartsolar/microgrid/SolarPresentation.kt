package com.smartsolar.microgrid

import android.content.res.ColorStateList
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.core.widget.doAfterTextChanged
import com.google.android.material.appbar.MaterialToolbar
import com.google.android.material.button.MaterialButton
import com.google.android.material.chip.Chip

enum class SolarTone { INFO, SUCCESS, WARNING, ERROR }

fun solarStatusColors(status: String?): Pair<Int, Int> = when (status) {
    "ACTIVE", "OPEN", "APPROVED", "COMPLETED" -> R.color.solar_success_background to R.color.solar_success
    "PENDING", "RESERVED", "DEACTIVATION_REQUESTED" -> R.color.solar_warning_background to R.color.solar_warning
    "CANCELLED", "REJECTED", "DEACTIVATED" -> R.color.solar_error_background to R.color.solar_error
    else -> R.color.solar_background to R.color.solar_text_secondary
}

fun Chip.solarStatus(status: String) {
    text = status
    val (background, foreground) = solarStatusColors(status)
    chipBackgroundColor = ColorStateList.valueOf(ContextCompat.getColor(context, background))
    setTextColor(ContextCompat.getColor(context, foreground))
    isCheckable = false
}

fun TextView.solarBanner(tone: SolarTone = SolarTone.INFO) {
    val background = when (tone) {
        SolarTone.INFO -> R.drawable.solar_banner_info
        SolarTone.SUCCESS -> R.drawable.solar_banner_success
        SolarTone.WARNING -> R.drawable.solar_banner_warning
        SolarTone.ERROR -> R.drawable.solar_banner_error
    }
    setBackgroundResource(background)
    setTextColor(ContextCompat.getColor(context, when (tone) {
        SolarTone.INFO -> R.color.solar_primary_dark
        SolarTone.SUCCESS -> R.color.solar_success
        SolarTone.WARNING -> R.color.solar_warning
        SolarTone.ERROR -> R.color.solar_error
    }))
}

fun MaterialButton.asSolarCard(status: String? = null) {
    val (background, foreground) = solarStatusColors(status)
    backgroundTintList = ColorStateList.valueOf(ContextCompat.getColor(context, if (status == null) R.color.solar_surface else background))
    setTextColor(ContextCompat.getColor(context, foreground))
    strokeColor = ColorStateList.valueOf(ContextCompat.getColor(context, R.color.solar_border))
    strokeWidth = resources.getDimensionPixelSize(R.dimen.border_width)
    cornerRadius = resources.getDimensionPixelSize(R.dimen.card_radius)
    gravity = android.view.Gravity.START or android.view.Gravity.CENTER_VERTICAL
    val padding = resources.getDimensionPixelSize(R.dimen.space_lg)
    setPadding(padding, padding, padding, padding)
    setTextSize(android.util.TypedValue.COMPLEX_UNIT_PX, resources.getDimension(R.dimen.body_size))
    isAllCaps = false
    layoutParams = android.widget.LinearLayout.LayoutParams(-1, -2).apply { bottomMargin = resources.getDimensionPixelSize(R.dimen.space_md) }
}

/** XML owns layout and appearance; this helper only wires common behaviour. */
fun View.polishSolarScreen() {
    val activity = context as? AppCompatActivity ?: return
    androidx.core.view.WindowCompat.getInsetsController(activity.window, this).apply {
        isAppearanceLightStatusBars = true
        isAppearanceLightNavigationBars = true
    }
    val toolbar = findViewById<MaterialToolbar>(R.id.solarToolbar)
    val back = findViewById<MaterialButton>(R.id.backButton)
    if (toolbar != null && back != null) {
        toolbar.setNavigationIcon(R.drawable.ic_solar_back)
        toolbar.navigationContentDescription = context.getString(R.string.back_action)
        toolbar.setNavigationOnClickListener { if (back.isEnabled) back.performClick() }
    }
    findViewById<TextView>(R.id.titleText)?.let { title ->
        if (title.text.isNotBlank()) toolbar?.title = title.text
        title.doAfterTextChanged { if (!it.isNullOrBlank()) toolbar?.title = it }
    }
    for (id in listOf(R.id.messageText, R.id.errorText)) {
        findViewById<TextView>(id)?.let { message ->
            message.doAfterTextChanged {
                message.visibility = if (it.isNullOrBlank()) View.GONE else View.VISIBLE
                message.solarBanner(if (id == R.id.errorText) SolarTone.ERROR else SolarTone.INFO)
            }
        }
    }
}
