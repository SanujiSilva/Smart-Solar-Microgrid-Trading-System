import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { apiClient, apiError } from '../lib/api'
import { StaffCreateForm } from './StaffCreateForm'

type Page<T> = { items: T[]; totalCount: number }
type User = { id: string; nic: string | null; fullName: string; email: string; phone: string; role: string; status: string }
type Period = { day: string; openMinuteOfDay: number; closeMinuteOfDay: number }
type Schedule = { timeZoneId: string; weeklyPeriods: Period[] }
type Station = { id: string; stationCode: string; name: string; address: string; latitude: number; longitude: number; capacityKWh: number; availableBatterySlots: number; status: string; operatingSchedule: Schedule }
type Slot = { id: string; startTime: string; endTime: string; capacity: number; availableCapacity: number; status: string }
type Reservation = { id: string; reservationCode: string; prosumerNIC: string; stationId: string; slotId: string; energyAmount: number; reservationDateTime: string; status: string; createdAt: string; completedAt?: string; completedByOperatorId?: string }

function useData<T>(url: string, params: Record<string, unknown> = {}) {
  const [result, setResult] = useState<{ key: string; data: T | null; error: string } | null>(null)
  const [version, setVersion] = useState(0)
  const key = JSON.stringify(params)
  const requestKey = url + key + version
  const reload = useCallback(() => setVersion(v => v + 1), [])
  useEffect(() => {
    const controller = new AbortController()
    apiClient.get<T>(url, { params: JSON.parse(key), signal: controller.signal })
      .then(response => { if (!controller.signal.aborted) setResult({ key: requestKey, data: response.data, error: '' }) })
      .catch(error => { if (!controller.signal.aborted) setResult({ key: requestKey, data: null, error: apiError(error) }) })
    return () => controller.abort()
  }, [url, key, requestKey])
  return { data: result?.key === requestKey ? result.data : null, error: result?.key === requestKey ? result.error : '', reload }
}
function Notice({ error, message = '' }: { error: string; message?: string }) {
  return <>{error && <div className="alert alert-danger" role="alert">{error}</div>}{message && <div className="alert alert-success" role="status">{message}</div>}</>
}
function Pager({ page, total, change }: { page: number; total: number; change: (page: number) => void }) {
  return <div className="d-flex gap-3 align-items-center mt-3"><button className="btn btn-outline-dark" disabled={page === 1} onClick={() => change(page - 1)}>Previous</button><span>Page {page} · {total} records</span><button className="btn btn-outline-dark" disabled={page * 20 >= total} onClick={() => change(page + 1)}>Next</button></div>
}
function Field({ label, value, change, type = 'text', disabled = false }: { label: string; value: string | number; change: (value: string) => void; type?: string; disabled?: boolean }) {
  return <label className="d-block mb-3">{label}<input className="form-control" required type={type} step={type === 'number' ? 'any' : undefined} value={value} disabled={disabled} onChange={e => change(e.target.value)} /></label>
}
const date = (value: string) => new Date(value).toLocaleString()
const localDate = (value: string) => { const d = new Date(value); return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16) }

