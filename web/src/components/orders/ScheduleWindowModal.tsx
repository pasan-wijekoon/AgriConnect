import { useState, type FormEvent } from 'react'
import type { CollectionCentreResponse } from '../../utils/ordersApi'
import { Button } from '../ui/Button'
import './ScheduleWindowModal.css'

export interface ScheduleWindowValue {
  centreId?: string
  start: string
  end: string
  reason?: string
}

interface ScheduleWindowModalProps {
  title: string
  description?: string
  submitLabel: string
  /** When provided, the Officer picks the centre (initial proposal); omitted for a revision (same centre). */
  centres?: CollectionCentreResponse[]
  defaultCentreId?: string
  /** Ask for a reason (revision requests). */
  askReason?: boolean
  busy: boolean
  error?: string | null
  onSubmit: (value: ScheduleWindowValue) => void
  onClose: () => void
}

const pad = (n: number) => String(n).padStart(2, '0')
const toDateInput = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`

/**
 * Officer picks a pickup window (and optionally the centre / a revision reason).
 * The result is only ever a *proposal* — the server validates capacity and conflicts,
 * and the Officer still has to Approve it (FR19).
 */
export function ScheduleWindowModal({
  title,
  description,
  submitLabel,
  centres,
  defaultCentreId,
  askReason,
  busy,
  error,
  onSubmit,
  onClose,
}: ScheduleWindowModalProps) {
  const tomorrow = new Date(Date.now() + 24 * 60 * 60 * 1000)
  const [centreId, setCentreId] = useState(defaultCentreId ?? centres?.[0]?.id ?? '')
  const [date, setDate] = useState(toDateInput(tomorrow))
  const [startTime, setStartTime] = useState('09:00')
  const [endTime, setEndTime] = useState('10:00')
  const [reason, setReason] = useState('')
  const [localError, setLocalError] = useState<string | null>(null)

  const submit = (e: FormEvent) => {
    e.preventDefault()
    const start = new Date(`${date}T${startTime}`)
    const end = new Date(`${date}T${endTime}`)
    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) {
      setLocalError('Choose a valid date and times.')
      return
    }
    if (end <= start) {
      setLocalError('The end time must be after the start time.')
      return
    }
    if (start.getTime() < Date.now()) {
      setLocalError('The window can’t be in the past.')
      return
    }
    setLocalError(null)
    onSubmit({
      centreId: centres ? centreId : undefined,
      start: start.toISOString(),
      end: end.toISOString(),
      reason: askReason && reason.trim() ? reason.trim() : undefined,
    })
  }

  const shownError = localError ?? error

  return (
    <div className="swm-overlay" role="presentation" onMouseDown={(e) => e.target === e.currentTarget && !busy && onClose()}>
      <form className="swm" role="dialog" aria-modal="true" aria-label={title} onSubmit={submit}>
        <h2>{title}</h2>
        {description && <p className="swm-desc">{description}</p>}

        {centres && (
          <label>
            Collection centre
            <select value={centreId} onChange={(e) => setCentreId(e.target.value)} required>
              {centres.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name} (capacity {c.capacity})
                </option>
              ))}
            </select>
          </label>
        )}

        <label>
          Date
          <input type="date" value={date} min={toDateInput(new Date())} onChange={(e) => setDate(e.target.value)} required />
        </label>

        <div className="swm-row">
          <label>
            From
            <input type="time" value={startTime} onChange={(e) => setStartTime(e.target.value)} required />
          </label>
          <label>
            To
            <input type="time" value={endTime} onChange={(e) => setEndTime(e.target.value)} required />
          </label>
        </div>

        {askReason && (
          <label>
            Reason (optional)
            <textarea rows={2} value={reason} onChange={(e) => setReason(e.target.value)} placeholder="e.g. Centre closed that afternoon" />
          </label>
        )}

        {shownError && (
          <p className="swm-error" role="alert">
            {shownError}
          </p>
        )}

        <div className="swm-actions">
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            Cancel
          </Button>
          <Button type="submit" loading={busy}>
            {submitLabel}
          </Button>
        </div>
      </form>
    </div>
  )
}
