// Helpers for the Officer's order queue: which orders need attention, how long a Pending
// reservation has left, and the sort orders. Visual hints only: no order status is added
// or changed here.

export type QueueSort = 'newest' | 'expiring' | 'pickup'

export const QUEUE_SORT_LABELS: Record<QueueSort, string> = {
  newest: 'Newest first',
  expiring: 'Reservation ending soonest',
  pickup: 'Pickup soonest',
}

/** The few order fields these helpers read (kept structural so tests don't need a full order). */
export interface QueueOrder {
  status: string
  createdAt: string
  reservationExpiresAt?: string | null
  scheduleStatus?: string | null
  slotStart?: string | null
  slotEnd?: string | null
}

export interface OrderFlag {
  label: string
  tone: 'info' | 'warn' | 'danger'
}

const HOUR = 3_600_000
const DAY = 24 * HOUR

const time = (iso: string | null | undefined): number | null => {
  if (!iso) return null
  const t = new Date(iso).getTime()
  return Number.isNaN(t) ? null : t
}

/** "45 min", "5 h", "2 d 3 h". */
export function formatRemaining(ms: number): string {
  if (ms < HOUR) return `${Math.max(1, Math.ceil(ms / 60_000))} min`
  if (ms < DAY) return `${Math.floor(ms / HOUR)} h`
  const days = Math.floor(ms / DAY)
  const hours = Math.floor((ms % DAY) / HOUR)
  return hours > 0 ? `${days} d ${hours} h` : `${days} d`
}

function pickupIsOverdue(order: QueueOrder, now: number): boolean {
  const end = time(order.slotEnd) ?? time(order.slotStart)
  return order.status === 'Scheduled' && end !== null && end < now
}

/** An order is waiting on the Officer: approval, a slot to propose, a proposal to review, or an overdue pickup. */
export function needsAction(order: QueueOrder, now: number = Date.now()): boolean {
  if (order.status === 'Pending') return true
  if (order.status === 'Approved') {
    return !order.scheduleStatus || order.scheduleStatus === 'Proposed' || order.scheduleStatus === 'Cancelled'
  }
  return pickupIsOverdue(order, now)
}

/** A short badge for the queue row, or null when there is nothing worth flagging. */
export function orderFlag(order: QueueOrder, now: number = Date.now()): OrderFlag | null {
  if (order.status === 'Pending') {
    const expires = time(order.reservationExpiresAt)
    if (expires === null) return null
    const left = expires - now
    if (left <= 0) return { label: 'Reservation ended', tone: 'danger' }
    return { label: `Ends in ${formatRemaining(left)}`, tone: left <= 6 * HOUR ? 'danger' : left <= DAY ? 'warn' : 'info' }
  }
  if (pickupIsOverdue(order, now)) return { label: 'Pickup overdue', tone: 'danger' }
  return null
}

/** Returns a sorted copy; the input array is left untouched. */
export function sortOrders<T extends QueueOrder>(orders: readonly T[], sort: QueueSort): T[] {
  const byNewest = (a: T, b: T) => (time(b.createdAt) ?? 0) - (time(a.createdAt) ?? 0)
  const copy = [...orders]

  if (sort === 'expiring') {
    // Pending orders first, the nearest reservation end on top; everything else newest first.
    return copy.sort((a, b) => {
      const ea = a.status === 'Pending' ? time(a.reservationExpiresAt) : null
      const eb = b.status === 'Pending' ? time(b.reservationExpiresAt) : null
      if (ea !== null && eb !== null) return ea - eb
      if (ea !== null) return -1
      if (eb !== null) return 1
      return byNewest(a, b)
    })
  }

  if (sort === 'pickup') {
    // Orders with a pickup slot first, soonest on top; the rest newest first.
    return copy.sort((a, b) => {
      const pa = time(a.slotStart)
      const pb = time(b.slotStart)
      if (pa !== null && pb !== null) return pa - pb
      if (pa !== null) return -1
      if (pb !== null) return 1
      return byNewest(a, b)
    })
  }

  return copy.sort(byNewest)
}
