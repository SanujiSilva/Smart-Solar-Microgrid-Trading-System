import { useState, type FormEvent } from 'react'
import { apiClient } from '../lib/api'

export function StaffCreateForm({ onCreated }: { onCreated: () => void }) {
  const [form, setForm] = useState({ fullName: '', email: '', phone: '', password: '', role: 'GRID_OPERATOR' })
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  async function submit(event: FormEvent) {
    event.preventDefault(); setError(''); setMessage('')
    try {
      await apiClient.post('/users', form)
      setForm({ fullName: '', email: '', phone: '', password: '', role: 'GRID_OPERATOR' })
      setMessage('Staff account created.'); onCreated()
    } catch { setError('Staff account creation failed. Check the fields and API response.') }
  }

  return <form className="data-panel compact-form staff-form" onSubmit={submit}>
    <div className="panel-label">Add user</div><h2>New staff account</h2>
    {message && <div className="alert alert-success compact-alert">{message}</div>}{error && <div className="alert alert-danger compact-alert">{error}</div>}
    <div className="form-two"><label>Full name<input className="form-control" required value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} /></label><label>Role<select className="form-select" value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })}><option>GRID_OPERATOR</option><option>BACKOFFICE</option></select></label></div>
    <div className="form-two"><label>Email<input className="form-control" required type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></label><label>Phone<input className="form-control" required value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></label></div>
    <label>Temporary password<input className="form-control" required minLength={12} type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} /></label>
    <button className="btn btn-dark" type="submit">Create staff account</button>
  </form>
}
