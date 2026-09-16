import { useEffect, useState } from 'react'
import { useLocation } from 'react-router-dom'
import { apiClient } from '../lib/api'

type Station = { id: string; stationCode: string; name: string; address: string; capacityKWh: number; availableBatterySlots: number; status: string }
type StationPage = { items: Station[] }
type Slot = { id: string; startTime: string; endTime: string; capacity: number; availableCapacity: number; status: string }
type SlotPage = { items: Slot[] }
type Reservation = { id: string; reservationCode: string; prosumerNIC: string; stationId: string; slotId: string; energyAmount: number; reservationDateTime: string; status: string; completedAt?: string }
type ReservationList = { items: Reservation[] }
type Dashboard = { pendingReservations: number; approvedFutureReservations: number; todayReservations: number; completedTransfers: number; activeStations: number; openSlots: number; availableSlotCapacity: number; recentReservations: Reservation[] }

function formatDate(value: string) { return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) }
function StatusPill({ value }: { value: string }) { return <span className={`status-pill status-pill--${value.toLowerCase()}`}>{value}</span> }
function Loading() { return <div className="loading-line">Loading operational data...</div> }
function ErrorBox() { return <div className="alert alert-danger compact-alert">Operational data could not be loaded.</div> }
function Heading({ title, copy }: { title: string; copy: string }) { return <div className="page-heading"><div className="eyebrow">Grid Operator / operations</div><h1>{title}</h1><p>{copy}</p></div> }

function DashboardView() {
  const [data, setData] = useState<Dashboard | null>(null)
  const [error, setError] = useState(false)
  useEffect(() => { void apiClient.get<Dashboard>('/reservations/dashboard').then(({ data: result }) => setData(result)).catch(() => setError(true)) }, [])
  if (error) return <ErrorBox />
  if (!data) return <Loading />
  const metrics = [['Pending reservations', data.pendingReservations], ['Today', data.todayReservations], ['Completed transfers', data.completedTransfers], ['Active stations', data.activeStations], ['Open slots', data.openSlots], ['Open capacity', `${data.availableSlotCapacity} kWh`]]
  return <><Heading title="Operations desk" copy="Monitor the live network and keep today’s energy handoffs moving." /><div className="metric-grid operator-metrics">{metrics.map(([label, value]) => <article className="metric-card" key={label as string}><span>{label}</span><strong>{value}</strong></article>)}</div><section className="data-panel"><div className="panel-heading"><div><div className="panel-label">Queue</div><h2>Recent reservations</h2></div><span className="capacity-note">API live</span></div><ReservationTable items={data.recentReservations} /></section></>
}

function ReservationTable({ items }: { items: Reservation[] }) { return <div className="table-wrap"><table className="table management-table"><thead><tr><th>Code</th><th>Prosumer</th><th>Scheduled</th><th>Energy</th><th>Status</th></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td><strong>{item.reservationCode}</strong></td><td>{item.prosumerNIC}</td><td>{formatDate(item.reservationDateTime)}</td><td>{item.energyAmount} kWh</td><td><StatusPill value={item.status} /></td></tr>)}</tbody></table>{items.length === 0 && <div className="empty-state">No reservations are currently visible.</div>}</div> }

function ReservationsView() {
  const [data, setData] = useState<ReservationList | null>(null); const [status, setStatus] = useState(''); const [error, setError] = useState(false)
  useEffect(() => { void apiClient.get<ReservationList>('/reservations/search', { params: { status: status || undefined, pageSize: 100 } }).then(({ data: result }) => setData(result)).catch(() => setError(true)) }, [status])
  return <><Heading title="Reservation queue" copy="Review pending and approved bookings before they reach the transfer workflow." /><section className="data-panel"><div className="filter-row"><select className="form-select" value={status} onChange={(e) => setStatus(e.target.value)}><option value="">All reservations</option><option>PENDING</option><option>APPROVED</option><option>COMPLETED</option><option>CANCELLED</option></select></div>{error ? <ErrorBox /> : !data ? <Loading /> : <ReservationTable items={data.items} />}</section></>
}

function StationsView() {
  const [data, setData] = useState<StationPage | null>(null); const [selected, setSelected] = useState<Station | null>(null); const [slots, setSlots] = useState<Slot[] | null>(null); const [error, setError] = useState(false)
  useEffect(() => { void apiClient.get<StationPage>('/stations', { params: { status: 'ACTIVE', pageSize: 100 } }).then(({ data: result }) => setData(result)).catch(() => setError(true)) }, [])
  async function openSlots(station: Station) { setSelected(station); setSlots(null); try { const { data: result } = await apiClient.get<SlotPage>(`/stations/${station.id}/slots`); setSlots(result.items) } catch { setError(true) } }
  return <><Heading title="Station overview" copy="Inspect active nodes and the slot availability the grid can offer right now." />{error && <ErrorBox />}{!data ? <Loading /> : <section className="data-panel"><div className="table-wrap"><table className="table management-table"><thead><tr><th>Station</th><th>Address</th><th>Capacity</th><th>Battery slots</th><th>Status</th><th></th></tr></thead><tbody>{data.items.map((station) => <tr key={station.id}><td><strong>{station.stationCode}</strong><small>{station.name}</small></td><td>{station.address}</td><td>{station.capacityKWh} kWh</td><td>{station.availableBatterySlots}</td><td><StatusPill value={station.status} /></td><td><button className="table-action" onClick={() => openSlots(station)}>View slots</button></td></tr>)}</tbody></table>{data.items.length === 0 && <div className="empty-state">No active stations are available.</div>}</div></section>}{selected && <section className="data-panel"><div className="panel-heading"><div><div className="panel-label">{selected.stationCode}</div><h2>Slot availability</h2></div><button className="table-action" onClick={() => setSelected(null)}>Close</button></div>{!slots ? <Loading /> : <div className="table-wrap"><table className="table management-table"><thead><tr><th>Window</th><th>Capacity</th><th>Available</th><th>Status</th></tr></thead><tbody>{slots.map((slot) => <tr key={slot.id}><td>{formatDate(slot.startTime)}<small>to {formatDate(slot.endTime)}</small></td><td>{slot.capacity} kWh</td><td>{slot.availableCapacity} kWh</td><td><StatusPill value={slot.status} /></td></tr>)}</tbody></table></div>}</section>}</>
}

export function OperatorPage() {
  const path = useLocation().pathname
  const view = path.startsWith('/stations') ? <StationsView /> : path.startsWith('/reservations') ? <ReservationsView /> : <DashboardView />
  return <>{view}<div className="operator-note">QR scanning and transfer completion are intentionally reserved for Phase 21.</div></>
}
