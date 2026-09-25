package com.smartsolar.microgrid

import android.text.Editable
import android.text.TextWatcher
import android.widget.EditText

/** Validate contact details before submitting registration or profile changes. */
fun validateContactInputs(name: EditText, email: EditText, phone: EditText): Boolean {
    val fields = listOf(name, email, phone)
    fields.forEach { it.error = null }
    validateNameInput(name, requestFocus = false)
    validateEmailInput(email, requestFocus = false)
    validatePhoneInput(phone, requestFocus = false)
    val invalid = fields.firstOrNull { it.error != null }
    invalid?.requestFocus()
    return invalid == null
}

fun validateNameInput(name: EditText, requestFocus: Boolean = true): Boolean {
    name.error = null
    val nameValue = name.text.toString().trim()
    if (nameValue.length in 2..150) return true
    name.error = "Full name must contain 2-150 characters."
    if (requestFocus) name.requestFocus()
    return false
}

fun validateEmailInput(email: EditText, requestFocus: Boolean = true): Boolean {
    email.error = null
    val emailValue = email.text.toString().trim()
    if (emailValue.length <= 254 && android.util.Patterns.EMAIL_ADDRESS.matcher(emailValue).matches()) return true
    email.error = "Enter a valid email address."
    if (requestFocus) email.requestFocus()
    return false
}

fun validatePhoneInput(phone: EditText, requestFocus: Boolean = true): Boolean {
    phone.error = null
    val phoneValue = phone.text.toString().trim()
    val phoneDigitCount = phoneValue.count { it.isDigit() }
    if (phoneValue.length <= 25 && Regex("""\+?[0-9() .-]+""").matches(phoneValue) && phoneDigitCount == 10) return true
    phone.error = "Enter a valid 10-digit phone number."
    if (requestFocus) phone.requestFocus()
    return false
}

fun validateNicInput(nic: EditText): Boolean {
    nic.error = null
    if (Regex("(?:[0-9]{12}|[0-9]{9}[vVxX])").matches(nic.text.toString().trim())) return true
    nic.error = "NIC must contain 12 digits or 9 digits followed by V/X."
    nic.requestFocus()
    return false
}

fun attachLiveContactValidation(name: EditText, email: EditText, phone: EditText) {
    name.validateWhileTyping { if (it.isNotBlank()) validateNameInput(name, requestFocus = false) else name.error = null }
    email.validateWhileTyping { if (it.isNotBlank()) validateEmailInput(email, requestFocus = false) else email.error = null }
    phone.validateWhileTyping { if (it.isNotBlank()) validatePhoneInput(phone, requestFocus = false) else phone.error = null }
}

fun attachLiveNicValidation(nic: EditText) {
    nic.validateWhileTyping {
        if (it.isBlank()) {
            nic.error = null
        } else {
            nic.error = if (Regex("(?:[0-9]{12}|[0-9]{9}[vVxX])").matches(it.trim())) null
                else "NIC must contain 12 digits or 9 digits followed by V/X."
        }
    }
}

fun attachLiveLoginValidation(identifier: EditText, password: EditText) {
    identifier.validateWhileTyping {
        if (it.isBlank()) {
            identifier.error = null
            return@validateWhileTyping
        }
        val value = it.trim()
        val valid = Regex("(?:[0-9]{12}|[0-9]{9}[vVxX])").matches(value) ||
            (value.length <= 254 && android.util.Patterns.EMAIL_ADDRESS.matcher(value).matches())
        identifier.error = if (valid) null else "Enter a valid NIC or email address."
    }
    password.validateWhileTyping {
        password.error = if (it.length <= 128) null else "Password must not exceed 128 characters."
    }
}

private fun EditText.validateWhileTyping(action: (String) -> Unit) {
    addTextChangedListener(object : TextWatcher {
        override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) = Unit
        override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) = action(s?.toString().orEmpty())
        override fun afterTextChanged(s: Editable?) = Unit
    })
}
