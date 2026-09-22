import { useEffect, useState } from 'react'
import { NavLink, useLocation, useNavigate } from 'react-router-dom'
import { apiClient } from '../lib/api'
import { AccountsView, StationManagement, ReservationManagement } from './ManagementViews'
import { SolarIcon, SolarLandscape } from '../components/SolarVisuals'

type User = { id: string; nic: string | null; fullName: string; email: string; phone: string; role: string; status: string; createdAt: string }
type UserPage = { items: User[]; totalCount: number }
type Reservation = { id: string; reservationCode: string; prosumerNIC: string; stationId: string; slotId: string; energyAmount: number; reservationDateTime: string; status: string }
type Dashboard = { pendingReservations: number; approvedFutureReservations: number; todayReservations: number; completedTransfers: number; activeStations: number; openSlots: number; availableSlotCapacity: number; recentReservations: Reservation[] }


function ApiNotice({ message }: { message: string }) { return <div className="alert alert-danger compact-alert" role="alert">{message}</div> }
function Loading() { return <div className="loading-line">Loading live data...</div> }
function StatusPill({ value }: { value: string }) { return <span className={`status-pill status-pill--${value.toLowerCase()}`}>{value}</span> }
function formatDate(value: string) { return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) }

function DashboardView() {
  const [data, setData] = useState<Dashboard | null>(null)
  const [counts, setCounts] = useState({ users: 0, pendingProsumer: 0 })
  const [error, setError] = useState('')
  const [version, setVersion] = useState(0)
  const [syncedAt, setSyncedAt] = useState<Date | null>(null)
  const [health, setHealth] = useState({ api: 'Checking API', database: 'Checking database' })
  const [search, setSearch] = useState('')
  const [period, setPeriod] = useState('recent')
  function refresh() {
    setError('')
    setHealth({ api: 'Checking API', database: 'Checking database' })
    setVersion(v => v + 1)
  }
  useEffect(() => {
    const controller = new AbortController()
    for (const [key, path, ok, failed] of [
      ['api', '/health', 'API online', 'API unavailable'],
      ['database', '/health/ready', 'Database connected', 'Database unavailable'],
    ] as const) {
      void apiClient.get<{ status: string }>(path, { signal: controller.signal }).then(response => {
        if (!controller.signal.aborted) setHealth(previous => ({ ...previous, [key]: response.data.status === 'Healthy' ? ok : failed }))
      }).catch(() => { if (!controller.signal.aborted) setHealth(previous => ({ ...previous, [key]: failed })) })
    }
    Promise.all([
      apiClient.get<Dashboard>('/reservations/dashboard', { signal: controller.signal }),
      apiClient.get<UserPage>('/users?pageSize=1', { signal: controller.signal }),
      apiClient.get<UserPage>('/prosumers?status=PENDING&pageSize=1', { signal: controller.signal }),
    ]).then(([dashboard, users, pending]) => {
      if (controller.signal.aborted) return
      setData(dashboard.data)
      setSyncedAt(new Date())
      setCounts({ users: users.data.totalCount, pendingProsumer: pending.data.totalCount })
    }).catch(() => { if (!controller.signal.aborted) setError('Dashboard data could not be loaded.') })
    return () => controller.abort()
  }, [version])
  if (error) return <div className="control-room"><ApiNotice message={error} /><button className="btn btn-outline-primary" onClick={refresh}>Retry</button></div>
  if (!data) return <Loading />
  const cards = [['Users', counts.users], ['Pending prosumers', counts.pendingProsumer], ['Pending reservations', data.pendingReservations], ['Active stations', data.activeStations], ['Open slots', data.openSlots], ['Completed transfers', data.completedTransfers]]
  const icons = ['Users', 'Account', 'Reservations', 'Stations', 'Reservations', 'Transfers']
  const recent = data.recentReservations.filter(item => item.reservationCode.toLowerCase().includes(search.toLowerCase()) && (period !== 'today' || new Date(item.reservationDateTime).toDateString() === new Date().toDateString()))
  function exportReport() {
    if (!data) return
    const rows = [['Metric', 'Value'], ...cards.map(([label, value]) => [String(label), String(value)]), ['Open capacity (kWh)', String(data.availableSlotCapacity)], ['Snapshot captured', syncedAt?.toISOString() ?? '']]
    const url = URL.createObjectURL(new Blob(['\uFEFF' + rows.map(row => row.map(value => `"${value.replaceAll('"', '""')}"`).join(',')).join('\r\n')], { type: 'text/csv;charset=utf-8' }))
    const link = document.createElement('a'); link.href = url; link.download = 'microgrid-overview.csv'; link.click(); setTimeout(() => URL.revokeObjectURL(url), 1000)
  }
  return <div className="control-room overview-dashboard">
    <div className="dashboard-hero">
      <PageHeading eyebrow="Backoffice / overview" title="Control room" copy="A live view of the people, stations, slots, and reservations that keep the network moving." />
      <div className="overview-heading-actions"><NavLink className="review-button" to="/reservations?status=PENDING"><SolarIcon name="Review" />Review pending<SolarIcon name="Arrow" /></NavLink><button className="btn btn-outline-primary" onClick={exportReport}>Export report</button><button className="btn btn-outline-primary" onClick={refresh}>Refresh overview</button></div>
      <div className="hero-scene"><span>Clean energy<br />stronger together</span><SolarLandscape /></div>
    </div>
    <div className="metric-grid">{cards.map(([label, value], index) => <article className={`metric-card metric-card--${index === 0 ? 'purple' : index < 3 ? 'amber' : 'green'}`} key={label as string}><SolarIcon name={icons[index]} /><span>{label}</span><strong>{value}</strong></article>)}</div>
    <div className="overview-panels">
      <section className="overview-panel"><h2><SolarIcon name="Energy" />Energy overview</h2><div className="overview-energy"><SolarIcon name="Energy" /><strong>{data.availableSlotCapacity} kWh</strong><span>open capacity</span></div><p>Across {data.openSlots} open {data.openSlots === 1 ? 'slot' : 'slots'}</p><NavLink to="/stations">Explore station capacity <SolarIcon name="Arrow" /></NavLink></section>
      <section className="overview-panel"><h2><SolarIcon name="Stations" />Network status</h2><div className="overview-network"><div><SolarIcon name="Stations" /><span>Active stations<strong>{data.activeStations}</strong></span></div><div><SolarIcon name="Reservations" /><span>Open slots<strong>{data.openSlots}</strong></span></div></div></section>
      <section className="overview-panel overview-quick"><h2><SolarIcon name="Energy" />Quick actions</h2><NavLink to="/prosumers?status=PENDING"><SolarIcon name="Users" />Review prosumers<SolarIcon name="Arrow" /></NavLink><NavLink to="/stations"><SolarIcon name="Stations" />Manage stations<SolarIcon name="Arrow" /></NavLink><NavLink to="/reservations"><SolarIcon name="Reservations" />View reservations<SolarIcon name="Arrow" /></NavLink></section>
    </div>
    <div className="overview-health" aria-live="polite"><span className={health.api === 'API online' ? 'is-healthy' : 'is-unknown'}>{health.api}</span><span className={health.database === 'Database connected' ? 'is-healthy' : 'is-unknown'}>{health.database}</span><span className="is-healthy">Snapshot captured {syncedAt?.toLocaleTimeString()}</span></div>
    <section className="data-panel">
      <div className="panel-heading"><h2><SolarIcon name="Reservations" />Latest reservations</h2><div className="overview-reservation-filters"><input className="form-control" aria-label="Search recent reservations" placeholder="Search reservation code…" value={search} onChange={e => setSearch(e.target.value)} /><select className="form-select" aria-label="Recent reservation period" value={period} onChange={e => setPeriod(e.target.value)}><option value="recent">All recent</option><option value="today">Scheduled today</option></select><NavLink className="btn btn-outline-primary" to="/reservations">All filters</NavLink></div></div>
      <ReservationTable items={recent} empty={data.recentReservations.length ? 'No recent reservations match these filters.' : 'No reservations have been created yet.'} />
    </section>
  </div>
}

