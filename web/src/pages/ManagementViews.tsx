import { contactError, emailError, fullNameError, phoneError } from '../lib/contactValidation'
import { useCallback, useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react'
import { apiClient, apiError } from '../lib/api'
import { StaffCreateForm } from './StaffCreateForm'
import { ProsumerCreateForm } from './ProsumerCreateForm'
import { ReservationEditor } from './ReservationEditor'
import { useSearchParams } from 'react-router-dom'
import { SolarIcon, SolarLandscape } from '../components/SolarVisuals'

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
function FormModal({ title, onClose, children, wide = false }: { title: string; onClose: () => void; children: ReactNode; wide?: boolean }) {
  return <div className="form-modal-backdrop" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget) onClose() }}>
    <section className={`form-modal ${wide ? 'form-modal--wide' : ''}`} role="dialog" aria-modal="true" aria-label={title}>
      <button type="button" className="form-modal-close" onClick={onClose} aria-label={`Close ${title}`}>x</button>
      {children}
    </section>
  </div>
}
function Field({ label, value, change, type = 'text', disabled = false, error = '', errorId }: { label: string; value: string | number; change: (value: string) => void; type?: string; disabled?: boolean; error?: string; errorId?: string }) {
  return <label className="d-block mb-3">{label}<input className="form-control" required type={type} step={type === 'number' ? 'any' : undefined} value={value} disabled={disabled} onChange={e => change(e.target.value)} aria-invalid={!!error} aria-describedby={error ? errorId : undefined} />{error && <small id={errorId} className="text-danger">{error}</small>}</label>
}
const date = (value: string) => new Date(value).toLocaleString()
const localDate = (value: string) => { const d = new Date(value); return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16) }

