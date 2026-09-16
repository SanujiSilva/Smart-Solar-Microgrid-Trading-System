import { useEffect, useState, type FormEvent } from 'react'
import { NavLink, useLocation, useNavigate } from 'react-router-dom'
import { apiClient } from '../lib/api'
import { StaffCreateForm } from './StaffCreateForm'

type User = { id: string; nic: string | null; fullName: string; email: string; phone: string; role: string; status: string; createdAt: string }
type UserPage = { items: User[]; totalCount: number }
type Station = { id: string; stationCode: string; name: string; address: string; capacityKWh: number; availableBatterySlots: number; status: string; latitude: number; longitude: number }
type StationPage = { items: Station[]; totalCount: number }
type Slot = { id: string; startTime: string; endTime: string; capacity: number; availableCapacity: number; status: string }
type SlotPage = { items: Slot[] }
type Reservation = { id: string; reservationCode: string; prosumerNIC: string; stationId: string; slotId: string; energyAmount: number; reservationDateTime: string; status: string }
type SearchPage = { items: Reservation[]; totalCount: number }
type Dashboard = { pendingReservations: number; approvedFutureReservations: number; todayReservations: number; completedTransfers: number; activeStations: number; openSlots: number; availableSlotCapacity: number; recentReservations: Reservation[] }
type FormState = { name: string; address: string; latitude: string; longitude: string; capacityKWh: string; availableBatterySlots: string }

const emptyStation: FormState = { name: '', address: '', latitude: '', longitude: '', capacityKWh: '', availableBatterySlots: '' }

function ApiNotice({ message }: { message: string }) { return <div className="alert alert-danger compact-alert" role="alert">{message}</div> }
function Loading() { return <div className="loading-line">Loading live data...</div> }
function StatusPill({ value }: { value: string }) { return <span className={`status-pill status-pill--${value.toLowerCase()}`}>{value}</span> }
function formatDate(value: string) { return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) }

function DashboardView() {
  const [data, setData] = useState<Dashboard | null>(null)
  const [counts, setCounts] = useState({ users: 0, pendingProsumer: 0 })
  const [error, setError] = useState('')
  useEffect(() => {
    Promise.all([
      apiClient.get<Dashboard>('/reservations/dashboard'),
      apiClient.get<UserPage>('/users?pageSize=1'),
      apiClient.get<UserPage>('/prosumers?status=PENDING&pageSize=1'),
    ]).then(([dashboard, users, pending]) => {
      setData(dashboard.data)
      setCounts({ users: users.data.totalCount, pendingProsumer: pending.data.totalCount })
    }).catch(() => setError('Dashboard data could not be loaded.'))
  }, [])
  if (error) return <ApiNotice message={error} />
  if (!data) return <Loading />
  const cards = [['Users', counts.users], ['Pending prosumers', counts.pendingProsumer], ['Pending reservations', data.pendingReservations], ['Active stations', data.activeStations], ['Open slots', data.openSlots], ['Completed transfers', data.completedTransfers]]
  return <>
    <PageHeading eyebrow="Backoffice / overview" title="Control room" copy="A live view of the people, stations, slots, and reservations that keep the network moving." />
    <div className="metric-grid">{cards.map(([label, value]) => <article className="metric-card" key={label as string}><span>{label}</span><strong>{value}</strong></article>)}</div>
    <section className="data-panel">
      <div className="panel-heading"><div><div className="panel-label">Recent activity</div><h2>Latest reservations</h2></div><span className="capacity-note">{data.availableSlotCapacity} kWh open capacity</span></div>
      <ReservationTable items={data.recentReservations} empty="No reservations have been created yet." />
    </section>
  </>
}