export function AccountsView({ prosumers }: { prosumers: boolean }) {
  const [page, setPage] = useState(1), [search, setSearch] = useState(''), [status, setStatus] = useState('')
  const { data, error, reload } = useData<Page<User>>(prosumers ? '/prosumers' : '/users', { page, pageSize: 20, search, status: status || undefined })
  const [selected, setSelected] = useState<User | null>(null), [actionError, setActionError] = useState(''), [message, setMessage] = useState(''), [busy, setBusy] = useState(false)
  async function act(task: () => Promise<unknown>, success: string) {
    setBusy(true); setActionError(''); setMessage('')
    try { await task(); setMessage(success); reload() } catch (e) { setActionError(apiError(e)) } finally { setBusy(false) }
  }
  async function open(user: User) {
    await act(async () => { const result = await apiClient.get<User>(prosumers ? `/prosumers/${encodeURIComponent(user.nic!)}` : `/users/${user.id}`); setSelected(result.data) }, '')
  }
  function changeStatus(user: User, status: string) {
    if (!window.confirm(`${status === 'ACTIVE' ? 'Activate' : 'Deactivate'} ${user.fullName}?`)) return
    void act(() => user.role === 'PROSUMER' && user.status === 'DEACTIVATED'
      ? apiClient.patch(`/prosumers/${encodeURIComponent(user.nic!)}/activate`)
      : apiClient.patch(`/users/${user.id}/status`, { status }), 'Account status updated.')
  }
  return <><h1>{prosumers ? 'Prosumer accounts' : 'User management'}</h1>
    {!prosumers && <StaffCreateForm onCreated={reload} />}
    <section className="data-panel">
      <div className="filter-row"><input className="form-control" aria-label="Search accounts" placeholder="Name, email or NIC" value={search} onChange={e => { setSearch(e.target.value); setPage(1) }} /><select className="form-select" aria-label="Account status" value={status} onChange={e => { setStatus(e.target.value); setPage(1) }}><option value="">All statuses</option>{['PENDING','ACTIVE','DEACTIVATION_REQUESTED','DEACTIVATED'].map(s => <option key={s}>{s}</option>)}</select><button className="btn btn-dark" onClick={reload}>Refresh</button></div>
      <Notice error={error || actionError} message={message} />
      {!data && !error && <p role="status">Loading accounts...</p>}
      {data && <><div className="table-responsive"><table className="table"><thead><tr><th>Name</th><th>NIC / role</th><th>Contact</th><th>Status</th><th>Actions</th></tr></thead><tbody>{data.items.map(u => <tr key={u.id}><td>{u.fullName}</td><td>{u.nic || u.role}</td><td>{u.email}<br />{u.phone}</td><td>{u.status}</td><td><button className="btn btn-sm btn-outline-dark" disabled={busy} onClick={() => void open(u)}>Details{u.role !== 'PROSUMER' && ' / Edit'}</button>{u.status === 'PENDING' || u.status === 'DEACTIVATED' ? <button className="btn btn-sm btn-success" disabled={busy} onClick={() => changeStatus(u, 'ACTIVE')}>{u.status === 'PENDING' ? 'Approve account' : 'Reactivate'}</button> : <button className="btn btn-sm btn-outline-danger" disabled={busy} onClick={() => changeStatus(u, 'DEACTIVATED')}>Deactivate</button>}</td></tr>)}</tbody></table></div>{data.items.length === 0 && <p>No accounts match.</p>}<Pager page={page} total={data.totalCount} change={setPage} /></>}
    </section>
    {selected && <form className="data-panel" onSubmit={e => { e.preventDefault(); void act(async () => { const result = await apiClient.put<User>(`/users/${selected.id}`, { fullName: selected.fullName, email: selected.email, phone: selected.phone }); setSelected(result.data) }, 'User saved.') }}>
      <h2>Account details</h2><p>{selected.nic || selected.id} · {selected.role} · {selected.status}</p>
      <fieldset disabled={busy || selected.role === 'PROSUMER'}>{(['fullName','email','phone'] as const).map(k => <Field key={k} label={{ fullName: 'Full name', email: 'Email', phone: 'Phone' }[k]} value={selected[k]} change={value => setSelected({ ...selected, [k]: value })} />)}{selected.role !== 'PROSUMER' && <button className="btn btn-dark">Save changes</button>}</fieldset>
      {selected.role === 'PROSUMER' && <p>Prosumers edit their own contact details in the Android app.</p>}<button type="button" className="btn btn-outline-dark" onClick={() => setSelected(null)}>Close</button>
    </form>}
  </>
}

