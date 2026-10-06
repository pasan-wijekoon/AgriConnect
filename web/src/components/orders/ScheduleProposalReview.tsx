import { useState } from 'react'
import type { ScheduleResponse } from '../../utils/ordersApi'
import { Button } from '../ui/Button'
import { ScheduleStatusBadge } from './OrderStatusBadge'
import './ScheduleProposalReview.css'

interface ScheduleProposalReviewProps {
  schedule: ScheduleResponse
  onApprove: () => Promise<void>
  onReject: () => Promise<void>
  /** Opens the revision dialog (FR19 "Request Revision"). */
  onRequestRevision?: () => void
  /** Why the nearest centre was suggested, when the Matching Agent was used. */
  matchExplanation?: string | null
  /** Loads other windows to choose from; the Officer picks one with `onUseAlternative`. */
  onLoadAlternatives?: () => Promise<{ start: string; end: string }[]>
  onUseAlternative?: (window: { start: string; end: string }) => Promise<void>
  busy: boolean
}

function formatWindow(startIso: string, endIso: string): string {
  const start = new Date(startIso)
  const end = new Date(endIso)
  const dateLabel = start.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })
  const timeLabel = (d: Date) => d.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' })
  return `${dateLabel} · ${timeLabel(start)} – ${timeLabel(end)}`
}

/**
 * Design.md §34 — Officer review screen. The AI-proposal framing (per plan
 * §8.4/CLAUDE.md §17/§18) is deliberate: nothing here is final until an
 * Officer clicks Approve. When the buyer shared a location the Buyer-Farmer
 * Matching Agent suggests the centre; its short explanation is shown here
 * ("Why this centre") without inventing a confidence number.
 */
export function ScheduleProposalReview({
  schedule,
  onApprove,
  onReject,
  onRequestRevision,
  matchExplanation,
  onLoadAlternatives,
  onUseAlternative,
  busy,
}: ScheduleProposalReviewProps) {
  const [action, setAction] = useState<'approve' | 'reject' | null>(null)
  const [alternatives, setAlternatives] = useState<{ start: string; end: string }[] | null>(null)
  const [loadingAlternatives, setLoadingAlternatives] = useState(false)
  const [alternativesError, setAlternativesError] = useState<string | null>(null)

  const isDecidable = schedule.status === 'Proposed'

  const handleApprove = async () => {
    setAction('approve')
    await onApprove()
  }

  const handleReject = async () => {
    setAction('reject')
    await onReject()
  }

  const showAlternatives = async () => {
    if (!onLoadAlternatives) return
    setLoadingAlternatives(true)
    setAlternativesError(null)
    try {
      setAlternatives(await onLoadAlternatives())
    } catch {
      setAlternativesError('We couldn’t load alternative slots. Please try again.')
    } finally {
      setLoadingAlternatives(false)
    }
  }

  return (
    <div className="schedule-review">
      <div className="schedule-review-header">
        <h3>Schedule Proposal</h3>
        <ScheduleStatusBadge status={schedule.status} />
      </div>

      {isDecidable && (
        <p className="schedule-review-ai-note">
          This slot was proposed automatically. It only takes effect once you approve it — or you can reject it, or send it back for a different window.
        </p>
      )}

      <dl className="schedule-review-details">
        <div>
          <dt>Proposed Time</dt>
          <dd>{formatWindow(schedule.slotStart, schedule.slotEnd)}</dd>
        </div>
        <div>
          <dt>Conflict Check</dt>
          <dd>{schedule.conflictChecked ? 'No conflicts detected' : 'Overlaps an existing booking'}</dd>
        </div>
        {matchExplanation && (
          <div>
            <dt>Why this centre</dt>
            <dd>{matchExplanation}</dd>
          </div>
        )}
      </dl>

      {isDecidable && onLoadAlternatives && (
        <div className="schedule-review-alternatives">
          {alternatives === null ? (
            <Button variant="secondary" onClick={showAlternatives} loading={loadingAlternatives} disabled={busy}>
              Suggest other slots
            </Button>
          ) : alternatives.length === 0 ? (
            <p className="schedule-review-alt-note">No other free slots were found in the next few days.</p>
          ) : (
            <>
              <p className="schedule-review-alt-note">Other free slots at this centre. Picking one sends it back for your review.</p>
              <ul className="schedule-review-alt-list">
                {alternatives.map((alt) => (
                  <li key={alt.start}>
                    <span>{formatWindow(alt.start, alt.end)}</span>
                    <Button variant="secondary" disabled={busy} onClick={() => onUseAlternative?.(alt)}>
                      Use this slot
                    </Button>
                  </li>
                ))}
              </ul>
            </>
          )}
          {alternativesError && <p className="schedule-review-alt-note" role="alert">{alternativesError}</p>}
        </div>
      )}

      {isDecidable && (
        <div className="schedule-review-actions">
          {onRequestRevision && (
            <Button variant="secondary" onClick={onRequestRevision} disabled={busy}>
              Request revision
            </Button>
          )}
          <Button variant="destructive" onClick={handleReject} loading={busy && action === 'reject'}>
            Reject
          </Button>
          <Button variant="primary" onClick={handleApprove} loading={busy && action === 'approve'}>
            Approve
          </Button>
        </div>
      )}
    </div>
  )
}
