import { parseDay, toIsoDay } from './format.ts'

export type Bucket = 'week' | 'month'

export interface TrendPoint {
  period: string
  avgPrice: number
  minPrice: number
  maxPrice: number
  sampleCount: number
}

export interface GridSlot<T> {
  period: string
  point: T | null
}

function step(isoDay: string, bucket: Bucket, n: number): string {
  const d = parseDay(isoDay)
  if (bucket === 'week') d.setUTCDate(d.getUTCDate() + 7 * n)
  else d.setUTCMonth(d.getUTCMonth() + n)
  return toIsoDay(d)
}

/**
 * Every period from `first` to `last`, with null where a series has no data. The API omits
 * empty periods; a chart must show them as gaps, never draw a line across or a zero.
 */
export function periodGrid(periods: string[], bucket: Bucket): string[] {
  if (periods.length === 0) return []
  const sorted = [...periods].sort()
  const grid: string[] = []
  for (let p = sorted[0]; p <= sorted[sorted.length - 1]; p = step(p, bucket, 1)) grid.push(p)
  return grid
}

export function fillGrid<T extends { period: string }>(points: T[], grid: string[]): GridSlot<T>[] {
  const byPeriod = new Map(points.map((p) => [p.period, p]))
  return grid.map((period) => ({ period, point: byPeriod.get(period) ?? null }))
}

/** Change of the latest average versus `stepsBack` periods earlier; null if that period is empty. */
export function changePercent(points: TrendPoint[], bucket: Bucket, stepsBack: number): number | null {
  if (points.length === 0) return null
  const latest = points[points.length - 1]
  const earlier = points.find((p) => p.period === step(latest.period, bucket, -stepsBack))
  if (!earlier || earlier.avgPrice === 0) return null
  return ((latest.avgPrice - earlier.avgPrice) / earlier.avgPrice) * 100
}

/** Round axis ticks (1, 2 or 5 × 10ⁿ apart) that cover [min, max]. */
export function niceTicks(min: number, max: number, target = 4): number[] {
  if (!Number.isFinite(min) || !Number.isFinite(max)) return []
  if (min === max) {
    min -= 1
    max += 1
  }
  const raw = (max - min) / target
  const magnitude = 10 ** Math.floor(Math.log10(raw))
  const stepSize = [1, 2, 5, 10].map((m) => m * magnitude).find((s) => s >= raw) ?? 10 * magnitude
  const ticks: number[] = []
  for (let t = Math.floor(min / stepSize) * stepSize; t <= max + stepSize * 0.999; t += stepSize) {
    ticks.push(Number(t.toFixed(10)))
  }
  return ticks
}
