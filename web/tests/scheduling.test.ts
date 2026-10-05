import assert from 'node:assert/strict'
import { describe, test } from 'node:test'

import {
  SCENARIOS,
  centreDateTime,
  timelinePercent,
  toIso,
  tomorrowAtCentre,
  validateRequest,
  weekday,
} from '../src/utils/scheduling.ts'

describe('AI scheduling helpers', () => {
  test('requests are sent in centre time (+05:30)', () => {
    assert.equal(toIso('2026-10-05', '13:00'), '2026-10-05T13:00:00+05:30')
  })

  test('answers are shown in centre time whatever offset they come back in', () => {
    assert.deepEqual(centreDateTime('2026-10-05T07:30:00Z'), { date: '2026-10-05', time: '13:00' })
    assert.deepEqual(centreDateTime('2026-10-05T13:00:00+05:30'), { date: '2026-10-05', time: '13:00' })
    // Late UTC evening is already the next day in Sri Lanka.
    assert.deepEqual(centreDateTime('2026-10-05T20:00:00Z'), { date: '2026-10-06', time: '01:30' })
  })

  test('validation catches what the user can fix', () => {
    assert.deepEqual(validateRequest('2026-10-05', '08:00', '17:00', []), [])
    assert.deepEqual(validateRequest('2026-10-05', '12:00', '09:00', []), ['The preferred window must end after it starts.'])
    assert.deepEqual(
      validateRequest('2026-10-05', '08:00', '17:00', [{ id: 'a', start: '10:00', end: '' }, { id: 'b', start: '11:00', end: '10:00' }]),
      ['Booking 1 needs a start and an end.', 'Booking 2 must end after it starts.'],
    )
    assert.deepEqual(validateRequest('', '08:00', '17:00', []), ['Pick a date.'])
  })

  test('the timeline spans 06:00 to 20:00 and clamps outside it', () => {
    assert.equal(timelinePercent('06:00'), 0)
    assert.equal(timelinePercent('13:00'), 50)
    assert.equal(timelinePercent('20:00'), 100)
    assert.equal(timelinePercent('05:00'), 0)
    assert.equal(timelinePercent('23:00'), 100)
  })

  test('the fully booked scenario really fills the 8-slot daily capacity', () => {
    const full = SCENARIOS.find((s) => s.id === 'full')
    assert.ok(full)
    assert.equal(full.bookings.length, 8)
    assert.deepEqual(full.bookings[0], ['08:00', '09:00'])
    assert.deepEqual(full.bookings[7], ['15:00', '16:00'])
  })

  test('tomorrow is counted at the centre, not in UTC', () => {
    // 20:00 UTC on the 5th is 01:30 on the 6th in Sri Lanka, so tomorrow is the 7th.
    assert.equal(tomorrowAtCentre(new Date('2026-10-05T20:00:00Z')), '2026-10-07')
    assert.equal(weekday('2026-10-05'), 'Mon')
  })
})