const emptyStation: Station = { id: '', stationCode: '', name: '', address: '', latitude: 0, longitude: 0, capacityKWh: 1, availableBatterySlots: 1, status: 'ACTIVE', operatingSchedule: { timeZoneId: 'Asia/Colombo', weeklyPeriods: [] } }
const days = ['Monday','Tuesday','Wednesday','Thursday','Friday','Saturday','Sunday']
const clock = (minutes: number) => `${String(Math.floor(minutes / 60)).padStart(2, '0')}:${String(minutes % 60).padStart(2, '0')}`
const minutes = (text: string) => { const [h, m] = text.split(':').map(Number); return h * 60 + m }

export function StationManagement({ operator = false }: { operator?: boolean }) {
  const [page, setPage] = useState(1), [search, setSearch] = useState('')
  const { data, error, reload } = useData<Page<Station>>('/stations', { page, pageSize: 20, search })
  const [selected, setSelected] = useState<Station | null>(null), [form, setForm] = useState<Station | null>(null), [busy, setBusy] = useState(false), [actionError, setActionError] = useState(''), [message, setMessage] = useState('')
  async function act(task: () => Promise<unknown>, success: string) {
    setBusy(true); setActionError(''); setMessage('')
    try { await task(); reload(); setMessage(success) } catch (e) { setActionError(apiError(e)) } finally { setBusy(false) }
  }
  async function open(station: Station) {
    await act(async () => { const result = await apiClient.get<Station>(`/stations/${station.id}`); setSelected(result.data); setForm(result.data) }, '')
  }
  async function save(e: FormEvent) {
    e.preventDefault(); if (!form) return
    await act(async () => {
      if (operator) { const result = await apiClient.patch<Station>(`/stations/${form.id}/availability`, { availableBatterySlots: Number(form.availableBatterySlots) }); setForm(result.data); setSelected(result.data); return }
      const body = { name: form.name, address: form.address, latitude: Number(form.latitude), longitude: Number(form.longitude), capacityKWh: Number(form.capacityKWh), availableBatterySlots: Number(form.availableBatterySlots), status: form.status, operatingSchedule: form.operatingSchedule }
      const result = form.id ? await apiClient.put<Station>(`/stations/${form.id}`, body) : await apiClient.post<Station>('/stations', { ...body, stationCode: form.stationCode })
      setForm(result.data); setSelected(result.data)
    }, 'Station saved.')
  }
  return <><h1>{operator ? 'Station overview and availability' : 'Microgrid node management'}</h1><section className="data-panel"><div className="filter-row"><input className="form-control" aria-label="Search stations" placeholder="Search stations" value={search} onChange={e => { setSearch(e.target.value); setPage(1) }} /><button className="btn btn-dark" onClick={reload}>Refresh</button>{!operator && <button className="btn btn-outline-dark" onClick={() => { setSelected(null); setForm(structuredClone(emptyStation)) }}>Add station</button>}</div><Notice error={error || actionError} message={message} />
    {!data && !error && <p>Loading stations...</p>}{data && <><div className="table-responsive"><table className="table"><thead><tr><th>Station</th><th>Address</th><th>Capacity</th><th>Battery slots</th><th>Status</th><th /></tr></thead><tbody>{data.items.map(s => <tr key={s.id}><td>{s.stationCode}<br />{s.name}</td><td>{s.address}</td><td>{s.capacityKWh} kWh</td><td>{s.availableBatterySlots}</td><td>{s.status}</td><td><button className="btn btn-outline-dark btn-sm" disabled={busy} onClick={() => void open(s)}>Details / Manage</button></td></tr>)}</tbody></table></div>{data.items.length === 0 && <p>No matching stations.</p>}<Pager page={page} total={data.totalCount} change={setPage} /></>}</section>
    {form && <form className="data-panel" onSubmit={save}><h2>{form.id ? form.name : 'New station'}</h2><fieldset disabled={busy}>
      <fieldset disabled={operator}><Field label="Station code" value={form.stationCode} disabled={Boolean(form.id)} change={v => setForm({ ...form, stationCode: v })} />{(['name','address','latitude','longitude','capacityKWh'] as const).map(k => <Field key={k} label={{name:'Name',address:'Address',latitude:'Latitude',longitude:'Longitude',capacityKWh:'Capacity (kWh)'}[k]} type={['latitude','longitude','capacityKWh'].includes(k) ? 'number' : 'text'} value={form[k]} change={v => setForm({ ...form, [k]: v })} />)}
      <label>Status<select className="form-select mb-3" value={form.status} onChange={e => setForm({ ...form, status: e.target.value })}>{['ACTIVE','INACTIVE','MAINTENANCE', ...(form.status === 'DEACTIVATED' ? ['DEACTIVATED'] : [])].map(s => <option key={s}>{s}</option>)}</select></label>
      <h3>Operating schedule</h3><Field label="Time zone" value={form.operatingSchedule.timeZoneId} change={v => setForm({ ...form, operatingSchedule: { ...form.operatingSchedule, timeZoneId: v } })} />
      {form.operatingSchedule.weeklyPeriods.map((p, i) => <div className="row mb-2" key={i}><label className="col">Day<select className="form-select" value={p.day} onChange={e => setForm({ ...form, operatingSchedule: { ...form.operatingSchedule, weeklyPeriods: form.operatingSchedule.weeklyPeriods.map((x, n) => n === i ? { ...x, day: e.target.value } : x) } })}>{days.map(d => <option key={d}>{d}</option>)}</select></label>{(['openMinuteOfDay','closeMinuteOfDay'] as const).map(k => <label className="col" key={k}>{k === 'openMinuteOfDay' ? 'Opens' : 'Closes (24:00 = midnight)'}<input className="form-control" required pattern="[0-2][0-9]:[0-5][0-9]" defaultValue={clock(p[k])} onBlur={e => setForm({ ...form, operatingSchedule: { ...form.operatingSchedule, weeklyPeriods: form.operatingSchedule.weeklyPeriods.map((x, n) => n === i ? { ...x, [k]: minutes(e.target.value) } : x) } })} /></label>)}<button type="button" className="btn btn-outline-danger col-auto" onClick={() => setForm({ ...form, operatingSchedule: { ...form.operatingSchedule, weeklyPeriods: form.operatingSchedule.weeklyPeriods.filter((_, n) => n !== i) } })}>Remove period</button></div>)}
      <button type="button" className="btn btn-outline-dark mb-3" onClick={() => setForm({ ...form, operatingSchedule: { ...form.operatingSchedule, weeklyPeriods: [...form.operatingSchedule.weeklyPeriods, { day: 'Monday', openMinuteOfDay: 480, closeMinuteOfDay: 1020 }] } })}>Add operating period</button></fieldset>
      <Field label="Available battery slots" type="number" value={form.availableBatterySlots} change={v => setForm({ ...form, availableBatterySlots: Number(v) })} />
      <button className="btn btn-dark">Save {operator ? 'availability' : 'station'}</button>
      {!operator && form.id && form.status !== 'DEACTIVATED' && <button type="button" className="btn btn-outline-danger" onClick={() => { if (window.confirm('Deactivate this station?')) void act(async () => { const result = await apiClient.patch<Station>(`/stations/${form.id}/deactivate`); setForm(result.data); setSelected(result.data) }, 'Station deactivated.') }}>Deactivate</button>}
      <button type="button" className="btn btn-outline-dark" onClick={() => { setForm(null); setSelected(null) }}>Close</button></fieldset></form>}
      {selected && <SlotManagement key={selected.id} station={selected} operator={operator} />}
    </>
}

