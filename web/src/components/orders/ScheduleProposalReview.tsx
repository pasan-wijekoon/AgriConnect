import { useState } from 'react'
import type { ScheduleResponse } from '../../utils/ordersApi'
import { Button } from '../ui/Button'
import { ScheduleStatusBadge } from './OrderStatusBadge'
import './ScheduleProposalReview.css'

interface ScheduleProposalReviewProps {
  schedule: ScheduleResponse
  onApprove: () => Promise<void>
  onReject: () => Promise<void>
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
 * Officer clicks Approve. The persisted schedule doesn't currently carry a
 * match-confidence score — the Buyer-Farmer Matching Agent isn't wired into
 * the backend's scheduling flow yet (see PROGRESS.md) — so this shows what
 * the API actually returns rather than fabricating a confidence number.
 */
export function ScheduleProposalReview({ schedule, onApprove, onReject, busy }: ScheduleProposalReviewProps) {
  const [action, setAction] = useState<'approve' | 'reject' | null>(null)

  const isDecidable = schedule.status === 'Proposed'

  const handleApprove = async () => {
    setAction('approve')
    await onApprove()
  }

  const handleReject = async () => {
    setAction('reject')
    await onReject()
  }

  return (
    <div className="schedule-review">
      <div className="schedule-review-header">
        <h3>Schedule Proposal</h3>
        <ScheduleStatusBadge status={schedule.status} />
      </div>

      {isDecidable && (
        <p className="schedule-review-ai-note">
          This is an AI-generated proposal. It only takes effect once you approve it.
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
      </dl>

      {isDecidable && (
        <div className="schedule-review-actions">
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