function UsersView({ prosumers }: { prosumers: boolean }) {
  const [data, setData] = useState<UserPage | null>(null)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState(prosumers ? 'PENDING' : '')
  const [error, setError] = useState('')
  const load = () => {
    const path = prosumers ? '/prosumers' : '/users'
    apiClient.get<UserPage>(path, { params: { search: search || undefined, status: status || undefined, pageSize: 50 } }).then(({ data: result }) => setData(result)).catch(() => setError('Accounts could not be loaded.'))
  }
  useEffect(() => {
    const path = prosumers ? '/prosumers' : '/users'
    void apiClient.get<UserPage>(path, { params: { search: search || undefined, status: status || undefined, pageSize: 50 } }).then(({ data: result }) => setData(result)).catch(() => setError('Accounts could not be loaded.'))
  }, [prosumers, search, status])
  async function activate(nic: string) { try { await apiClient.patch(`/prosumers/${encodeURIComponent(nic)}/activate`); load() } catch { setError('The prosumer could not be activated.') } }
  async function changeStatus(id: string, next: string) { try { await apiClient.patch(`/users/${id}/status`, { status: next }); load() } catch { setError('The account status could not be updated.') } }
  return <>
    <PageHeading eyebrow={`Backoffice / ${prosumers ? 'prosumer review' : 'user management'}`} title={prosumers ? 'Prosumer accounts' : 'System users'} copy={prosumers ? 'Review pending registrations and reactivate deactivated prosumers.' : 'Manage Backoffice and Grid Operator access.'} />
    {!prosumers && <StaffCreateForm onCreated={load} />}
    <section className="data-panel">
      <div className="filter-row"><input className="form-control" placeholder="Search name, email, or NIC" value={search} onChange={(event) => setSearch(event.target.value)} /><select className="form-select" value={status} onChange={(event) => setStatus(event.target.value)}><option value="">All statuses</option><option>PENDING</option><option>ACTIVE</option><option>DEACTIVATION_REQUESTED</option><option>DEACTIVATED</option></select><button className="btn btn-dark" type="button" onClick={load}>Search</button></div>
      {error && <ApiNotice message={error} />}
      {!data ? <Loading /> : <div className="table-wrap"><table className="table management-table"><thead><tr><th>Name</th><th>{prosumers ? 'NIC' : 'Role'}</th><th>Contact</th><th>Status</th><th>Action</th></tr></thead><tbody>{data.items.map((item) => <tr key={item.id}><td><strong>{item.fullName}</strong><small>{item.email}</small></td><td>{prosumers ? item.nic : item.role}</td><td>{item.phone}</td><td><StatusPill value={item.status} /></td><td>{prosumers && item.status === 'DEACTIVATED' ? <button className="table-action" onClick={() => activate(item.nic ?? '')}>Reactivate</button> : !prosumers && item.status !== 'DEACTIVATED' ? <button className="table-action" onClick={() => changeStatus(item.id, 'DEACTIVATED')}>Deactivate</button> : !prosumers && item.status === 'DEACTIVATED' ? <button className="table-action" onClick={() => changeStatus(item.id, 'ACTIVE')}>Activate</button> : <span className="muted-cell">Review</span>}</td></tr>)}</tbody></table>{data.items.length === 0 && <div className="empty-state">No accounts match this filter.</div>}</div>}
    </section>
  </>
}

function StationsView() {
  const [data, setData] = useState<StationPage | null>(null)
  const [selected, setSelected] = useState<Station | null>(null)
  const [slots, setSlots] = useState<Slot[] | null>(null)
  const [form, setForm] = useState(emptyStation)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const load = () => apiClient.get<StationPage>('/stations', { params: { pageSize: 100 } }).then(({ data: result }) => setData(result)).catch(() => setError('Stations could not be loaded.'))
  useEffect(() => { void load() }, [])
  async function createStation(event: FormEvent) {
    event.preventDefault(); setError(''); setMessage('')
    try { await apiClient.post('/stations', { stationCode: `NODE-${Date.now().toString().slice(-6)}`, name: form.name, address: form.address, latitude: Number(form.latitude), longitude: Number(form.longitude), capacityKWh: Number(form.capacityKWh), availableBatterySlots: Number(form.availableBatterySlots), status: 'ACTIVE', operatingSchedule: { timeZoneId: 'Asia/Colombo', weeklyPeriods: [{ day: 'Monday', openMinuteOfDay: 0, closeMinuteOfDay: 1440 }] } }); setMessage('Station created.'); setForm(emptyStation); load() } catch { setError('Station creation failed. Check the form and API response.') }
  }
  async function viewSlots(station: Station) { setSelected(station); setSlots(null); try { const { data: result } = await apiClient.get<SlotPage>(`/stations/${station.id}/slots`); setSlots(result.items) } catch { setError('Slots could not be loaded.') } }
  return <>
    <PageHeading eyebrow="Backoffice / infrastructure" title="Microgrid nodes" copy="Create stations, inspect availability, and keep the network’s physical footprint current." />
    {error && <ApiNotice message={error} />}{message && <div className="alert alert-success compact-alert">{message}</div>}
    <div className="split-grid"><section className="data-panel"><div className="panel-heading"><div><div className="panel-label">Network</div><h2>Station directory</h2></div></div>{!data ? <Loading /> : <div className="table-wrap"><table className="table management-table"><thead><tr><th>Node</th><th>Capacity</th><th>Battery slots</th><th>Status</th><th></th></tr></thead><tbody>{data.items.map((station) => <tr key={station.id}><td><strong>{station.stationCode}</strong><small>{station.name}</small></td><td>{station.capacityKWh} kWh</td><td>{station.availableBatterySlots}</td><td><StatusPill value={station.status} /></td><td><button className="table-action" onClick={() => viewSlots(station)}>Slots</button></td></tr>)}</tbody></table></div>}</section>
    <form className="data-panel compact-form" onSubmit={createStation}><div className="panel-label">Add node</div><h2>New station</h2><label>Name<input className="form-control" required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></label><label>Address<input className="form-control" required value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} /></label><div className="form-two"><label>Latitude<input className="form-control" required type="number" step="any" value={form.latitude} onChange={(e) => setForm({ ...form, latitude: e.target.value })} /></label><label>Longitude<input className="form-control" required type="number" step="any" value={form.longitude} onChange={(e) => setForm({ ...form, longitude: e.target.value })} /></label></div><div className="form-two"><label>Capacity kWh<input className="form-control" required type="number" value={form.capacityKWh} onChange={(e) => setForm({ ...form, capacityKWh: e.target.value })} /></label><label>Battery slots<input className="form-control" required type="number" value={form.availableBatterySlots} onChange={(e) => setForm({ ...form, availableBatterySlots: e.target.value })} /></label></div><button className="btn btn-dark" type="submit">Create station</button></form></div>
    {selected && <section className="data-panel slot-drawer"><div className="panel-heading"><div><div className="panel-label">{selected.stationCode}</div><h2>Energy slots</h2></div><button className="table-action" onClick={() => setSelected(null)}>Close</button></div>{!slots ? <Loading /> : <SlotTable items={slots} />}</section>}
  </>
}

