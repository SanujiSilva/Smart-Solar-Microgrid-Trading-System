import { PasswordInput } from '../components/PasswordInput'
import { contactError, emailError, fullNameError, passwordError, phoneError } from '../lib/contactValidation'
import { useState, type FormEvent } from 'react'
import { apiClient, apiError } from '../lib/api'

export function StaffCreateForm({ onCreated, onClose }: { onCreated: () => void; onClose?: () => void }) {
  const [form, setForm] = useState({ fullName: '', email: '', phone: '', password: '', role: 'GRID_OPERATOR' })
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const liveErrors = {
    fullName: form.fullName ? fullNameError(form.fullName) : '',
    email: form.email ? emailError(form.email) : '',
    phone: form.phone ? phoneError(form.phone) : '',
    password: form.password ? passwordError(form.password) : '',
  }

  async function submit(event: FormEvent) {
    event.preventDefault(); setError(''); setMessage('')
    const validation = contactError(form)
    if (validation) { setError(validation); return }
    setBusy(true)
    try {
      await apiClient.post('/users', { ...form, fullName: form.fullName.trim(), email: form.email.trim(), phone: form.phone.trim() })
      setForm({ fullName: '', email: '', phone: '', password: '', role: 'GRID_OPERATOR' })
      setMessage('Staff account created.'); onCreated()
    } catch (error) { setError(apiError(error)) } finally { setBusy(false) }
  }

  return <form className="data-panel compact-form staff-form" onSubmit={submit}>
    <div className="staff-panel-heading"><h2>New staff account</h2>{onClose && <button type="button" className="panel-close" disabled={busy} onClick={onClose} aria-label="Close staff form">x</button>}</div>
    <p className="staff-intro">Create a new staff account with appropriate access rights.</p>
    {message && <div className="alert alert-success compact-alert">{message}</div>}{error && <div className="alert alert-danger compact-alert" role="alert">{error}</div>}
    <fieldset disabled={busy} className="staff-fields">
      <label>Full name<input className="form-control" placeholder="Enter full name" autoComplete="name" required minLength={2} maxLength={150} value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} aria-invalid={!!liveErrors.fullName} aria-describedby="staff-name-error" />{liveErrors.fullName && <small id="staff-name-error" className="text-danger">{liveErrors.fullName}</small>}</label>
      <label>Role<select className="form-select" value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })}><option>GRID_OPERATOR</option><option>BACKOFFICE</option></select></label>
      <label>Email<input className="form-control" placeholder="Enter email address" autoComplete="email" required type="email" maxLength={254} value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} aria-invalid={!!liveErrors.email} aria-describedby="staff-email-error" />{liveErrors.email && <small id="staff-email-error" className="text-danger">{liveErrors.email}</small>}</label>
      <label>Phone<input className="form-control" placeholder="Enter 10-digit phone number" autoComplete="tel" required type="tel" inputMode="tel" minLength={10} maxLength={25} pattern="\+?[0-9() .-]*[0-9][0-9() .-]*" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} aria-invalid={!!liveErrors.phone} aria-describedby="staff-phone-error" />{liveErrors.phone && <small id="staff-phone-error" className="text-danger">{liveErrors.phone}</small>}</label>
      <label htmlFor="staff-password">Password</label><PasswordInput id="staff-password" className="form-control" placeholder="Enter a secure password" autoComplete="new-password" required minLength={12} maxLength={128} value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} aria-invalid={!!liveErrors.password} aria-describedby="staff-password-error" />{liveErrors.password && <small id="staff-password-error" className="text-danger">{liveErrors.password}</small>}
    </fieldset>
    <p className="staff-info">Use 12-128 characters. Share credentials securely with the staff member.</p>
    <div className="staff-footer">{onClose && <button className="btn btn-outline-dark" disabled={busy} type="button" onClick={onClose}>Cancel</button>}<button className="btn btn-dark" disabled={busy} type="submit">{busy ? 'Creating account...' : 'Create staff account'}</button></div>
  </form>
}