function SlotManagement({ station, operator }: { station: Station; operator: boolean }) {
  const { data, error, reload } = useData<{ items: Slot[] }>(`/stations/${station.id}/slots`, { includeCancelled: true })
  const [form, setForm] = useState<Slot | null>(null), [busy, setBusy] = useState(false), [actionError, setActionError] = useState(''), [message, setMessage] = useState('')
  async function act(task: () => Promise<unknown>) { setBusy(true); setActionError(''); setMessage(''); try { await task(); setForm(null); reload(); setMessage('Slot saved.') } catch (e) { setActionError(apiError(e)) } finally { setBusy(false) } }
  async function save(e: FormEvent) {
    e.preventDefault(); if (!form) return
    await act(() => {
      const availability = { availableCapacity: Number(form.availableCapacity), status: form.status }
      if (operator) return apiClient.patch(`/slots/${form.id}/availability`, availability)
      const body = { ...availability, capacity: Number(form.capacity), startTime: new Date(form.startTime).toISOString(), endTime: new Date(form.endTime).toISOString() }
      return form.id ? apiClient.put(`/slots/${form.id}`, body) : apiClient.post(`/stations/${station.id}/slots`, body)
    })
  }
  return <section className="data-panel"><h2>Energy slots · {station.name}</h2><Notice error={error || actionError} message={message} /><button className="btn btn-outline-dark" onClick={reload}>Refresh slots</button>{!operator && <button className="btn btn-dark" onClick={() => setForm({ id: '', startTime: '', endTime: '', capacity: 1, availableCapacity: 1, status: 'OPEN' })}>Add slot</button>}
    {!data && !error && <p>Loading slots...</p>}{data && <div className="table-responsive"><table className="table"><thead><tr><th>Window</th><th>Capacity</th><th>Available</th><th>Status</th><th /></tr></thead><tbody>{data.items.map(s => <tr key={s.id}><td>{date(s.startTime)} – {date(s.endTime)}</td><td>{s.capacity}</td><td>{s.availableCapacity}</td><td>{s.status}</td><td><button className="btn btn-sm btn-outline-dark" disabled={busy || s.status === 'CANCELLED'} onClick={() => setForm({ ...s, startTime: localDate(s.startTime), endTime: localDate(s.endTime) })}>Edit {operator ? 'availability' : 'slot'}</button>{!operator && <button className="btn btn-sm btn-outline-danger" disabled={busy || s.status === 'CANCELLED'} onClick={() => { if (window.confirm('Cancel this slot?')) void act(() => apiClient.delete(`/slots/${s.id}`)) }}>Cancel slot</button>}</td></tr>)}</tbody></table>{data.items.length === 0 && <p>No slots configured.</p>}</div>}
    {form && <form onSubmit={save}><fieldset disabled={busy}><fieldset disabled={operator}><Field label="Start (local time)" type="datetime-local" value={form.startTime} change={v => setForm({ ...form, startTime: v })} /><Field label="End (local time)" type="datetime-local" value={form.endTime} change={v => setForm({ ...form, endTime: v })} /><Field label="Capacity (kWh)" type="number" value={form.capacity} change={v => setForm({ ...form, capacity: Number(v) })} /></fieldset><Field label="Available capacity (kWh)" type="number" value={form.availableCapacity} change={v => setForm({ ...form, availableCapacity: Number(v) })} /><label>Status<select className="form-select mb-3" value={form.status} onChange={e => setForm({ ...form, status: e.target.value })}><option>OPEN</option><option>CLOSED</option></select></label><button className="btn btn-dark">Save slot</button><button type="button" className="btn btn-outline-dark" onClick={() => setForm(null)}>Close editor</button></fieldset></form>}
    </section>
}

