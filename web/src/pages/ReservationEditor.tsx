import { useEffect, useState, type FormEvent } from 'react'
import { apiClient, apiError } from '../lib/api'
import { nicError } from '../lib/contactValidation'

type Booking = { id: string; prosumerNIC: string; stationId: string; slotId: string; energyAmount: number; reservationDateTime: string }
type Station = { id: string; name: string; stationCode: string }
type Slot = { id: string; startTime: string; endTime: string; availableCapacity: number; status: string }

export function ReservationEditor({ booking, onSaved, onClose }: { booking?: Booking; onSaved: () => void; onClose: () => void }) {
  const [nic, setNic] = useState(booking?.prosumerNIC ?? '')
  const [stationId, setStationId] = useState(booking?.stationId ?? '')
  const [slotId, setSlotId] = useState(booking?.slotId ?? '')
  const [amount, setAmount] = useState(String(booking?.energyAmount ?? ''))
  const [stations, setStations] = useState<Station[]>([])
  const [slotResult, setSlotResult] = useState<{ key: string; items: Slot[]; error: string } | null>(null)
  const [stationError, setStationError] = useState('')
  const [error, setError] = useState(''), [busy, setBusy] = useState(false)
  const [version] = useState(0)
  const liveNicError = !booking && nic ? nicError(nic) : ''
  const slotKey = `${stationId}:${version}`
  const loadingSlots = !!stationId && slotResult?.key !== slotKey
  const slots = slotResult?.key === slotKey ? slotResult.items : []
  const slotError = slotResult?.key === slotKey ? slotResult.error : ''
  useEffect(() => {
    const controller = new AbortController()
    apiClient.get<{ items: Station[] }>('/stations', { params: { page: 1, pageSize: 100, status: 'ACTIVE' }, signal: controller.signal })
      .then(({ data }) => { if (!controller.signal.aborted) { setStations(data.items); setStationError('') } })
      .catch(e => { if (!controller.signal.aborted) setStationError(apiError(e)) })
    return () => controller.abort()
  }, [version])
  useEffect(() => {
    if (!stationId) return
    const controller = new AbortController()
    apiClient.get<{ items: Slot[] }>(`/stations/${stationId}/slots`, { signal: controller.signal })
      .then(({ data }) => { if (!controller.signal.aborted) setSlotResult({ key: slotKey, items: data.items, error: '' }) })
      .catch(e => { if (!controller.signal.aborted) setSlotResult({ key: slotKey, items: [], error: apiError(e) }) })
    return () => controller.abort()
  }, [stationId, slotKey])
  async function submit(event: FormEvent) {
    event.preventDefault(); setError('')
    if (!booking) {
      const nicValidation = nicError(nic)
      if (nicValidation) { setError(nicValidation); return }
    }
    setBusy(true)
    try {
      const body = { slotId, energyAmount: Number(amount) }
      if (booking) await apiClient.put(`/reservations/${booking.id}`, body)
      else await apiClient.post('/reservations', { ...body, prosumerNIC: nic.trim().toUpperCase() })
      onSaved()
    } catch (e) { setError(apiError(e)) } finally { setBusy(false) }
  }
  return <form className="data-panel" onSubmit={submit} aria-label="Reservation editor">
    <h2>{booking ? 'Edit reservation' : 'New reservation'}</h2>
    <p>Choose an active prosumer and a scheduled energy slot. Changes to energy or schedule require Backoffice approval again.</p>
    {(error || stationError || slotError) && <div className="alert alert-danger" role="alert">{error || stationError || slotError}</div>}
    <fieldset disabled={busy}>
      <label className="d-block mb-3">Prosumer NIC<input className="form-control" required disabled={!!booking} pattern="(?:[0-9]{9}[vVxX]|[0-9]{12})" value={nic} onChange={e => setNic(e.target.value)} aria-invalid={!!liveNicError} aria-describedby="reservation-nic-error" />{liveNicError && <small id="reservation-nic-error" className="text-danger">{liveNicError}</small>}</label>
      <label className="d-block mb-3">Booking station<select className="form-select" required value={stationId} onChange={e => { setStationId(e.target.value); setSlotId('') }}>
        <option value="">Choose station</option>
        {stationId && !stations.some(s => s.id === stationId) && <option value={stationId}>Selected station ({stationId})</option>}
        {stations.map(s => <option key={s.id} value={s.id}>{s.name} ({s.stationCode})</option>)}
      </select></label>
      <label className="d-block mb-3">Scheduled slot<select className="form-select" required value={slotId} disabled={!stationId || loadingSlots} onChange={e => setSlotId(e.target.value)}>
        <option value="">{loadingSlots ? 'Loading slots...' : 'Choose slot'}</option>
        {booking && stationId === booking.stationId && !slots.some(s => s.id === booking.slotId) && <option value={booking.slotId}>Current: {new Date(booking.reservationDateTime).toLocaleString()}</option>}
        {slots.map(s => <option key={s.id} value={s.id} disabled={s.status !== 'OPEN' && s.id !== booking?.slotId}>{new Date(s.startTime).toLocaleString()} - {new Date(s.endTime).toLocaleTimeString()} - {s.availableCapacity} kWh available - {s.status}</option>)}
      </select></label>
      <label className="d-block mb-3">Reservation energy (kWh)<input className="form-control" required type="number" min="0.001" step="any" value={amount} onChange={e => setAmount(e.target.value)} /></label>
      <button className="btn btn-dark" disabled={loadingSlots || !slotId || !!slotError} type="submit">{busy ? 'Saving...' : 'Save reservation'}</button>{' '}
      <button className="btn btn-outline-dark" type="button" onClick={onClose}>Close editor</button>
    </fieldset>
  </form>
}