export function AccountsView({ prosumers }: { prosumers: boolean }) {
  const [searchParams] = useSearchParams()
  const [page, setPage] = useState(1), [search, setSearch] = useState(searchParams.get('search') ?? ''), [status, setStatus] = useState(searchParams.get('status') === 'PENDING' ? 'PENDING' : '')
  const [role, setRole] = useState(''), [showStaff, setShowStaff] = useState(false)
  const [showProsumer, setShowProsumer] = useState(false)
  const endpoint = prosumers ? '/prosumers' : '/users'
  const { data, error, reload: reloadList } = useData<Page<User>>(endpoint, { page, pageSize: 20, search, status: status || undefined, role: !prosumers && role ? role : undefined })
  const total = useData<Page<User>>(endpoint, { pageSize: 1 })
  const active = useData<Page<User>>(endpoint, { pageSize: 1, status: 'ACTIVE' })
  const inactive = useData<Page<User>>(endpoint, { pageSize: 1, status: 'DEACTIVATED' })
  function reload() { reloadList(); total.reload(); active.reload(); inactive.reload() }
  function exportPage() {
    if (!data) return
    const cell = (value: string | null) => '"' + (value ?? '').replace(/^[=+@\-\t\r]/, c => "'" + c).replaceAll('"', '""') + '"'
    const rows = [['Name', 'NIC', 'Role', 'Email', 'Phone', 'Status'], ...data.items.map(u => [u.fullName, u.nic, u.role, u.email, u.phone, u.status])]
    const url = URL.createObjectURL(new Blob(['\uFEFF' + rows.map(row => row.map(cell).join(',')).join('\r\n')], { type: 'text/csv;charset=utf-8' }))
    const link = document.createElement('a'); link.href = url; link.download = `users-page-${page}.csv`; link.click(); setTimeout(() => URL.revokeObjectURL(url), 1000)
  }
  const [selected, setSelected] = useState<User | null>(null), [actionError, setActionError] = useState(''), [message, setMessage] = useState(''), [busy, setBusy] = useState(false)
  const selectedErrors = selected ? {
    fullName: selected.fullName ? fullNameError(selected.fullName) : '',
    email: selected.email ? emailError(selected.email) : '',
    phone: selected.phone ? phoneError(selected.phone) : '',
  } : null
  async function act(task: () => Promise<unknown>, success: string) {
    setBusy(true); setActionError(''); setMessage('')
    try { await task(); setMessage(success); reload() } catch (e) { setActionError(apiError(e)) } finally { setBusy(false) }
  }
  async function open(user: User) {
    await act(async () => { const result = await apiClient.get<User>(prosumers ? `/prosumers/${encodeURIComponent(user.nic!)}` : `/users/${user.id}`); setSelected(result.data); setShowProsumer(false) }, '')
  }
  function changeStatus(user: User, status: string) {
    if (!window.confirm(`${status === 'ACTIVE' ? 'Activate' : 'Deactivate'} ${user.fullName}?`)) return
    void act(() => user.role === 'PROSUMER' && user.status === 'DEACTIVATED'
      ? apiClient.patch(`/prosumers/${encodeURIComponent(user.nic!)}/activate`)
      : apiClient.patch(`/users/${user.id}/status`, { status }), 'Account status updated.')
  }
  return <div className="accounts-layout"><div className="accounts-main">
    <div className="accounts-heading"><div><div className="eyebrow">Backoffice <span>/</span> {prosumers ? 'Prosumers' : 'Users'}</div><h1>{prosumers ? 'Prosumer accounts' : 'User management'}</h1><p>Create staff accounts, manage access, and monitor account status.</p></div><div className="accounts-heading-actions">{prosumers && <button className="btn btn-dark" onClick={() => { setSelected(null); setShowProsumer(true) }}>Add prosumer</button>}{!prosumers && <button className="btn btn-dark" onClick={() => setShowStaff(true)}>＋ Add staff member</button>}<button className="btn btn-outline-primary" disabled={!data?.items.length} onClick={exportPage}>Export this page</button></div></div>
    <div className="account-metrics">{[['Total users', total, 'Users'], ['Active users', active, 'Account'], ['Deactivated users', inactive, 'Account']].map(([label, result, icon], i) => {
      const metric = result as typeof total
      return <article className={`account-metric account-metric--${i}`} key={label as string}><span className="account-metric-icon"><SolarIcon name={icon as string} /></span><div><span>{label as string}</span><strong>{metric.data?.totalCount ?? (metric.error ? 'Unavailable' : '…')}</strong></div></article>
    })}</div>
    <section className="data-panel">
      <h2 className="accounts-table-title">All {prosumers ? 'prosumers' : 'users'}</h2>
      <div className="filter-row"><input className="form-control" aria-label="Search accounts" placeholder="Search name, email or NIC" value={search} onChange={e => { setSearch(e.target.value); setPage(1) }} /><select className="form-select" aria-label="Account status" value={status} onChange={e => { setStatus(e.target.value); setPage(1) }}><option value="">All statuses</option>{['PENDING','ACTIVE','DEACTIVATION_REQUESTED','DEACTIVATED'].map(s => <option key={s}>{s}</option>)}</select>{!prosumers && <select className="form-select" aria-label="Account role" value={role} onChange={e => { setRole(e.target.value); setPage(1) }}><option value="">All roles</option>{['BACKOFFICE', 'GRID_OPERATOR', 'PROSUMER'].map(r => <option key={r}>{r}</option>)}</select>}<button className="btn btn-outline-primary" onClick={reload} aria-label="Refresh"><span aria-hidden="true">↻ </span>Refresh</button></div>
      <Notice error={error || actionError} message={message} />
      {!data && !error && <p role="status">Loading accounts...</p>}
      {data && <><div className="table-responsive"><table className="table accounts-table"><thead><tr><th>User</th><th>NIC / role</th><th>Contact</th><th>Status</th><th>Actions</th></tr></thead><tbody>{data.items.map(u => <tr key={u.id}><td><div className="account-identity"><span className="account-avatar" aria-hidden="true">{u.fullName.slice(0, 1).toUpperCase()}</span><strong>{u.fullName}</strong></div></td><td>{u.role}<small>{u.nic || '—'}</small></td><td>{u.email}<small>{u.phone}</small></td><td><span className={`status-pill status-pill--${u.status.toLowerCase()}`}>{u.status}</span></td><td><div className="account-row-actions"><button className="btn btn-sm btn-outline-dark" disabled={busy} onClick={() => void open(u)}>Details / Edit</button>{u.status === 'PENDING' || u.status === 'DEACTIVATED' ? <button className="btn btn-sm btn-success" disabled={busy} onClick={() => changeStatus(u, 'ACTIVE')}>{u.status === 'PENDING' ? 'Approve account' : 'Reactivate'}</button> : (u.status === 'DEACTIVATION_REQUESTED' || (u.role !== 'PROSUMER' && u.status === 'ACTIVE')) ? <button className="btn btn-sm btn-outline-danger" disabled={busy} onClick={() => changeStatus(u, 'DEACTIVATED')}>{u.status === 'DEACTIVATION_REQUESTED' ? 'Approve deactivation' : 'Deactivate'}</button> : null}</div></td></tr>)}</tbody></table></div>{data.items.length === 0 && <p className="empty-state">No accounts match these filters.</p>}<div className="accounts-pagination"><span>Showing {data.totalCount === 0 ? 0 : (page - 1) * 20 + 1}–{Math.min(page * 20, data.totalCount)} of {data.totalCount} users</span><Pager page={page} total={data.totalCount} change={setPage} /></div></>}
    </section>
    {showProsumer && <FormModal title="Create prosumer account" onClose={() => setShowProsumer(false)}><ProsumerCreateForm onCreated={reload} onClose={() => setShowProsumer(false)} /></FormModal>}
    {selected && <FormModal title="Edit account details" onClose={() => setSelected(null)}><form className="data-panel modal-form-panel" onSubmit={e => { e.preventDefault(); const validation = contactError(selected); if (validation) { setActionError(validation); return } void act(async () => { const result = await apiClient.put<User>(selected.role === 'PROSUMER' ? `/prosumers/${encodeURIComponent(selected.nic!)}` : `/users/${selected.id}`, { fullName: selected.fullName, email: selected.email, phone: selected.phone }); setSelected(result.data) }, 'User saved.') }}>
      <h2>Account details</h2><p>{selected.nic || selected.id} · {selected.role} · {selected.status}</p>
      <fieldset disabled={busy}>{(['fullName','email','phone'] as const).map(k => <Field key={k} label={{ fullName: 'Full name', email: 'Email', phone: 'Phone' }[k]} value={selected[k]} change={value => setSelected({ ...selected, [k]: value })} error={selectedErrors?.[k] ?? ''} errorId={`account-${k}-error`} />)}<button className="btn btn-dark">Save changes</button></fieldset>
      {selected.role === 'PROSUMER' && <p>NIC is the permanent account key and cannot be edited.</p>}<button type="button" className="btn btn-outline-dark" onClick={() => setSelected(null)}>Close</button>
    </form></FormModal>}
    {!prosumers && showStaff && <FormModal title="Create staff account" onClose={() => setShowStaff(false)}><StaffCreateForm onCreated={reload} onClose={() => setShowStaff(false)} /></FormModal>}
  </div></div>
}

