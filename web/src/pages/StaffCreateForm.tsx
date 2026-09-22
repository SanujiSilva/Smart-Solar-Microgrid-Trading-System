import { useState, type FormEvent } from 'react'
import { apiClient, apiError } from '../lib/api'

export function StaffCreateForm({ onCreated, onClose }: { onCreated: () => void; onClose?: () => void }) {
  const [form, setForm] = useState({ fullName: '', email: '', phone: '', password: '', role: 'GRID_OPERATOR' })
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [showPassword, setShowPassword] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault(); setError(''); setMessage('')
    setBusy(true)
    try {
      await apiClient.post('/users', form)
      setForm({ fullName: '', email: '', phone: '', password: '', role: 'GRID_OPERATOR' })
      setMessage('Staff account created.'); onCreated()
    } catch (error) { setError(apiError(error)) } finally { setBusy(false) }
  }

  return <form className="data-panel compact-form staff-form" onSubmit={submit}>
    <div className="staff-panel-heading"><h2>New staff account</h2>{onClose && <button type="button" className="panel-close" disabled={busy} onClick={onClose} aria-label="Close staff form">×</button>}</div>
    <p className="staff-intro">Create a new staff account with appropriate access rights.</p>
    {message && <div className="alert alert-success compact-alert">{message}</div>}{error && <div className="alert alert-danger compact-alert">{error}</div>}
    <fieldset disabled={busy} className="staff-fields">
      <label>Full name<input className="form-control" placeholder="Enter full name" autoComplete="name" required minLength={2} maxLength={150} value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} /></label>
      <label>Role<select className="form-select" value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })}><option>GRID_OPERATOR</option><option>BACKOFFICE</option></select></label>
      <label>Email<input className="form-control" placeholder="Enter email address" autoComplete="email" required type="email" maxLength={254} value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></label>
      <label>Phone<input className="form-control" placeholder="Enter phone number" autoComplete="tel" required type="tel" minLength={7} maxLength={25} value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></label>
      <label htmlFor="staff-password">Password</label><div className="staff-password"><input id="staff-password" className="form-control" placeholder="Enter a secure password" autoComplete="new-password" required minLength={12} maxLength={128} type={showPassword ? 'text' : 'password'} value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} /><button type="button" onClick={() => setShowPassword(v => !v)} aria-label={showPassword ? 'Hide password' : 'Show password'} aria-pressed={showPassword}>{showPassword ? 'Hide' : 'Show'}</button></div>
    </fieldset>
    <p className="staff-info">Use 12–128 characters. Share credentials securely with the staff member.</p>
    <div className="staff-footer">{onClose && <button className="btn btn-outline-dark" disabled={busy} type="button" onClick={onClose}>Cancel</button>}<button className="btn btn-dark" disabled={busy} type="submit">{busy ? 'Creating account…' : 'Create staff account'}</button></div>
  </form>
}
