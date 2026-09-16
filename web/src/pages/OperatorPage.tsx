import { useEffect, useState } from 'react'
import { useLocation } from 'react-router-dom'
import { apiClient } from '../lib/api'
import { StationManagement, ReservationManagement } from './ManagementViews'

type Reservation = { id: string; reservationCode: string; prosumerNIC: string; stationId: string; slotId: string; energyAmount: number; reservationDateTime: string; status: string; completedAt?: string }
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

export function OperatorPage() {
  const path = useLocation().pathname
  const view = path.startsWith('/stations') ? <StationManagement operator /> : path.startsWith('/reservations') ? <ReservationManagement operator /> : <DashboardView />
  return <>{view}</>
}
