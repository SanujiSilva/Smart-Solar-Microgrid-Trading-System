import { useEffect, useState } from 'react'
import { NavLink, useLocation, useNavigate } from 'react-router-dom'
import { apiClient } from '../lib/api'
import { AccountsView, StationManagement, ReservationManagement } from './ManagementViews'

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

function ReservationTable({ items, empty }: { items: Reservation[]; empty: string }) { return <div className="table-wrap"><table className="table management-table"><thead><tr><th>Code</th><th>Prosumer</th><th>Scheduled</th><th>Energy</th><th>Status</th></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td><strong>{item.reservationCode}</strong></td><td>{item.prosumerNIC}</td><td>{formatDate(item.reservationDateTime)}</td><td>{item.energyAmount} kWh</td><td><StatusPill value={item.status} /></td></tr>)}</tbody></table>{items.length === 0 && <div className="empty-state">{empty}</div>}</div> }
function PageHeading({ eyebrow, title, copy }: { eyebrow: string; title: string; copy: string }) { return <div className="page-heading"><div className="eyebrow">{eyebrow}</div><h1>{title}</h1><p>{copy}</p></div> }

export function BackofficePage() {
  const location = useLocation(); const navigate = useNavigate(); const path = location.pathname
  const view = path.startsWith('/users') ? <AccountsView key="users" prosumers={false} /> : path.startsWith('/prosumers') ? <AccountsView key="prosumers" prosumers /> : path.startsWith('/stations') ? <StationManagement /> : path.startsWith('/reservations') ? <ReservationManagement /> : <DashboardView />
  return <>{view}<div className="backoffice-tabs"><NavLink to="/dashboard">Dashboard</NavLink><NavLink to="/users">Users</NavLink><NavLink to="/prosumers">Prosumers</NavLink><NavLink to="/stations">Stations</NavLink><NavLink to="/reservations">Reservations</NavLink><button onClick={() => navigate('/dashboard')}>Back to dashboard</button></div></>
}
