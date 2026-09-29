// Run with `npm test` (Node's built-in runner; Node 23+ runs TypeScript directly).
import assert from 'node:assert/strict'
import { describe, test } from 'node:test'

import { causeLabel, describeDeviation } from '../src/utils/anomalies.ts'
import { daysAgo, formatDay, formatLkr, parseDay } from '../src/utils/format.ts'
import { toApiError } from '../src/utils/problem.ts'
import { normalizeApiBaseUrl, normalizeRole } from '../src/utils/marketApi.ts'
import { buildHeatmap, cellColor, type ShortageEvent } from '../src/utils/shortages.ts'
import { changePercent, fillGrid, niceTicks, periodGrid, type TrendPoint } from '../src/utils/trend.ts'

const point = (period: string, avgPrice: number): TrendPoint => ({
  period,
  avgPrice,
  minPrice: avgPrice - 10,
  maxPrice: avgPrice + 10,
  sampleCount: 20,
})

describe('trend', () => {
  // 14 Sep is missing: the API omits empty weeks.
  const points = [point('2026-08-24', 118.84), point('2026-08-31', 119.08), point('2026-09-07', 120.52), point('2026-09-21', 125.94)]

  test('weekly grid keeps missing weeks as gaps, never zeros', () => {
    const slots = fillGrid(points, periodGrid(points.map((p) => p.period), 'week'))

    assert.deepEqual(slots.map((s) => s.period), ['2026-08-24', '2026-08-31', '2026-09-07', '2026-09-14', '2026-09-21'])
    assert.equal(slots[3].point, null)
    assert.equal(slots[4].point?.avgPrice, 125.94)
  })

  test('monthly grid steps by calendar month', () => {
    assert.deepEqual(periodGrid(['2026-06-01', '2026-09-01'], 'month'), ['2026-06-01', '2026-07-01', '2026-08-01', '2026-09-01'])
  })

  test('change compares against the same week n steps back', () => {
    const change = changePercent(points, 'week', 4)
    assert.ok(change !== null)
    assert.equal(change.toFixed(2), '5.97')
  })

  test('change is null when the comparison week is a gap', () => {
    assert.equal(changePercent(points, 'week', 1), null)
  })

  test('nice ticks are round and cover the range', () => {
    assert.deepEqual(niceTicks(62.3, 98.7), [60, 70, 80, 90, 100])
    assert.deepEqual(niceTicks(100, 100).length > 1, true)
  })
})

describe('format', () => {
  test('LKR amounts group thousands and keep two decimals', () => {
    assert.equal(formatLkr(1234.5), 'LKR 1,234.50')
  })

  test('API dates are read as calendar days, not shifted by timezone', () => {
    assert.equal(parseDay('2026-09-21').toISOString(), '2026-09-21T00:00:00.000Z')
    assert.equal(formatDay('2026-09-21'), '21 Sep')
  })

  test('daysAgo counts back from the local calendar day', () => {
    assert.equal(daysAgo(112, new Date(2026, 8, 27, 23, 30)), '2026-06-07')
  })
})

describe('shortages heatmap', () => {
  const crops = [{ id: 'tomato', name: 'Tomato' }, { id: 'cabbage', name: 'Cabbage' }]
  const regions = [{ id: 'dambulla', name: 'Dambulla' }, { id: 'ne', name: 'Nuwara Eliya' }]
  const event = (id: string, cropId: string, regionId: string, type: ShortageEvent['type'], severity: ShortageEvent['severity'], detectedAt: string): ShortageEvent =>
    ({ id, cropId, regionId, type, severity, detectedAt, notes: null })

  const events = [
    event('a', 'tomato', 'dambulla', 'Shortage', 'Medium', '2026-09-01T00:00:00Z'),
    event('b', 'tomato', 'dambulla', 'Shortage', 'High', '2026-08-01T00:00:00Z'),
    event('c', 'cabbage', 'ne', 'Oversupply', 'Low', '2026-09-10T00:00:00Z'),
  ]
  const grid = buildHeatmap(events, crops, regions)

  test('one row per crop and one cell per region', () => {
    assert.equal(grid.length, 2)
    assert.deepEqual(grid.map((row) => row.length), [2, 2])
  })

  test('a cell is coloured by its most severe event', () => {
    assert.equal(grid[0][0].worst?.id, 'b')
    assert.equal(grid[0][0].events.length, 2)
  })

  test('empty cells use the neutral midpoint; arms follow the event type', () => {
    assert.equal(cellColor(grid[0][1].worst), 'var(--neutral-cell)')
    assert.equal(cellColor(grid[0][0].worst), 'var(--short-high)')
    assert.equal(cellColor(grid[1][1].worst), 'var(--over-low)')
  })
})

describe('anomalies', () => {
  test('deviation direction is spelled out', () => {
    assert.equal(describeDeviation(52.7), '52.7% above AI price')
    assert.equal(describeDeviation(-30), '30.0% below AI price')
  })

  test('cause codes read as words, including unknown future codes', () => {
    assert.equal(causeLabel('DistressedSale'), 'Distressed sale')
    assert.equal(causeLabel('SeasonalHarvestPeak'), 'Seasonal Harvest Peak')
  })
})

describe('API errors', () => {
  test('uses the ProblemDetails detail when present', () => {
    const error = toApiError(404, { title: 'Resource not found', status: 404, detail: 'Report x was not found.' })
    assert.equal(error.message, 'Report x was not found.')
  })

  test('surfaces field validation errors', () => {
    const error = toApiError(400, { title: 'One or more validation errors occurred.', errors: { dateRangeEnd: ["'dateRangeEnd' must be on or after 'dateRangeStart'."] } })
    assert.deepEqual(Object.keys(error.fieldErrors), ['dateRangeEnd'])
    assert.equal(error.message, "'dateRangeEnd' must be on or after 'dateRangeStart'.")
  })

  test('an empty 403 still gets a readable message', () => {
    assert.equal(toApiError(403, null).message, "Your role doesn't have access to this.")
  })
})

describe('authentication configuration', () => {
  test('normalizes API roots to exactly one /api suffix', () => {
    assert.equal(normalizeApiBaseUrl('http://localhost:5000'), 'http://localhost:5000/api')
    assert.equal(normalizeApiBaseUrl('http://localhost:5000/api/'), 'http://localhost:5000/api')
    assert.equal(normalizeApiBaseUrl(), 'http://localhost:5000/api')
  })

  test('maps the legacy Admin role to the canonical Administrator role', () => {
    assert.equal(normalizeRole('Admin'), 'Administrator')
    assert.equal(normalizeRole('Administrator'), 'Administrator')
  })
})
