import { Badge, type BadgeTone } from '../ui/Badge'
import type { OrderStatus, ScheduleStatus } from '../../utils/ordersApi'

// Design.md §6/§20 — one shared mapping so status colors are consistent
// everywhere. Icons accompany color, never color alone (§39).
const ORDER_STATUS_META: Record<OrderStatus, { tone: BadgeTone; icon: string }> = {
  Pending: { tone: 'pending', icon: '⏳' },
  Approved: { tone: 'success', icon: '✓' },
  Scheduled: { tone: 'info', icon: '📅' },
  Completed: { tone: 'success', icon: '✓' },
  Cancelled: { tone: 'error', icon: '✕' },
}

export function OrderStatusBadge({ status }: { status: OrderStatus }) {
  const meta = ORDER_STATUS_META[status]
  return <Badge tone={meta.tone}>{`${meta.icon} ${status}`}</Badge>
}

const SCHEDULE_STATUS_META: Record<ScheduleStatus, { tone: BadgeTone; icon: string }> = {
  Proposed: { tone: 'proposed', icon: '✦' },
  Confirmed: { tone: 'success', icon: '✓' },
  Cancelled: { tone: 'error', icon: '✕' },
}

export function ScheduleStatusBadge({ status }: { status: ScheduleStatus }) {
  const meta = SCHEDULE_STATUS_META[status]
  return <Badge tone={meta.tone}>{`${meta.icon} ${status}`}</Badge>
}
