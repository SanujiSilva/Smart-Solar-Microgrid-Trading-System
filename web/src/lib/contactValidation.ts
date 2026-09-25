export function fullNameError(fullName: string) {
  if (fullName.trim().length < 2 || fullName.trim().length > 150) return 'Full name must contain 2-150 characters.'
  return ''
}

export function emailError(email: string) {
  if (email.trim().length > 254 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) return 'Enter a valid email address.'
  return ''
}

export function phoneError(phoneValue: string) {
  const phone = phoneValue.trim()
  const phoneDigitCount = phone.replace(/\D/g, '').length
  if (phone.length > 25 || !/^\+?[0-9() .-]+$/.test(phone) || phoneDigitCount !== 10) return 'Enter a valid 10-digit phone number.'
  return ''
}

export function passwordError(password: string) {
  if (password.length < 12 || password.length > 128) return 'Password must contain 12-128 characters.'
  return ''
}

export function contactError(form: { fullName: string; email: string; phone: string }) {
  const nameValidation = fullNameError(form.fullName)
  if (nameValidation) return nameValidation
  const emailValidation = emailError(form.email)
  if (emailValidation) return emailValidation
  const phoneValidation = phoneError(form.phone)
  if (phoneValidation) return phoneValidation
  return ''
}

export function nicError(nic: string) {
  if (!/^(?:[0-9]{9}[vVxX]|[0-9]{12})$/.test(nic.trim())) return 'NIC must contain 12 digits or 9 digits followed by V/X.'
  return ''
}
