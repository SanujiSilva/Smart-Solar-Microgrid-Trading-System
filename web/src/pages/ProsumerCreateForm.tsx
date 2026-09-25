import { PasswordInput } from '../components/PasswordInput'
import { contactError, emailError, fullNameError, nicError, passwordError, phoneError } from '../lib/contactValidation'
import { useState, type FormEvent } from 'react'
import { apiClient, apiError } from '../lib/api'

export function ProsumerCreateForm({ onCreated, onClose }: { onCreated: () => void; onClose: () => void }) {
  const empty = { nic: '', fullName: '', email: '', phone: '', password: '' }
  const [form, setForm] = useState(empty)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const liveErrors = {
    nic: form.nic ? nicError(form.nic) : '',
    fullName: form.fullName ? fullNameError(form.fullName) : '',
    email: form.email ? emailError(form.email) : '',
    phone: form.phone ? phoneError(form.phone) : '',
    password: form.password ? passwordError(form.password) : '',
  }
  async function submit(event: FormEvent) {
    event.preventDefault(); setError(''); setMessage('')
    const validation = contactError(form)
    if (validation) { setError(validation); return }
    const nicValidation = nicError(form.nic)
    if (nicValidation) { setError(nicValidation); return }
    setBusy(true)
    try {
      await apiClient.post('/prosumers', { ...form, nic: form.nic.trim().toUpperCase() })
      setForm(empty); setMessage('Active prosumer account created.'); onCreated()
    } catch (error) { setError(apiError(error)) } finally { setBusy(false) }
  }
  return <form className="data-panel compact-form" onSubmit={submit} aria-label="Create prosumer">
    <h2>New prosumer account</h2><p>Backoffice-created accounts are active immediately. NIC cannot be changed after creation.</p>
    {error && <div className="alert alert-danger" role="alert">{error}</div>}
    {message && <div className="alert alert-success" role="status">{message}</div>}
    <fieldset disabled={busy}>
      <label className="d-block mb-3">NIC<input className="form-control" required pattern="(?:[0-9]{9}[vVxX]|[0-9]{12})" maxLength={12} value={form.nic} onChange={e => setForm({ ...form, nic: e.target.value })} aria-invalid={!!liveErrors.nic} aria-describedby="prosumer-nic-error" />{liveErrors.nic && <small id="prosumer-nic-error" className="text-danger">{liveErrors.nic}</small>}</label>
      <label className="d-block mb-3">Full name<input className="form-control" required minLength={2} maxLength={150} value={form.fullName} onChange={e => setForm({ ...form, fullName: e.target.value })} aria-invalid={!!liveErrors.fullName} aria-describedby="prosumer-name-error" />{liveErrors.fullName && <small id="prosumer-name-error" className="text-danger">{liveErrors.fullName}</small>}</label>
      <label className="d-block mb-3">Email<input className="form-control" required type="email" maxLength={254} value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} aria-invalid={!!liveErrors.email} aria-describedby="prosumer-email-error" />{liveErrors.email && <small id="prosumer-email-error" className="text-danger">{liveErrors.email}</small>}</label>
      <label className="d-block mb-3">Phone<input className="form-control" required type="tel" inputMode="tel" minLength={10} maxLength={25} pattern="\+?[0-9() .-]*[0-9][0-9() .-]*" value={form.phone} onChange={e => setForm({ ...form, phone: e.target.value })} aria-invalid={!!liveErrors.phone} aria-describedby="prosumer-phone-error" />{liveErrors.phone && <small id="prosumer-phone-error" className="text-danger">{liveErrors.phone}</small>}</label>
      <label className="d-block" htmlFor="prosumer-password">Password</label><div className="mb-3"><PasswordInput id="prosumer-password" className="form-control" required type="password" autoComplete="new-password" minLength={12} maxLength={128} value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} aria-invalid={!!liveErrors.password} aria-describedby="prosumer-password-error" />{liveErrors.password && <small id="prosumer-password-error" className="text-danger">{liveErrors.password}</small>}</div>
      <button className="btn btn-dark" type="submit">{busy ? 'Creating...' : 'Create prosumer account'}</button>{' '}
      <button className="btn btn-outline-dark" type="button" onClick={onClose}>Close prosumer form</button>
    </fieldset>
  </form>
}
