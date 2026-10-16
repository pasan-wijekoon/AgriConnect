import { useCallback, useEffect, useMemo, useState } from 'react'
import { useAuth } from '../context/AuthContext'
import { OrderActivity } from '../components/orders/OrderActivity'
import { ApiError, ordersApi, type OrderResponse, type OrderStatus } from '../utils/ordersApi'
import '../styles/my-orders.css'

const FILTERS = ['All', 'Active', 'Completed', 'Cancelled'] as const
type Filter = (typeof FILTERS)[number]

const STAGES: OrderStatus[] = ['Pending', 'Approved', 'Scheduled', 'Completed']
const STAGE_HINT: Record<string, string> = {
  Pending: 'An officer is reviewing your order',
  Approved: 'A pickup slot is being arranged',
  Scheduled: 'Pickup slot confirmed',
  Completed: 'Produce collected',
}

/**
 * The backend *sometimes* returns enum values as numbers (0, 1, 2, …) rather than
 * strings, and TypeScript's `OrderStatus` union can't protect us at runtime. Coerce
 * whatever we get back onto the union so downstream code can safely call
 * `.toLowerCase()`, compare against `STAGES`, etc.
 */
const STATUS_BY_INDEX: OrderStatus[] = ['Pending', 'Approved', 'Scheduled', 'Completed', 'Cancelled']
const STATUS_BY_NAME: Record<string, OrderStatus> = {
  pending: 'Pending',
  approved: 'Approved',
  scheduled: 'Scheduled',
  completed: 'Completed',
  cancelled: 'Cancelled',
  canceled: 'Cancelled',
}

function normalizeStatus(value: unknown, fallback: OrderStatus = 'Pending'): OrderStatus {
  if (typeof value === 'number' && Number.isInteger(value) && value >= 0 && value < STATUS_BY_INDEX.length) {
    return STATUS_BY_INDEX[value]
  }
  if (typeof value === 'string') {
    const named = STATUS_BY_NAME[value.trim().toLowerCase()]
    if (named) return named
    const asNum = Number(value)
    if (Number.isInteger(asNum) && asNum >= 0 && asNum < STATUS_BY_INDEX.length) {
      return STATUS_BY_INDEX[asNum]
    }
  }
  return fallback
}

function normalizeOrder(raw: OrderResponse): OrderResponse {
  const rawStatus = (raw as unknown as { status?: unknown }).status
  const rawSchedule = (raw as unknown as { scheduleStatus?: unknown }).scheduleStatus
  return {
    ...raw,
    status: normalizeStatus(rawStatus),
    scheduleStatus:
      rawSchedule == null || rawSchedule === ''
        ? undefined
        : (String(rawSchedule) as OrderResponse['scheduleStatus']),
  }
}

const fmtQty = (n: number) => Number(n.toFixed(2)).toString()
const fmtDate = (iso: string) => new Date(iso).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })
const fmtDateTime = (iso: string) =>
  new Date(iso).toLocaleString(undefined, { day: 'numeric', month: 'short', year: 'numeric', hour: 'numeric', minute: '2-digit' })

function StatusPill({ status }: { status: unknown }) {
  const label = status == null || status === '' ? 'Unknown' : String(status)
  const slug = label.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9-]/g, '')
  return <span className={`mo-pill mo-pill-${slug}`}>{label}</span>
}

function isActive(s: OrderStatus) {
  return s === 'Pending' || s === 'Approved' || s === 'Scheduled'
}

/**
 * "My Orders" for Buyers and "Incoming Orders" for Farmers (FR11): the caller's own
 * orders (scoped server-side), filterable, each opening a tracking timeline with the
 * pickup schedule. Buyers can cancel while an order is still Pending/Approved.
 */
