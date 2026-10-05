import { useState, type FormEvent } from 'react'

import { Card, ErrorNotice, Field, Icon, PageHeader } from '../components/ui.tsx'
import { getFilters, previewSchedule, type SchedulingPreview } from '../utils/api.ts'
import { formatDay } from '../utils/format.ts'
import { ApiError } from '../utils/problem.ts'
import {
  SCENARIOS,
  centreDateTime,
  timelinePercent,
  toIso,
  tomorrowAtCentre,
  validateRequest,
  weekday,
  type BookingInput,
  type Scenario,
} from '../utils/scheduling.ts'
import { useAsync } from '../utils/useAsync.ts'

let nextId = 0
const newBooking = (start = '', end = ''): BookingInput => ({ id: `b${++nextId}`, start, end })

interface Result {
  preview: SchedulingPreview
  /** The request the preview answers, so the timeline stays truthful if the form changes. */
  requestedDate: string
}

/**
 * Component D — lets an officer try the Logistics Scheduling Agent directly. The agent
 * runs server-side (the browser never calls it); nothing here is saved.
 */
export function AiSchedulingPage() {
  const filters = useAsync(getFilters, 'filters')
  const regions = filters.data?.regions ?? []

  const [centreId, setCentreId] = useState('')
  const [date, setDate] = useState(tomorrowAtCentre)
  const [start, setStart] = useState('08:00')
  const [end, setEnd] = useState('17:00')
  const [bookings, setBookings] = useState<BookingInput[]>([])
  const [scenario, setScenario] = useState<Scenario | null>(null)

  const [asking, setAsking] = useState(false)
  const [result, setResult] = useState<Result | null>(null)
  const [error, setError] = useState<Error | null>(null)

  const centre = centreId || (regions[0] ? centreCode(regions[0].name) : 'CC-DAMBULLA')
  const problems = validateRequest(date, start, end, bookings)

  const applyScenario = (s: Scenario) => {
    setScenario(s)
    setStart(s.start)
    setEnd(s.end)
    setBookings(s.bookings.map(([bs, be]) => newBooking(bs, be)))
    setResult(null)
    setError(null)
  }

  const updateBooking = (id: string, field: 'start' | 'end', value: string) => {
    setScenario(null)
    setBookings((list) => list.map((b) => (b.id === id ? { ...b, [field]: value } : b)))
  }

  const ask = async (e: FormEvent) => {
    e.preventDefault()
    if (problems.length) return
    setAsking(true)
    setError(null)
    setResult(null)
    try {
      const preview = await previewSchedule({
        centreId: centre,
        preferredWindow: { start: toIso(date, start), end: toIso(date, end) },
        existingBookings: bookings.map((b) => ({ slotStart: toIso(date, b.start), slotEnd: toIso(date, b.end) })),
      })
      setResult({ preview, requestedDate: date })
    } catch (err) {
      setError(err as Error)
    } finally {
      setAsking(false)
    }
  }

  const proposed = result && {
    start: centreDateTime(result.preview.proposedSlotStart),
    end: centreDateTime(result.preview.proposedSlotEnd),
  }
  const movedDay = proposed && proposed.start.date !== result.requestedDate

  return (
    <div className="stack">
      <PageHeader
        title="AI scheduling"
        description="Ask the Logistics Scheduling Agent for the earliest conflict-free pickup slot. Preview only: nothing is saved."
      />

      <div className="scenario-block">
      <div className="scenario-row" role="group" aria-label="Example scenarios">
        <span className="muted small">Try a scenario:</span>
        {SCENARIOS.map((s) => (
          <button
            key={s.id}
            type="button"
            className={`btn btn-small${scenario?.id === s.id ? ' btn-primary' : ''}`}
            aria-pressed={scenario?.id === s.id}
            onClick={() => applyScenario(s)}
          >
            {s.label}
          </button>
        ))}
      </div>
      {scenario && <p className="muted scenario-note">{scenario.description}</p>}
      </div>

      <div className="scheduling-grid">
        <Card title="Request">
          <form className="scheduling-form" onSubmit={ask} noValidate>
            <div className="filters">
              <Field label="Collection centre">
                <select value={centre} onChange={(e) => setCentreId(e.target.value)}>
                  {regions.length === 0 && <option value="CC-DAMBULLA">Dambulla</option>}
                  {regions.map((r) => (
                    <option key={r.id} value={centreCode(r.name)}>
                      {r.name}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label="Date">
                <input type="date" value={date} onChange={(e) => setDate(e.target.value)} required />
              </Field>
            </div>

            <fieldset className="window-fields">
              <legend>Preferred window (centre time)</legend>
              <div className="filters">
                <Field label="From">
                  <input type="time" value={start} onChange={(e) => { setScenario(null); setStart(e.target.value) }} required />
                </Field>
                <Field label="To">
                  <input type="time" value={end} onChange={(e) => { setScenario(null); setEnd(e.target.value) }} required />
                </Field>
              </div>
            </fieldset>

            <fieldset className="window-fields">
              <legend>Existing bookings that day</legend>
              {bookings.length === 0 && <p className="muted small">None. The agent still checks the centre's own calendar.</p>}
              <ul className="booking-list">
                {bookings.map((b, i) => (
                  <li key={b.id}>
                    <span className="muted small booking-index">{i + 1}</span>
                    <input type="time" aria-label={`Booking ${i + 1} start`} value={b.start} onChange={(e) => updateBooking(b.id, 'start', e.target.value)} />
                    <span className="muted">to</span>
                    <input type="time" aria-label={`Booking ${i + 1} end`} value={b.end} onChange={(e) => updateBooking(b.id, 'end', e.target.value)} />
                    <button
                      type="button"
                      className="btn btn-quiet btn-icon"
                      aria-label={`Remove booking ${i + 1}`}
                      onClick={() => { setScenario(null); setBookings((list) => list.filter((x) => x.id !== b.id)) }}
                    >
                      <Icon name="close" />
                    </button>
                  </li>
                ))}
              </ul>
              <button type="button" className="btn btn-small" onClick={() => { setScenario(null); setBookings((list) => [...list, newBooking()]) }}>
                Add booking
              </button>
            </fieldset>

            {problems.length > 0 && (
              <ul className="field-error" role="alert">
                {problems.map((p) => <li key={p}>{p}</li>)}
              </ul>
            )}
            <div>
              <button type="submit" className="btn btn-primary" disabled={asking || problems.length > 0}>
                <Icon name="spark" />
                {asking ? 'Asking the agent…' : 'Find a slot'}
              </button>
            </div>
          </form>
        </Card>

        <div className="stack">
          <Card title={`${weekday(date)} ${formatDay(date)}`}>
            <DayTimeline
              start={start}
              end={end}
              bookings={bookings}
              proposed={proposed && !movedDay ? { start: proposed.start.time, end: proposed.end.time } : null}
            />
          </Card>

          {error && <SchedulingError error={error} />}

          {result && proposed && (
            <Card title="Agent's proposal" className="card-success">
              <div className="proposal">
                <div className="proposal-slot">
                  {weekday(proposed.start.date)} {formatDay(proposed.start.date)}, {proposed.start.time}–{proposed.end.time}
                </div>
                {result.preview.conflictChecked && (
                  <span className="badge badge-reviewed"><Icon name="check" /> Conflict-checked</span>
                )}
              </div>
              {movedDay && (
                <p className="notice">
                  <Icon name="alert" />
                  <span>Moved to {weekday(proposed.start.date)} {formatDay(proposed.start.date)}: the requested day had no room.</span>
                </p>
              )}
              <div>
                <h3 className="small muted">Agent's reasoning</h3>
                <p className="reasoning">{result.preview.reasoning}</p>
              </div>
              <p className="footnote muted">
                In the real flow this slot is saved as Proposed on the order and waits for an officer's approval.
              </p>
            </Card>
          )}
        </div>
      </div>
    </div>
  )
}

/** Demo centre codes from region names, e.g. "Nuwara Eliya" → "CC-NUWARA-ELIYA". */
function centreCode(regionName: string): string {
  return `CC-${regionName.toUpperCase().replace(/[^A-Z0-9]+/g, '-')}`
}

function SchedulingError({ error }: { error: Error }) {
  if (error instanceof ApiError && error.status === 409) {
    return (
      <Card title="No free slot">
        <p>{error.message}</p>
        <p className="muted small">The agent looks at the requested day and the next three. Try a wider window or fewer bookings.</p>
      </Card>
    )
  }
  return <ErrorNotice error={error} />
}

function DayTimeline({ start, end, bookings, proposed }: {
  start: string
  end: string
  bookings: BookingInput[]
  proposed: { start: string; end: string } | null
}) {
  const hours = [6, 8, 10, 12, 14, 16, 18, 20]
  const span = (s: string, e: string) => ({ left: `${timelinePercent(s)}%`, width: `${Math.max(0, timelinePercent(e) - timelinePercent(s))}%` })
  const valid = (b: { start: string; end: string }) => b.start && b.end && b.end > b.start

  const summary = [
    valid({ start, end }) ? `Preferred window ${start} to ${end}` : null,
    ...bookings.filter(valid).map((b, i) => `booking ${i + 1} ${b.start} to ${b.end}`),
    proposed ? `proposed slot ${proposed.start} to ${proposed.end}` : null,
  ].filter(Boolean).join('; ')

  return (
    <div className="timeline" role="img" aria-label={summary || 'Empty day'}>
      <div className="timeline-track">
        {valid({ start, end }) && <div className="timeline-window" style={span(start, end)} />}
        {bookings.filter(valid).map((b) => (
          <div key={b.id} className="timeline-booking" style={span(b.start, b.end)} title={`Booked ${b.start}–${b.end}`} />
        ))}
        {proposed && (
          <div className="timeline-proposed" style={span(proposed.start, proposed.end)} title={`Proposed ${proposed.start}–${proposed.end}`}>
            <span>{proposed.start}</span>
          </div>
        )}
      </div>
      <div className="timeline-axis" aria-hidden="true">
        {hours.map((h) => (
          <span key={h} style={{ left: `${timelinePercent(`${String(h).padStart(2, '0')}:00`)}%` }}>
            {String(h).padStart(2, '0')}:00
          </span>
        ))}
      </div>
      <ul className="legend timeline-legend">
        <li><span className="key-area timeline-key-window" />Preferred window</li>
        <li><span className="key-area timeline-key-booking" />Existing booking</li>
        <li><span className="key-area timeline-key-proposed" />Proposed slot</li>
      </ul>
    </div>
  )
}