function ReservationTable({ items, empty }: { items: Reservation[]; empty: string }) { return <div className="table-wrap"><table className="table management-table"><thead><tr><th>Code</th><th>Prosumer</th><th>Scheduled</th><th>Energy</th><th>Status</th></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td><strong>{item.reservationCode}</strong></td><td>{item.prosumerNIC}</td><td>{formatDate(item.reservationDateTime)}</td><td>{item.energyAmount} kWh</td><td><StatusPill value={item.status} /></td></tr>)}</tbody></table>{items.length === 0 && <div className="empty-state">{empty}</div>}</div> }
function PageHeading({ eyebrow, title, copy }: { eyebrow: string; title: string; copy: string }) { return <div className="page-heading"><div className="eyebrow">{eyebrow}</div><h1>{title}</h1><p>{copy}</p></div> }

export function BackofficePage() {
  const location = useLocation(); const navigate = useNavigate(); const path = location.pathname
  const view = path.startsWith('/users') ? <AccountsView key={`users${location.search}`} prosumers={false} /> : path.startsWith('/prosumers') ? <AccountsView key={`prosumers${location.search}`} prosumers /> : path.startsWith('/stations') ? <StationManagement key={location.search} /> : path.startsWith('/reservations') ? <ReservationManagement key={location.search} /> : <DashboardView />
  return <>{view}<div className="backoffice-tabs"><NavLink to="/dashboard">Dashboard</NavLink><NavLink to="/users">Users</NavLink><NavLink to="/prosumers">Prosumers</NavLink><NavLink to="/stations">Stations</NavLink><NavLink to="/reservations">Reservations</NavLink><button onClick={() => navigate('/dashboard')}>Back to dashboard</button></div></>
}
