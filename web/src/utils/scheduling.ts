/** Helpers for the AI Scheduling preview. Times are "HH:MM" at the collection centre (Sri Lanka). */

export const CENTRE_OFFSET = '+05:30'
export const CENTRE_TIME_ZONE = 'Asia/Colombo'

/** The timeline's visible day: 06:00 to 20:00. */
export const DAY_START = 6 * 60
export const DAY_END = 20 * 60

export interface BookingInput {
  id: string
  start: string
  end: string
}

export interface Scenario {
  id: string
  label: string
  description: string
  start: string
  end: string
  bookings: [string, string][]
}

/** Mirrors the agent's golden cases, so the demo shows each behaviour on purpose. */
export const SCENARIOS: Scenario[] = [
  {
    id: 'empty',
    label: 'Empty morning',
    description: 'No bookings: the slot starts right at the beginning of the window.',
    start: '08:00',
    end: '12:00',
    bookings: [],
  },
  {
    id: 'busy',
    label: 'Busy morning',
    description: 'Back-to-back bookings until 10:30: the slot shifts past the last one.',
    start: '08:00',
    end: '17:00',
    bookings: [['08:00', '09:00'], ['09:00', '10:30']],
  },
  {
    id: 'full',
    label: 'Fully booked day',
    description: 'Eight bookings fill the centre’s daily capacity: the agent moves to the next day.',
    start: '08:00',
    end: '17:00',
    bookings: [8, 9, 10, 11, 12, 13, 14, 15].map((h) => [pad(h) + ':00', pad(h + 1) + ':00'] as [string, string]),
  },
]

function pad(n: number): string {
  return String(n).padStart(2, '0')
}

/** "2026-10-05" + "13:00" → "2026-10-05T13:00:00+05:30". */
export const toIso = (date: string, time: string) => `${date}T${time}:00${CENTRE_OFFSET}`

export function toMinutes(time: string): number {
  const [h, m] = time.split(':').map(Number)
  return h * 60 + m
}

const partsFormat = new Intl.DateTimeFormat('en-CA', {
  timeZone: CENTRE_TIME_ZONE,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
})

/** An ISO timestamp as the centre's local date and time, whatever offset it was written in. */
export function centreDateTime(iso: string): { date: string; time: string } {
  const parts = Object.fromEntries(partsFormat.formatToParts(new Date(iso)).map((p) => [p.type, p.value]))
  return { date: `${parts.year}-${parts.month}-${parts.day}`, time: `${parts.hour}:${parts.minute}` }
}

const WEEKDAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']

export function weekday(date: string): string {
  return WEEKDAYS[new Date(`${date}T00:00:00Z`).getUTCDay()]
}

/** Problems a user can fix before asking the agent; empty when the request is valid. */
export function validateRequest(date: string, start: string, end: string, bookings: BookingInput[]): string[] {
  const problems: string[] = []
  if (!date) problems.push('Pick a date.')
  if (!start || !end) problems.push('Set both ends of the preferred window.')
  else if (toMinutes(end) <= toMinutes(start)) problems.push('The preferred window must end after it starts.')
  bookings.forEach((b, i) => {
    if (!b.start || !b.end) problems.push(`Booking ${i + 1} needs a start and an end.`)
    else if (toMinutes(b.end) <= toMinutes(b.start)) problems.push(`Booking ${i + 1} must end after it starts.`)
  })
  return problems
}

/** Position of a time on the 06:00–20:00 timeline, as a clamped percentage. */
export function timelinePercent(time: string): number {
  const pct = ((toMinutes(time) - DAY_START) / (DAY_END - DAY_START)) * 100
  return Math.min(100, Math.max(0, pct))
}

/** Tomorrow's date at the centre, the default day to schedule. */
export function tomorrowAtCentre(now = new Date()): string {
  const { date } = centreDateTime(now.toISOString())
  const d = new Date(`${date}T00:00:00Z`)
  d.setUTCDate(d.getUTCDate() + 1)
  return d.toISOString().slice(0, 10)
}
