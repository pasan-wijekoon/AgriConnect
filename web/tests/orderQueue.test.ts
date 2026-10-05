// Run with `npm test` (Node's built-in runner; Node 23+ runs TypeScript directly).
import assert from 'node:assert/strict'
import { describe, test } from 'node:test'

import { formatRemaining, needsAction, orderFlag, sortOrders, type QueueOrder } from '../src/utils/orderQueue.ts'

const NOW = Date.parse('2026-10-04T12:00:00Z')
const HOUR = 3_600_000
const iso = (offsetMs: number) => new Date(NOW + offsetMs).toISOString()

const order = (over: Partial<QueueOrder> = {}): QueueOrder => ({
  status: 'Pending',
  createdAt: iso(-2 * HOUR),
  reservationExpiresAt: iso(40 * HOUR),
  ...over,
})

describe('formatRemaining', () => {
  test('minutes, hours, days', () => {
    assert.equal(formatRemaining(10 * 60_000), '10 min')
    assert.equal(formatRemaining(1000), '1 min')
    assert.equal(formatRemaining(5 * HOUR + 30 * 60_000), '5 h')
    assert.equal(formatRemaining(48 * HOUR), '2 d')
    assert.equal(formatRemaining(51 * HOUR), '2 d 3 h')
  })
})

describe('needsAction', () => {
  test('Pending always needs the officer', () => {
    assert.equal(needsAction(order(), NOW), true)
  })

  test('Approved needs action until a slot is confirmed', () => {
    assert.equal(needsAction(order({ status: 'Approved', scheduleStatus: null }), NOW), true)
    assert.equal(needsAction(order({ status: 'Approved', scheduleStatus: 'Proposed' }), NOW), true)
    assert.equal(needsAction(order({ status: 'Approved', scheduleStatus: 'Cancelled' }), NOW), true)
    assert.equal(needsAction(order({ status: 'Approved', scheduleStatus: 'Confirmed' }), NOW), false)
  })

  test('Scheduled needs action only once the pickup time has passed', () => {
    const future = order({ status: 'Scheduled', slotStart: iso(5 * HOUR), slotEnd: iso(6 * HOUR) })
    const past = order({ status: 'Scheduled', slotStart: iso(-5 * HOUR), slotEnd: iso(-4 * HOUR) })
    assert.equal(needsAction(future, NOW), false)
    assert.equal(needsAction(past, NOW), true)
  })

  test('Completed and Cancelled never do', () => {
    assert.equal(needsAction(order({ status: 'Completed' }), NOW), false)
    assert.equal(needsAction(order({ status: 'Cancelled' }), NOW), false)
  })
})

describe('orderFlag', () => {
  test('Pending shows how long the reservation has left, with rising urgency', () => {
    assert.deepEqual(orderFlag(order({ reservationExpiresAt: iso(40 * HOUR) }), NOW), { label: 'Ends in 1 d 16 h', tone: 'info' })
    assert.deepEqual(orderFlag(order({ reservationExpiresAt: iso(10 * HOUR) }), NOW), { label: 'Ends in 10 h', tone: 'warn' })
    assert.deepEqual(orderFlag(order({ reservationExpiresAt: iso(3 * HOUR) }), NOW), { label: 'Ends in 3 h', tone: 'danger' })
  })

  test('an ended reservation is flagged', () => {
    assert.deepEqual(orderFlag(order({ reservationExpiresAt: iso(-HOUR) }), NOW), { label: 'Reservation ended', tone: 'danger' })
  })

  test('a Pending order without an expiry has no flag', () => {
    assert.equal(orderFlag(order({ reservationExpiresAt: null }), NOW), null)
  })

  test('an overdue pickup is flagged; other orders are not', () => {
    const overdue = order({ status: 'Scheduled', slotStart: iso(-3 * HOUR), slotEnd: iso(-2 * HOUR) })
    assert.deepEqual(orderFlag(overdue, NOW), { label: 'Pickup overdue', tone: 'danger' })
    assert.equal(orderFlag(order({ status: 'Approved' }), NOW), null)
  })
})

describe('sortOrders', () => {
  const a = order({ createdAt: iso(-3 * HOUR), reservationExpiresAt: iso(30 * HOUR), slotStart: iso(9 * HOUR) })
  const b = order({ createdAt: iso(-1 * HOUR), reservationExpiresAt: iso(10 * HOUR), slotStart: null })
  const c = order({ status: 'Approved', createdAt: iso(-2 * HOUR), reservationExpiresAt: iso(5 * HOUR), slotStart: iso(2 * HOUR) })

  test('newest first, without mutating the input', () => {
    const input = [a, b, c]
    assert.deepEqual(sortOrders(input, 'newest'), [b, c, a])
    assert.deepEqual(input, [a, b, c])
  })

  test('reservation ending soonest puts Pending orders first, nearest end on top', () => {
    // c is Approved, so its reservation no longer matters: it follows the Pending ones.
    assert.deepEqual(sortOrders([a, b, c], 'expiring'), [b, a, c])
  })

  test('pickup soonest puts scheduled orders first, others newest first', () => {
    assert.deepEqual(sortOrders([a, b, c], 'pickup'), [c, a, b])
  })
})
