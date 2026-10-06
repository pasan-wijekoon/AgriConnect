const money = new Intl.NumberFormat('en-LK', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const whole = new Intl.NumberFormat('en-LK', { maximumFractionDigits: 0 })
// Fixed names: ICU versions disagree ("Sep" vs "Sept"), and the Flutter app uses these.
const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

export const formatLkr = (value: number) => `LKR ${money.format(value)}`

export const formatWhole = (value: number) => whole.format(value)

export const formatPercent = (value: number) => `${Math.abs(value).toFixed(1)}%`

/** A calendar date from the API ("2026-09-21"), read as UTC so no timezone shifts the day. */
export const parseDay = (isoDate: string) => new Date(`${isoDate.slice(0, 10)}T00:00:00Z`)

export const toIsoDay = (date: Date) => date.toISOString().slice(0, 10)

export function formatDay(isoDate: string): string {
  const d = parseDay(isoDate)
  return `${d.getUTCDate()} ${MONTHS[d.getUTCMonth()]}`
}

export const formatDate = (isoDate: string) => `${formatDay(isoDate)} ${parseDay(isoDate).getUTCFullYear()}`

export function formatMonth(isoDate: string): string {
  const d = parseDay(isoDate)
  return `${MONTHS[d.getUTCMonth()]} ${d.getUTCFullYear()}`
}

/** A timestamp with offset from the API, shown in the viewer's local time. */
export function formatDateTime(isoTimestamp: string): string {
  const d = new Date(isoTimestamp)
  const time = `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
  return `${d.getDate()} ${MONTHS[d.getMonth()]} ${d.getFullYear()}, ${time}`
}

/** Today as a calendar date in the viewer's timezone, minus `days`. */
export function daysAgo(days: number, now = new Date()): string {
  const local = new Date(Date.UTC(now.getFullYear(), now.getMonth(), now.getDate()))
  local.setUTCDate(local.getUTCDate() - days)
  return toIsoDay(local)
}