export function MyOrdersPage() {
  const { user } = useAuth()
  const isBuyer = user?.role === 'Buyer'

  const [orders, setOrders] = useState<OrderResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [filter, setFilter] = useState<Filter>('All')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [cancelling, setCancelling] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)

  const load = useCallback(() => {
    setLoading(true)
    setError(null)
    ordersApi
      .list({ size: 100 })
      .then((r) => setOrders(r.items.map(normalizeOrder)))
      .catch((e: unknown) => setError(e instanceof ApiError ? e.message : 'We couldn’t load your orders.'))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    load()
  }, [load])

  const visible = useMemo(
    () =>
      orders.filter((o) =>
        filter === 'All'
          ? true
          : filter === 'Active'
            ? isActive(o.status)
            : filter === 'Completed'
              ? o.status === 'Completed'
              : o.status === 'Cancelled',
      ),
    [orders, filter],
  )
  const selected = orders.find((o) => o.id === selectedId) ?? null

  const cancel = async (order: OrderResponse) => {
    if (!window.confirm('Cancel this order? The stock reserved for you will be released.')) return
    setCancelling(true)
    setNotice(null)
    try {
      const updated = await ordersApi.cancel(order.id)
      const normalized = normalizeOrder(updated)
      setOrders((prev) => prev.map((o) => (o.id === normalized.id ? normalized : o)))
    } catch (e) {
      setNotice(e instanceof ApiError ? e.message : 'Could not cancel the order.')
    } finally {
      setCancelling(false)
    }
  }

  return (
    <div className="mo">
      <div className="mo-head">
        <div>
          <h1>{isBuyer ? 'My Orders' : 'Incoming Orders'}</h1>
          <p>
            {isBuyer
              ? 'Track your orders from review to pickup.'
              : 'Orders buyers have placed on your listings.'}
          </p>
        </div>
        <button type="button" className="btn btn-ghost" onClick={load} disabled={loading}>
          Refresh
        </button>
      </div>

      <div className="mo-filters" role="tablist" aria-label="Order filter">
        {FILTERS.map((f) => (
          <button
            key={f}
            type="button"
            role="tab"
            aria-selected={filter === f}
            className={`mo-chip${filter === f ? ' active' : ''}`}
            onClick={() => setFilter(f)}
          >
            {f}
          </button>
        ))}
      </div>

      {loading && orders.length === 0 && <p className="mo-state">Loading orders…</p>}
      {error && (
        <div className="mo-state mo-error" role="alert">
          {error} <button type="button" className="mo-link" onClick={load}>Try again</button>
        </div>
      )}
      {!loading && !error && orders.length === 0 && (
        <div className="mo-state">
          <strong>No orders yet</strong>
          <span>
            {isBuyer
              ? 'Open a listing in the marketplace and place your first order.'
              : 'Orders placed on your listings will show up here.'}
          </span>
        </div>
      )}
      {!loading && !error && orders.length > 0 && visible.length === 0 && (
        <div className="mo-state">Nothing in this view. Try a different filter.</div>
      )}

      <div className="mo-layout">
        <ul className="mo-list">
          {visible.map((o) => {
            const counterpart = isBuyer ? o.farmerName : o.buyerName
            return (
              <li key={o.id}>
                <button
                  type="button"
                  className={`mo-card${o.id === selectedId ? ' selected' : ''}`}
                  onClick={() => setSelectedId(o.id)}
                >
                  <div className="mo-card-top">
                    <strong>{o.cropName ?? 'Produce order'}</strong>
                    <StatusPill status={o.status} />
                  </div>
                  <div className="mo-meta">
                    {fmtQty(o.quantity)} {o.unit ?? 'kg'} · {o.deliveryPreference}
                    {counterpart ? ` · ${isBuyer ? 'Farmer' : 'Buyer'}: ${counterpart}` : ''}
                  </div>
                  {o.slotStart ? (
                    <div className="mo-slot">
                      📅 {fmtDateTime(o.slotStart)}
                      {o.collectionCentreName ? ` · ${o.collectionCentreName}` : ''}
                      {String(o.scheduleStatus ?? '').toLowerCase() === 'proposed' && <em> (proposed)</em>}
                    </div>
                  ) : (
                    o.status === 'Pending' && <div className="mo-slot mo-waiting">Waiting for an officer to review</div>
                  )}
                  <div className="mo-placed">Placed {fmtDate(o.createdAt)}</div>
                </button>
              </li>
            )
          })}
        </ul>

        {selected && (
          <aside className="mo-detail" aria-label="Order tracking">
            <div className="mo-detail-head">
              <div>
                <h2>{selected.cropName ?? 'Produce order'}</h2>
                <span className="mo-ref">Order #{selected.id.slice(0, 8)}</span>
              </div>
              <button type="button" className="mo-close" aria-label="Close" onClick={() => setSelectedId(null)}>
                ✕
              </button>
            </div>

            <dl className="mo-kv">
              <dt>Status</dt>
              <dd><StatusPill status={selected.status} /></dd>
              <dt>Quantity</dt>
              <dd>{fmtQty(selected.quantity)} {selected.unit ?? 'kg'}</dd>
              <dt>Handling</dt>
              <dd>{selected.deliveryPreference}</dd>
              {selected.regionName && (<><dt>Region</dt><dd>{selected.regionName}</dd></>)}
              {selected.farmerName && (<><dt>Farmer</dt><dd>{selected.farmerName}</dd></>)}
              {selected.buyerName && !isBuyer && (<><dt>Buyer</dt><dd>{selected.buyerName}</dd></>)}
              {selected.collectionCentreName && (<><dt>Centre</dt><dd>{selected.collectionCentreName}</dd></>)}
              <dt>Placed</dt>
              <dd>{fmtDateTime(selected.createdAt)}</dd>
            </dl>

            {selected.status === 'Pending' && selected.reservationExpiresAt && (
              <p className="mo-banner mo-banner-warn">
                Stock is held until {fmtDateTime(selected.reservationExpiresAt)}. It is released if the order isn’t approved by then.
              </p>
            )}

            {selected.status === 'Cancelled' ? (
              <p className="mo-banner mo-banner-error">This order was cancelled and its reserved stock was released.</p>
            ) : (
              <ol className="mo-timeline">
                {STAGES.map((stage, i) => {
                  const current = STAGES.indexOf(selected.status)
                  return (
                    <li key={stage} className={i <= current ? 'done' : ''}>
                      <span className="mo-dot" aria-hidden="true">{i <= current ? '✓' : ''}</span>
                      <div>
                        <strong>{stage}</strong>
                        {i === current && <small>{STAGE_HINT[stage]}</small>}
                      </div>
                    </li>
                  )
                })}
              </ol>
            )}

            {selected.slotStart && selected.status !== 'Cancelled' && (
              <div className="mo-schedule">
                <h3>
                  Pickup schedule{' '}
                  {selected.scheduleStatus && <StatusPill status={selected.scheduleStatus} />}
                </h3>
                <p>
                  {fmtDateTime(selected.slotStart)}
                  {selected.slotEnd
                    ? ` – ${new Date(selected.slotEnd).toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' })}`
                    : ''}
                </p>
                {selected.collectionCentreName && <p>{selected.collectionCentreName}</p>}
                {String(selected.scheduleStatus ?? '').toLowerCase() === 'proposed' && (
                  <p className="mo-banner mo-banner-info">This slot is a proposal — it becomes final once the officer confirms it.</p>
                )}
              </div>
            )}

            <div className="mo-activity">
              <h3>Activity</h3>
              <OrderActivity
                orderId={selected.id}
                refreshKey={`${selected.updatedAt}|${selected.scheduleStatus ?? ''}|${selected.slotStart ?? ''}`}
              />
            </div>

            {notice && <p className="mo-banner mo-banner-error" role="alert">{notice}</p>}

            {isBuyer && (selected.status === 'Pending' || selected.status === 'Approved') && (
              <button type="button" className="mo-cancel" onClick={() => cancel(selected)} disabled={cancelling}>
                {cancelling ? 'Cancelling…' : 'Cancel order'}
              </button>
            )}
          </aside>
        )}
      </div>
    </div>
  )
}