function SlotTable({ items }: { items: Slot[] }) { return <div className="table-wrap"><table className="table management-table"><thead><tr><th>Window</th><th>Capacity</th><th>Available</th><th>Status</th></tr></thead><tbody>{items.map((slot) => <tr key={slot.id}><td>{formatDate(slot.startTime)}<small>to {formatDate(slot.endTime)}</small></td><td>{slot.capacity} kWh</td><td>{slot.availableCapacity} kWh</td><td><StatusPill value={slot.status} /></td></tr>)}</tbody></table>{items.length === 0 && <div className="empty-state">No slots configured for this station.</div>}</div> }
function ReservationTable({ items, empty }: { items: Reservation[]; empty: string }) { return <div className="table-wrap"><table className="table management-table"><thead><tr><th>Code</th><th>Prosumer</th><th>Scheduled</th><th>Energy</th><th>Status</th></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td><strong>{item.reservationCode}</strong></td><td>{item.prosumerNIC}</td><td>{formatDate(item.reservationDateTime)}</td><td>{item.energyAmount} kWh</td><td><StatusPill value={item.status} /></td></tr>)}</tbody></table>{items.length === 0 && <div className="empty-state">{empty}</div>}</div> }
function ReservationsView() { const [data, setData] = useState<SearchPage | null>(null); const [status, setStatus] = useState(''); const [error, setError] = useState(''); const load = () => apiClient.get<SearchPage>('/reservations/search', { params: { status: status || undefined, pageSize: 100 } }).then(({ data: result }) => setData(result)).catch(() => setError('Reservations could not be loaded.')); useEffect(() => { void apiClient.get<SearchPage>('/reservations/search', { params: { status: status || undefined, pageSize: 100 } }).then(({ data: result }) => setData(result)).catch(() => setError('Reservations could not be loaded.')) }, [status]); return <><PageHeading eyebrow="Backoffice / operations" title="Reservations" copy="Search the live booking ledger and monitor the movement of energy across stations." /> <section className="data-panel"><div className="filter-row"><select className="form-select" value={status} onChange={(e) => setStatus(e.target.value)}><option value="">All statuses</option><option>PENDING</option><option>APPROVED</option><option>CANCELLED</option><option>COMPLETED</option><option>REJECTED</option></select><button className="btn btn-dark" onClick={load}>Refresh</button></div>{error && <ApiNotice message={error} />}{!data ? <Loading /> : <ReservationTable items={data.items} empty="No reservations match this filter." />}</section></> }
function PageHeading({ eyebrow, title, copy }: { eyebrow: string; title: string; copy: string }) { return <div className="page-heading"><div className="eyebrow">{eyebrow}</div><h1>{title}</h1><p>{copy}</p></div> }

export function BackofficePage() {
  const location = useLocation(); const navigate = useNavigate(); const path = location.pathname
  const view = path.startsWith('/users') ? <UsersView prosumers={false} /> : path.startsWith('/prosumers') ? <UsersView prosumers /> : path.startsWith('/stations') ? <StationsView /> : path.startsWith('/reservations') ? <ReservationsView /> : <DashboardView />
  return <>{view}<div className="backoffice-tabs"><NavLink to="/dashboard">Dashboard</NavLink><NavLink to="/users">Users</NavLink><NavLink to="/prosumers">Prosumers</NavLink><NavLink to="/stations">Stations</NavLink><NavLink to="/reservations">Reservations</NavLink><button onClick={() => navigate('/dashboard')}>Back to dashboard</button></div></>
}
