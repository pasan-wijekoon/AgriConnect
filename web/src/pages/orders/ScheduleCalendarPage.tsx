import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import { PageHeader } from '../../components/ui/PageHeader'
import { Card } from '../../components/ui/Card'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/StateViews'
import { ScheduleStatusBadge } from '../../components/orders/OrderStatusBadge'
import {
  ApiError,
  ordersApi,
  type CollectionCentreResponse,
  type ScheduleResponse,
} from '../../utils/ordersApi'
import './ScheduleCalendarPage.css'

function dateKey(iso: string): string {
  return new Date(iso).toISOString().slice(0, 10)
}

function timeLabel(iso: string): string {
  return new Date(iso).toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' })
}

function windowsOverlap(a: ScheduleResponse, b: ScheduleResponse): boolean {
  return new Date(a.slotStart) < new Date(b.slotEnd) && new Date(a.slotEnd) > new Date(b.slotStart)
}

/** Design.md §33 — Officer scheduling calendar: bookings grouped by day, a
 * capacity indicator per day, and overlapping Confirmed bookings visually
 * flagged as conflicts. */
export function ScheduleCalendarPage() {
  const { identity } = useAuth()
  const [centres, setCentres] = useState<CollectionCentreResponse[]>([])
  const [centresLoading, setCentresLoading] = useState(true)
  const [centresError, setCentresError] = useState<string | null>(null)
  const [centreId, setCentreId] = useState<string>('')
  const [schedules, setSchedules] = useState<ScheduleResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const loadCentres = () => {
    setCentresLoading(true)
    setCentresError(null)
    ordersApi
      .listCentres(identity)
      .then((result) => {
        setCentres(result)
        setCentreId((current) => (result.some((c) => c.id === current) ? current : (result[0]?.id ?? '')))
        setCentresLoading(false)
      })
      .catch((err: unknown) => {
        setCentresError(err instanceof ApiError ? err.message : 'Unable to load collection centres.')
        setCentresLoading(false)
      })
  }

  useEffect(loadCentres, [identity])

  const loadSchedules = () => {
    if (!centreId) return
    setLoading(true)
    setError(null)
    ordersApi
      .listCentreSchedules(identity, centreId)
      .then((result) => {
        setSchedules(result)
        setLoading(false)
      })
      .catch((err: unknown) => {
        setError(err instanceof ApiError ? err.message : 'Unable to load the schedule for this centre.')
        setLoading(false)
      })
  }

  useEffect(loadSchedules, [identity, centreId])

  const selectedCentre = centres.find((c) => c.id === centreId)

  const conflictIds = new Set<string>()
  const confirmed = schedules.filter((s) => s.status === 'Confirmed')
  for (let i = 0; i < confirmed.length; i++) {
    for (let j = i + 1; j < confirmed.length; j++) {
      if (windowsOverlap(confirmed[i], confirmed[j])) {
        conflictIds.add(confirmed[i].id)
        conflictIds.add(confirmed[j].id)
      }
    }
  }

  const byDate = new Map<string, ScheduleResponse[]>()
  for (const schedule of schedules) {
    const key = dateKey(schedule.slotStart)
    const list = byDate.get(key) ?? []
    list.push(schedule)
    byDate.set(key, list)
  }
  const sortedDates = [...byDate.keys()].sort()

  if (centresLoading) return <LoadingState label="Loading collection centres…" />
  if (centresError) return <ErrorState message={centresError} onRetry={loadCentres} />
  if (centres.length === 0) {
    return (
      <div>
        <PageHeader title="Schedule" description="Collection centre bookings calendar" />
        <Card>
          <EmptyState title="No Collection Centres Yet" description="Collection centres will appear here once they're added." />
        </Card>
      </div>
    )
  }

  return (
    <div>
      <PageHeader title="Schedule" description="Collection centre bookings calendar" />

      <div className="schedule-calendar-toolbar">
        <label>
          Collection Centre
          <select value={centreId} onChange={(e) => setCentreId(e.target.value)}>
            {centres.map((centre) => (
              <option key={centre.id} value={centre.id}>
                {centre.name}
              </option>
            ))}
          </select>
        </label>
        {selectedCentre && (
          <span className="schedule-calendar-capacity">Centre capacity: {selectedCentre.capacity} concurrent bookings</span>
        )}
      </div>

      <Card>
        {loading && <LoadingState label="Loading schedule…" />}
        {!loading && error && <ErrorState message={error} onRetry={loadSchedules} />}
        {!loading && !error && schedules.length === 0 && (
          <EmptyState title="No Bookings Yet" description="Proposed or confirmed schedules for this centre will appear here." />
        )}
        {!loading && !error && schedules.length > 0 && (
          <div className="schedule-calendar-days">
            {sortedDates.map((date) => (
              <div key={date} className="schedule-calendar-day">
                <div className="schedule-calendar-day-header">
                  {new Date(date).toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', month: 'short' })}
                </div>
                <div className="schedule-calendar-day-bookings">
                  {(byDate.get(date) ?? []).map((schedule) => (
                    <Link
                      key={schedule.id}
                      to={`/orders/${schedule.orderId}`}
                      className={`schedule-booking-card${conflictIds.has(schedule.id) ? ' conflict' : ''}`}
                    >
                      <span className="schedule-booking-time">
                        {timeLabel(schedule.slotStart)} – {timeLabel(schedule.slotEnd)}
                      </span>
                      <span className="schedule-booking-order">Order #{schedule.orderId.slice(0, 8)}</span>
                      <ScheduleStatusBadge status={schedule.status} />
                      {conflictIds.has(schedule.id) && <span className="schedule-booking-conflict-label">⚠ Conflict</span>}
                    </Link>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>
    </div>
  )
}