const emptyStation: Station = { id: '', stationCode: '', name: '', address: '', latitude: 0, longitude: 0, capacityKWh: 1, availableBatterySlots: 1, status: 'ACTIVE', operatingSchedule: { timeZoneId: 'Asia/Colombo', weeklyPeriods: [] } }
const days = ['Monday','Tuesday','Wednesday','Thursday','Friday','Saturday','Sunday']
const clock = (minutes: number) => `${String(Math.floor(minutes / 60)).padStart(2, '0')}:${String(minutes % 60).padStart(2, '0')}`
const minutes = (text: string) => { const [h, m] = text.split(':').map(Number); return h * 60 + m }
const stationCodePreview = (number: number) => `SOLAR${String(Math.max(1, number)).padStart(3, '0')}`
const stationSnapshot = (station: Station) => JSON.stringify({
  name: station.name,
  address: station.address,
  latitude: Number(station.latitude),
  longitude: Number(station.longitude),
  capacityKWh: Number(station.capacityKWh),
  availableBatterySlots: Number(station.availableBatterySlots),
  status: station.status,
  operatingSchedule: station.operatingSchedule,
})
let googleMapsLoad: Promise<void> | null = null
function loadGoogleMaps() {
  const apiKey = import.meta.env.VITE_GOOGLE_MAPS_API_KEY as string | undefined
  if (!apiKey) return Promise.reject(new Error('Add VITE_GOOGLE_MAPS_API_KEY to web/.env.local to enable the station location map.'))
  const global = window as typeof window & { google?: unknown; initStationLocationMap?: () => void }
  if (global.google) return Promise.resolve()
  if (googleMapsLoad) return googleMapsLoad
  googleMapsLoad = new Promise((resolve, reject) => {
    global.initStationLocationMap = () => resolve()
    const script = document.createElement('script')
    script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(apiKey)}&callback=initStationLocationMap`
    script.async = true
    script.defer = true
    script.onerror = () => reject(new Error('Google Maps could not be loaded. Check the API key and network connection.'))
    document.head.appendChild(script)
  })
  return googleMapsLoad
}
function StationLocationMap({ address, latitude, longitude, onPick }: { address: string; latitude: number; longitude: number; onPick: (latitude: number, longitude: number, address?: string) => void }) {
  const mapRef = useRef<HTMLDivElement | null>(null)
  const mapObjectRef = useRef<any>(null)
  const markerRef = useRef<any>(null)
  const geocoderRef = useRef<any>(null)
  const lastGeocodedAddressRef = useRef('')
  const [error, setError] = useState('')
  useEffect(() => {
    let cancelled = false
    loadGoogleMaps().then(() => {
      if (cancelled || !mapRef.current) return
      const google = (window as typeof window & { google: any }).google
      const center = { lat: Number(latitude) || 6.9357, lng: Number(longitude) || 79.9843 }
      geocoderRef.current ??= new google.maps.Geocoder()
      const reversePick = (lat: number, lng: number) => {
        markerRef.current?.setPosition({ lat, lng })
        geocoderRef.current.geocode({ location: { lat, lng } }, (results: Array<{ formatted_address: string }> | null, status: string) => {
          const formattedAddress = status === 'OK' && results?.[0]?.formatted_address ? results[0].formatted_address : undefined
          if (formattedAddress) lastGeocodedAddressRef.current = formattedAddress
          onPick(lat, lng, formattedAddress)
        })
      }
      if (!mapObjectRef.current) {
        mapObjectRef.current = new google.maps.Map(mapRef.current, { center, zoom: 14, mapTypeControl: false, streetViewControl: false, fullscreenControl: false })
        markerRef.current = new google.maps.Marker({ map: mapObjectRef.current, position: center, draggable: true })
        mapObjectRef.current.addListener('click', (event: { latLng: { lat: () => number; lng: () => number } }) => {
          reversePick(Number(event.latLng.lat().toFixed(6)), Number(event.latLng.lng().toFixed(6)))
        })
        markerRef.current.addListener('dragend', (event: { latLng: { lat: () => number; lng: () => number } }) => {
          reversePick(Number(event.latLng.lat().toFixed(6)), Number(event.latLng.lng().toFixed(6)))
        })
      } else {
        mapObjectRef.current.setCenter(center)
        markerRef.current?.setPosition(center)
      }
      setError('')
    }).catch(e => { if (!cancelled) setError(e instanceof Error ? e.message : 'Google Maps could not be loaded.') })
    return () => { cancelled = true }
  }, [latitude, longitude, onPick])
  useEffect(() => {
    const query = address.trim()
    if (query.length < 3 || query === lastGeocodedAddressRef.current) return
    const timer = window.setTimeout(() => {
      loadGoogleMaps().then(() => {
        const google = (window as typeof window & { google: any }).google
        geocoderRef.current ??= new google.maps.Geocoder()
        geocoderRef.current.geocode({ address: query }, (results: Array<{ formatted_address: string; geometry: { location: { lat: () => number; lng: () => number } } }> | null, status: string) => {
          if (status !== 'OK' || !results?.[0]) return
          const location = results[0].geometry.location
          const next = { lat: Number(location.lat().toFixed(6)), lng: Number(location.lng().toFixed(6)) }
          mapObjectRef.current?.setCenter(next)
          markerRef.current?.setPosition(next)
          lastGeocodedAddressRef.current = query
          onPick(next.lat, next.lng)
        })
      }).catch(() => undefined)
    }, 700)
    return () => window.clearTimeout(timer)
  }, [address, onPick])
  return <div className="station-location-picker">
    <div className="station-location-picker__header"><strong>Select station location</strong><span>Type an address to search, or click the map to fill address and coordinates.</span></div>
    <div ref={mapRef} className="station-location-picker__map" role="application" aria-label="Google Map for station location" />
    {error && <p className="station-location-picker__fallback" role="status">{error}</p>}
  </div>
}

export function StationManagement({ operator = false }: { operator?: boolean }) {
  const [searchParams] = useSearchParams()
  const [page, setPage] = useState(1), [search, setSearch] = useState(searchParams.get('search') ?? ''), [status, setStatus] = useState('')
  const { data, error, reload } = useData<Page<Station>>('/stations', { page, pageSize: 20, search, status: status || undefined })
  const [overviewId, setOverviewId] = useState<string | null>(null)
  const [selected, setSelected] = useState<Station | null>(null), [form, setForm] = useState<Station | null>(null), [busy, setBusy] = useState(false), [actionError, setActionError] = useState(''), [message, setMessage] = useState('')
  async function act(task: () => Promise<unknown>, success: string) {
    setBusy(true); setActionError(''); setMessage('')
    try { await task(); reload(); setMessage(success) } catch (e) { setActionError(apiError(e)) } finally { setBusy(false) }
  }
  async function open(station: Station) {
    await act(async () => { const result = await apiClient.get<Station>(`/stations/${station.id}`); setSelected(result.data); setForm(result.data) }, '')
  }
  async function selectStation(station: Station) {
    setOverviewId(station.id)
    await act(async () => {
      const result = await apiClient.get<Station>(`/stations/${station.id}`)
      setSelected(result.data)
      setTimeout(() => document.getElementById('station-slots-panel')?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 50)
    }, '')
  }
  async function save(e: FormEvent) {
    e.preventDefault(); if (!form) return
    if (form.id && selected) {
      const changed = operator ? Number(form.availableBatterySlots) !== Number(selected.availableBatterySlots) : stationSnapshot(form) !== stationSnapshot(selected)
      if (!changed) { setActionError(''); setMessage('No changes to save.'); setForm(null); return }
    }
    await act(async () => {
      if (operator) { const result = await apiClient.patch<Station>(`/stations/${form.id}/availability`, { availableBatterySlots: Number(form.availableBatterySlots) }); setSelected(result.data); setForm(null); return }
      const body = { name: form.name, address: form.address, latitude: Number(form.latitude), longitude: Number(form.longitude), capacityKWh: Number(form.capacityKWh), availableBatterySlots: Number(form.availableBatterySlots), status: form.status, operatingSchedule: form.operatingSchedule }
      const result = form.id ? await apiClient.put<Station>(`/stations/${form.id}`, body) : await apiClient.post<Station>('/stations', body)
      setSelected(result.data); setForm(null)
    }, form.id ? 'Station details modified successfully.' : 'Station saved successfully.')
  }
  const overview = data?.items.find(s => s.id === overviewId) ?? data?.items[0]
  const pageCapacity = data?.items.reduce((sum, s) => sum + s.capacityKWh, 0)
  const pageSlots = data?.items.reduce((sum, s) => sum + s.availableBatterySlots, 0)
  const pickStationLocation = useCallback((latitude: number, longitude: number, address?: string) => setForm(current => current ? { ...current, latitude, longitude, address: address ?? current.address } : current), [])
  function exportStations() {
    if (!data) return
    const cell = (v: string | number) => '"' + String(v).replace(/^[=+@\-\t\r]/, c => "'" + c).replaceAll('"', '""') + '"'
    const rows = [['Code', 'Name', 'Address', 'Capacity kWh', 'Available battery slots', 'Status'], ...data.items.map(s => [s.stationCode, s.name, s.address, s.capacityKWh, s.availableBatterySlots, s.status])]
    const url = URL.createObjectURL(new Blob(['\uFEFF' + rows.map(row => row.map(cell).join(',')).join('\r\n')], { type: 'text/csv;charset=utf-8' }))
    const link = document.createElement('a'); link.href = url; link.download = `stations-page-${page}.csv`; link.click(); setTimeout(() => URL.revokeObjectURL(url), 1000)
  }
  return <div className="stations-page"><div className="accounts-heading"><div><div className="eyebrow">{operator ? 'Grid operator' : 'Backoffice'} <span>/</span> Stations</div><h1>{operator ? 'Station overview and availability' : 'Microgrid node management'}</h1><p>Monitor station capacity, battery slots, and operational status.</p></div><div className="accounts-heading-actions"><button className="btn btn-outline-primary" disabled={!data?.items.length} onClick={exportStations}>Export this page</button>{!operator && <button className="btn btn-dark" onClick={() => { setSelected(null); setForm(structuredClone(emptyStation)) }}>＋ Add station</button>}</div></div>
    <div className="station-metrics">{[['Matching stations', data?.totalCount, 'Stations'], ['Active on this page', data?.items.filter(s => s.status === 'ACTIVE').length, 'Energy'], ['Capacity on this page', pageCapacity === undefined ? undefined : `${pageCapacity} kWh`, 'Stations'], ['Available battery slots on this page', pageSlots, 'Transfers']].map(([label, value, icon], i) => <article className={`station-metric station-metric--${i % 2}`} key={String(label)}><span className="account-metric-icon"><SolarIcon name={String(icon)} /></span><div><span>{label}</span><strong>{value ?? (error ? 'Unavailable' : '…')}</strong></div></article>)}</div>
    <div className="station-browser"><section className="data-panel station-list-panel"><h2>All stations</h2><p className="station-help">Search and manage microgrid stations in your network.</p><div className="filter-row"><input className="form-control" aria-label="Search stations" placeholder="Search stations" value={search} onChange={e => { setSearch(e.target.value); setPage(1) }} /><select className="form-select" aria-label="Station status filter" value={status} onChange={e => { setStatus(e.target.value); setPage(1) }}><option value="">All statuses</option>{['ACTIVE', 'INACTIVE', 'MAINTENANCE', 'DEACTIVATED'].map(s => <option key={s}>{s}</option>)}</select><button className="btn btn-outline-primary" onClick={reload}>Refresh</button></div><Notice error={error || actionError} message={message} />
    {!data && !error && <p role="status" className="loading-line">Loading stations...</p>}{data && <><div className="station-card-list">{data.items.map(s => <article className={`station-node-card ${overview?.id === s.id ? 'is-selected' : ''}`} key={s.id}><button className="station-card-identity" onClick={() => void selectStation(s)} aria-label={`Select ${s.name} and show available slots`} aria-pressed={selected?.id === s.id}><SolarLandscape /><span><strong>{s.stationCode}</strong><span>{s.name}</span><small>{s.address}</small></span></button><div className="station-card-facts"><div><SolarIcon name="Stations" /><span>Capacity<strong>{s.capacityKWh} kWh</strong></span></div><div><SolarIcon name="Transfers" /><span>Available slots<strong>{s.availableBatterySlots}</strong></span></div><div><span>Status<strong><span className={`status-pill status-pill--${s.status.toLowerCase()}`}>{s.status}</span></strong></span></div></div><button className="btn btn-outline-primary" disabled={busy} onClick={() => { setOverviewId(s.id); void open(s) }}>Details / Manage</button></article>)}</div>{data.items.length === 0 && <p className="empty-state">No stations match these filters.</p>}<Pager page={page} total={data.totalCount} change={setPage} /></>}</section>
    <aside className="data-panel station-overview" aria-label="Station overview"><h2>Station overview</h2><p className="station-help">Key information for the selected station.</p>{overview ? <><SolarLandscape /><h3>{overview.name}</h3><p>{overview.stationCode}</p><span className={`status-pill status-pill--${overview.status.toLowerCase()}`}>{overview.status}</span><dl><div><SolarIcon name="Stations" /><dt>Capacity</dt><dd>{overview.capacityKWh} kWh</dd></div><div><SolarIcon name="Transfers" /><dt>Available battery slots</dt><dd>{overview.availableBatterySlots}</dd></div></dl><div className="station-address">{overview.address}</div></> : <p className="empty-state">{error ? 'Station information is unavailable.' : data ? 'No station selected.' : 'Loading station information…'}</p>}</aside></div>
    {form && <FormModal title={form.id ? 'Edit station' : 'New station'} onClose={() => { setForm(null); setSelected(null) }} wide><form className="data-panel modal-form-panel" onSubmit={save}><h2>{form.id ? form.name : 'New station'}</h2><Notice error={actionError} message={message} /><fieldset disabled={busy}>
      <fieldset disabled={operator}><Field label="Station code" value={form.id ? form.stationCode : stationCodePreview((data?.totalCount ?? 0) + 1)} disabled change={() => undefined} /><Field label="Name" value={form.name} change={v => setForm({ ...form, name: v })} /><Field label="Address" value={form.address} change={v => setForm({ ...form, address: v })} /><Field label="Latitude" type="number" value={form.latitude} change={v => setForm({ ...form, latitude: Number(v) })} /><Field label="Longitude" type="number" value={form.longitude} change={v => setForm({ ...form, longitude: Number(v) })} />{!operator && <StationLocationMap address={form.address} latitude={Number(form.latitude)} longitude={Number(form.longitude)} onPick={pickStationLocation} />}<Field label="Capacity (kWh)" type="number" value={form.capacityKWh} change={v => setForm({ ...form, capacityKWh: Number(v) })} />
      <label>Status<select className="form-select mb-3" value={form.status} onChange={e => setForm({ ...form, status: e.target.value })}>{['ACTIVE','INACTIVE','MAINTENANCE', ...(form.status === 'DEACTIVATED' ? ['DEACTIVATED'] : [])].map(s => <option key={s}>{s}</option>)}</select></label>
      <h3>Operating schedule</h3><Field label="Time zone" value={form.operatingSchedule.timeZoneId} change={v => setForm({ ...form, operatingSchedule: { ...form.operatingSchedule, timeZoneId: v } })} />
      {form.operatingSchedule.weeklyPeriods.map((p, i) => <div className="row mb-2" key={i}><label className="col">Day<select className="form-select" value={p.day} onChange={e => setForm({ ...form, operatingSchedule: { ...form.operatingSchedule, weeklyPeriods: form.operatingSchedule.weeklyPeriods.map((x, n) => n === i ? { ...x, day: e.target.value } : x) } })}>{days.map(d => <option key={d}>{d}</option>)}</select></label>{(['openMinuteOfDay','closeMinuteOfDay'] as const).map(k => <label className="col" key={k}>{k === 'openMinuteOfDay' ? 'Opens' : 'Closes (24:00 = midnight)'}<input className="form-control" required pattern="[0-2][0-9]:[0-5][0-9]" defaultValue={clock(p[k])} onBlur={e => setForm({ ...form, operatingSchedule: { ...form.operatingSchedule, weeklyPeriods: form.operatingSchedule.weeklyPeriods.map((x, n) => n === i ? { ...x, [k]: minutes(e.target.value) } : x) } })} /></label>)}<button type="button" className="btn btn-outline-danger col-auto" onClick={() => setForm({ ...form, operatingSchedule: { ...form.operatingSchedule, weeklyPeriods: form.operatingSchedule.weeklyPeriods.filter((_, n) => n !== i) } })}>Remove period</button></div>)}
      <button type="button" className="btn btn-outline-dark mb-3" onClick={() => setForm({ ...form, operatingSchedule: { ...form.operatingSchedule, weeklyPeriods: [...form.operatingSchedule.weeklyPeriods, { day: 'Monday', openMinuteOfDay: 480, closeMinuteOfDay: 1020 }] } })}>Add operating period</button></fieldset>
      <Field label="Available battery slots" type="number" value={form.availableBatterySlots} change={v => setForm({ ...form, availableBatterySlots: Number(v) })} />
      <button className="btn btn-dark">Save {operator ? 'availability' : 'station'}</button>
      {!operator && form.id && form.status !== 'DEACTIVATED' && <button type="button" className="btn btn-outline-danger" onClick={() => { if (window.confirm('Deactivate this station?')) void act(async () => { const result = await apiClient.patch<Station>(`/stations/${form.id}/deactivate`); setForm(result.data); setSelected(result.data) }, 'Station deactivated.') }}>Deactivate</button>}
      {!operator && form.id && <button type="button" className="btn btn-outline-danger" onClick={() => { if (window.confirm('Permanently delete this station? Only stations without slots or reservation history can be deleted.')) void act(async () => { await apiClient.delete(`/stations/${form.id}`); setForm(null); setSelected(null); setOverviewId(null) }, 'Station deleted.') }}>Delete station</button>}
      <button type="button" className="btn btn-outline-dark" onClick={() => { setForm(null); setSelected(null) }}>Close</button></fieldset></form></FormModal>}
      {selected && <SlotManagement key={selected.id} station={selected} operator={operator} />}
    </div>
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
  return <section id="station-slots-panel" className="data-panel station-slots-panel"><h2>Available slots · {station.name}</h2><p className="station-help">Review this station’s slots at the end of the page and add new slots when needed.</p><Notice error={error || actionError} message={message} /><button className="btn btn-outline-dark" onClick={reload}>Refresh slots</button>{!operator && <button className="btn btn-dark" onClick={() => setForm({ id: '', startTime: '', endTime: '', capacity: 1, availableCapacity: 1, status: 'OPEN' })}>Add new slot</button>}
    {!data && !error && <p>Loading slots...</p>}{data && <div className="table-responsive"><table className="table"><thead><tr><th>Window</th><th>Capacity</th><th>Available</th><th>Status</th><th /></tr></thead><tbody>{data.items.map(s => <tr key={s.id}><td>{date(s.startTime)} – {date(s.endTime)}</td><td>{s.capacity}</td><td>{s.availableCapacity}</td><td>{s.status}</td><td><button className="btn btn-sm btn-outline-dark" disabled={busy || s.status === 'CANCELLED'} onClick={() => setForm({ ...s, startTime: localDate(s.startTime), endTime: localDate(s.endTime) })}>Edit {operator ? 'availability' : 'slot'}</button>{!operator && <button className="btn btn-sm btn-outline-danger" disabled={busy || s.status === 'CANCELLED'} onClick={() => { if (window.confirm('Cancel this slot?')) void act(() => apiClient.delete(`/slots/${s.id}`)) }}>Cancel slot</button>}</td></tr>)}</tbody></table>{data.items.length === 0 && <p>No slots configured.</p>}</div>}
    {form && <FormModal title={form.id ? 'Edit slot' : 'New slot'} onClose={() => setForm(null)}><form className="data-panel modal-form-panel" onSubmit={save}><h2>{form.id ? 'Edit slot' : 'New slot'}</h2><fieldset disabled={busy}><fieldset disabled={operator}><Field label="Start (local time)" type="datetime-local" value={form.startTime} change={v => setForm({ ...form, startTime: v })} /><Field label="End (local time)" type="datetime-local" value={form.endTime} change={v => setForm({ ...form, endTime: v })} /><Field label="Capacity (kWh)" type="number" value={form.capacity} change={v => setForm({ ...form, capacity: Number(v), availableCapacity: form.id ? form.availableCapacity : Number(v) })} /></fieldset><Field label="Available capacity (kWh)" type="number" value={form.availableCapacity} change={v => setForm({ ...form, availableCapacity: Number(v) })} /><label>Status<select className="form-select mb-3" value={form.status} onChange={e => setForm({ ...form, status: e.target.value })}><option>OPEN</option><option>CLOSED</option></select></label><button className="btn btn-dark">Save slot</button><button type="button" className="btn btn-outline-dark" onClick={() => setForm(null)}>Close editor</button></fieldset></form></FormModal>}
    </section>
}

export function ReservationManagement({ operator = false }: { operator?: boolean }) {
  const [searchParams] = useSearchParams()
  const [page, setPage] = useState(1), [filters, setFilters] = useState({ reservationCode: searchParams.get('search') ?? '', stationId: '', status: searchParams.get('status') === 'PENDING' ? 'PENDING' : '', from: '', to: '' })
  const params = { ...filters, page, pageSize: 20, status: filters.status || undefined, from: filters.from ? new Date(filters.from).toISOString() : undefined, to: filters.to ? new Date(filters.to).toISOString() : undefined }
  const { data, error, reload } = useData<Page<Reservation>>('/reservations/search', params)
  const stationChoices = useData<Page<Station>>('/stations', { page: 1, pageSize: 100 })
  const [selected, setSelected] = useState<Reservation | null>(null), [busy, setBusy] = useState(false), [actionError, setActionError] = useState(''), [message, setMessage] = useState('')
  const [editor, setEditor] = useState<Reservation | 'new' | null>(null)
  const selectedStation = selected ? stationChoices.data?.items.find(station => station.id === selected.stationId) : undefined
  async function act(task: () => Promise<unknown>, message: string) { setBusy(true); setActionError(''); setMessage(''); try { await task(); reload(); setMessage(message) } catch (e) { setActionError(apiError(e)) } finally { setBusy(false) } }
  const filter = (key: keyof typeof filters, value: string) => { setFilters({ ...filters, [key]: value }); setPage(1) }
  function change(action: string) {
    if (!selected || !window.confirm(`${action} reservation ${selected.reservationCode}?`)) return
    void act(async () => { const result = action === 'Cancel' ? await apiClient.delete<Reservation>(`/reservations/${selected.id}`) : await apiClient.patch<Reservation>(`/reservations/${selected.id}/${action.toLowerCase()}`); setSelected(result.data) }, 'Reservation updated.')
  }
  function exportReservations() {
    if (!data) return
    const cell = (v: string | number) => '"' + String(v).replace(/^[=+@\-\t\r]/, c => "'" + c).replaceAll('"', '""') + '"'
    const rows = [['Reservation', 'Prosumer NIC', 'Scheduled', 'Energy kWh', 'Status'], ...data.items.map(r => [r.reservationCode, r.prosumerNIC, r.reservationDateTime, r.energyAmount, r.status])]
    const url = URL.createObjectURL(new Blob(['\uFEFF' + rows.map(row => row.map(cell).join(',')).join('\r\n')], { type: 'text/csv;charset=utf-8' }))
    const link = document.createElement('a'); link.href = url; link.download = `reservations-page-${page}.csv`; link.click(); setTimeout(() => URL.revokeObjectURL(url), 1000)
  }
  return <div className="reservations-page"><div className="accounts-heading"><div><div className="eyebrow">{operator ? 'Grid operator' : 'Backoffice'} <span>/</span> Reservations</div><h1>Reservation management</h1><p>Track bookings, energy transfers, schedules, and reservation status.</p></div><button className="btn btn-dark" disabled={busy} onClick={() => setEditor('new')}>New reservation</button><button className="btn btn-outline-primary" disabled={!data?.items.length} onClick={exportReservations}>Export this page</button></div>
    {editor && <FormModal title={editor === 'new' ? 'New reservation' : 'Edit reservation'} onClose={() => setEditor(null)} wide><ReservationEditor key={editor === 'new' ? 'new' : editor.id} booking={editor === 'new' ? undefined : editor} onClose={() => setEditor(null)} onSaved={() => { setEditor(null); setSelected(null); reload(); setMessage('Reservation saved. Changes require Backoffice approval.') }} /></FormModal>}
    <div className="reservation-metrics">{[['Matching reservations', data?.totalCount, 'Reservations'], ['Pending on this page', data?.items.filter(r => r.status === 'PENDING').length, 'Review'], ['Completed on this page', data?.items.filter(r => r.status === 'COMPLETED').length, 'Transfers'], ['Booked energy on this page', data ? `${data.items.filter(r => !['CANCELLED', 'REJECTED'].includes(r.status)).reduce((sum, r) => sum + r.energyAmount, 0)} kWh` : undefined, 'Energy']].map(([label, value, icon], i) => <article className={`reservation-metric reservation-metric--${i}`} key={String(label)}><span className="account-metric-icon"><SolarIcon name={String(icon)} /></span><div><span>{label}</span><strong>{value ?? (error ? 'Unavailable' : '…')}</strong></div></article>)}</div>
    <div className="reservation-workspace"><section className="data-panel reservation-list"><h2>All reservations</h2><div className="reservation-filters"><div className="reservation-filter-fields"><label>Reservation code<input className="form-control" placeholder="Search reservation code" type="text" value={filters.reservationCode} onChange={e => filter('reservationCode', e.target.value)} /></label><label>Station<select className="form-select" value={filters.stationId} onChange={e => filter('stationId', e.target.value)}><option value="">All stations</option>{stationChoices.data?.items.map(station => <option key={station.id} value={station.id}>{station.name} ({station.stationCode})</option>)}</select></label><label>From (local time)<input className="form-control" type="datetime-local" value={filters.from} onChange={e => filter('from', e.target.value)} /></label><label>Until (exclusive, local time)<input className="form-control" type="datetime-local" value={filters.to} onChange={e => filter('to', e.target.value)} /></label></div><div className="reservation-filter-actions"><label>Status<select className="form-select" value={filters.status} onChange={e => filter('status', e.target.value)}><option value="">All statuses</option>{['PENDING','APPROVED','CANCELLED','COMPLETED','REJECTED'].map(s => <option key={s}>{s}</option>)}</select></label><button className="btn btn-outline-dark" onClick={() => { setFilters({ reservationCode: '', stationId: '', from: '', to: '', status: '' }); setPage(1) }}>Clear filters</button><button className="btn btn-outline-primary" onClick={() => { reload(); stationChoices.reload() }}>Refresh</button></div></div><Notice error={error || actionError || stationChoices.error} message={message} />
    {!data && !error && <p className="loading-line" role="status">Loading reservations...</p>}{data && <><div className="table-responsive"><table className="table reservation-table"><thead><tr><th>Reservation</th><th>Prosumer</th><th>Scheduled</th><th>Energy</th><th>Status</th><th>Actions</th></tr></thead><tbody>{data.items.map(r => <tr key={r.id} className={selected?.id === r.id ? 'is-selected' : ''}><td><div className="reservation-code"><SolarIcon name="Reservations" /><strong>{r.reservationCode}</strong></div></td><td>{r.prosumerNIC}</td><td>{date(r.reservationDateTime)}</td><td>{r.energyAmount} kWh</td><td><span className={`status-pill status-pill--${r.status.toLowerCase()}`}>{r.status}</span></td><td><button className="btn btn-sm btn-outline-primary" disabled={busy} onClick={() => void act(async () => { const result = await apiClient.get<Reservation>(`/reservations/${r.id}`); setSelected(result.data) }, '')}>Details</button></td></tr>)}</tbody></table></div>{data.items.length === 0 && <p className="empty-state">No reservations match these filters.</p>}<div className="reservation-pagination"><Pager page={page} total={data.totalCount} change={setPage} /></div></>}</section>
    <aside className="data-panel reservation-details" aria-label="Reservation details"><div className="reservation-details-heading"><h2>Reservation details</h2>{selected && <button className="panel-close" aria-label="Close reservation details" onClick={() => setSelected(null)}>×</button>}</div>{selected ? <><div className="reservation-detail-identity"><SolarIcon name="Reservations" /><div><h3>{selected.reservationCode}</h3><span className={`status-pill status-pill--${selected.status.toLowerCase()}`}>{selected.status}</span></div></div><dl><dt>Prosumer NIC</dt><dd>{selected.prosumerNIC}</dd><dt>Station name</dt><dd>{selectedStation ? `${selectedStation.name} (${selectedStation.stationCode})` : 'Loading station...'}</dd><dt>Station ID</dt><dd>{selected.stationId}</dd><dt>Slot ID</dt><dd>{selected.slotId}</dd><dt>Scheduled</dt><dd>{date(selected.reservationDateTime)}</dd><dt>Energy</dt><dd>{selected.energyAmount} kWh</dd>{selected.completedByOperatorId && <><dt>Completed by</dt><dd>{selected.completedByOperatorId}</dd></>}</dl>
      <div className="reservation-history"><h3>Recorded activity</h3><ol><li><strong>Requested</strong><time>{date(selected.createdAt)}</time></li><li><strong>Scheduled transfer</strong><time>{date(selected.reservationDateTime)}</time></li>{selected.completedAt && <li className="is-complete"><strong>Completed</strong><time>{date(selected.completedAt)}</time></li>}</ol><p>Current status: {selected.status.toLowerCase().replaceAll('_', ' ')}</p></div><div className="reservation-detail-actions">
      {!operator && selected.status === 'PENDING' && <><button className="btn btn-success" disabled={busy} onClick={() => change('Approve')}>Approve</button><button className="btn btn-outline-danger" disabled={busy} onClick={() => change('Reject')}>Reject</button></>}
      {['PENDING','APPROVED'].includes(selected.status) && <button className="btn btn-dark" disabled={busy} onClick={() => setEditor(selected)}>Edit reservation</button>}
      {['PENDING','APPROVED'].includes(selected.status) && <button className="btn btn-outline-danger" disabled={busy} onClick={() => change('Cancel')}>Cancel reservation</button>}<button className="btn btn-outline-dark" onClick={() => setSelected(null)}>Close</button>
    </div></> : <div className="reservation-detail-empty"><SolarIcon name="Reservations" /><p>Select a reservation’s Details button to review its schedule, status, and available actions.</p></div>}</aside></div></div>
}