export function ReservationManagement({ operator = false }: { operator?: boolean }) {
  const [page, setPage] = useState(1), [filters, setFilters] = useState({ reservationCode: '', stationId: '', status: '', from: '', to: '' })
  const params = { ...filters, page, pageSize: 20, status: filters.status || undefined, from: filters.from ? new Date(filters.from).toISOString() : undefined, to: filters.to ? new Date(filters.to).toISOString() : undefined }
  const { data, error, reload } = useData<Page<Reservation>>('/reservations/search', params)
  const [selected, setSelected] = useState<Reservation | null>(null), [busy, setBusy] = useState(false), [actionError, setActionError] = useState(''), [message, setMessage] = useState('')
  async function act(task: () => Promise<unknown>, message: string) { setBusy(true); setActionError(''); setMessage(''); try { await task(); reload(); setMessage(message) } catch (e) { setActionError(apiError(e)) } finally { setBusy(false) } }
  const filter = (key: keyof typeof filters, value: string) => { setFilters({ ...filters, [key]: value }); setPage(1) }
  function change(action: string) {
    if (!selected || !window.confirm(`${action} reservation ${selected.reservationCode}?`)) return
    void act(async () => { const result = action === 'Cancel' ? await apiClient.delete<Reservation>(`/reservations/${selected.id}`) : await apiClient.patch<Reservation>(`/reservations/${selected.id}/${action.toLowerCase()}`); setSelected(result.data) }, 'Reservation updated.')
  }
  return <><h1>Reservation management</h1><section className="data-panel"><div className="row">{(['reservationCode','stationId','from','to'] as const).map(k => <label className="col-md-3 mb-3" key={k}>{{reservationCode:'Reservation code',stationId:'Station ID',from:'From (local time)',to:'Until (exclusive, local time)'}[k]}<input className="form-control" type={k === 'from' || k === 'to' ? 'datetime-local' : 'text'} value={filters[k]} onChange={e => filter(k, e.target.value)} /></label>)}</div><label>Status<select className="form-select mb-3" value={filters.status} onChange={e => filter('status', e.target.value)}><option value="">All statuses</option>{['PENDING','APPROVED','CANCELLED','COMPLETED','REJECTED'].map(s => <option key={s}>{s}</option>)}</select></label><button className="btn btn-outline-dark" onClick={reload}>Refresh</button><Notice error={error || actionError} message={message} />
    {!data && !error && <p>Loading reservations...</p>}{data && <><div className="table-responsive"><table className="table"><thead><tr><th>Code</th><th>Prosumer</th><th>Scheduled</th><th>Energy</th><th>Status</th><th /></tr></thead><tbody>{data.items.map(r => <tr key={r.id}><td>{r.reservationCode}</td><td>{r.prosumerNIC}</td><td>{date(r.reservationDateTime)}</td><td>{r.energyAmount} kWh</td><td>{r.status}</td><td><button className="btn btn-sm btn-outline-dark" disabled={busy} onClick={() => void act(async () => { const result = await apiClient.get<Reservation>(`/reservations/${r.id}`); setSelected(result.data) }, '')}>Details</button></td></tr>)}</tbody></table></div>{data.items.length === 0 && <p>No reservations match.</p>}<Pager page={page} total={data.totalCount} change={setPage} /></>}</section>
    {selected && <section className="data-panel"><h2>{selected.reservationCode}</h2><dl><dt>Prosumer NIC</dt><dd>{selected.prosumerNIC}</dd><dt>Station / slot</dt><dd>{selected.stationId} / {selected.slotId}</dd><dt>Scheduled</dt><dd>{date(selected.reservationDateTime)}</dd><dt>Energy</dt><dd>{selected.energyAmount} kWh</dd><dt>Status</dt><dd>{selected.status}</dd><dt>Created</dt><dd>{date(selected.createdAt)}</dd>{selected.completedAt && <><dt>Completed</dt><dd>{date(selected.completedAt)} by {selected.completedByOperatorId}</dd></>}</dl>
      {!operator && selected.status === 'PENDING' && <><button className="btn btn-success" disabled={busy} onClick={() => change('Approve')}>Approve</button><button className="btn btn-outline-danger" disabled={busy} onClick={() => change('Reject')}>Reject</button></>}
      {['PENDING','APPROVED'].includes(selected.status) && <button className="btn btn-outline-danger" disabled={busy} onClick={() => change('Cancel')}>Cancel reservation</button>}<button className="btn btn-outline-dark" onClick={() => setSelected(null)}>Close</button>
    </section>}</>
}
