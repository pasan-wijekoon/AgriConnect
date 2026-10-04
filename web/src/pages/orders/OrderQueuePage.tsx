import { useEffect, useMemo, useState } from 'react'
import { OrderTable } from '../../components/orders/OrderTable'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/StateViews'
import { Card } from '../../components/ui/Card'
import { ApiError, ordersApi, type OrderResponse, type OrderStatus } from '../../utils/ordersApi'
import { needsAction, QUEUE_SORT_LABELS, sortOrders, type QueueSort } from '../../utils/orderQueue'
import './OrderQueuePage.css'

const STATUS_OPTIONS: (OrderStatus | 'All')[] = ['All', 'Pending', 'Approved', 'Scheduled', 'Completed', 'Cancelled']
const PAGE_SIZE = 10
const FETCH_SIZE = 100

const OVERVIEW: { status: OrderStatus; label: string; hint: string }[] = [
  { status: 'Pending', label: 'Awaiting approval', hint: 'Review and approve' },
  { status: 'Approved', label: 'Approved', hint: 'Slot to confirm' },
  { status: 'Scheduled', label: 'Scheduled', hint: 'Pickup confirmed' },
  { status: 'Completed', label: 'Completed', hint: 'Collected' },
]

type LoadState = { kind: 'loading' } | { kind: 'error'; message: string } | { kind: 'ready' }

/**
 * Design.md §30 — the Officer's order list: centre-scoped server-side, with a status
 * overview, status filter, free-text search and pagination.
 */
export function OrderQueuePage() {
  const [status, setStatus] = useState<OrderStatus | 'All'>('All')
  const [query, setQuery] = useState('')
  const [sort, setSort] = useState<QueueSort>('newest')
  const [onlyNeedsAction, setOnlyNeedsAction] = useState(false)
  const [page, setPage] = useState(1)
  const [orders, setOrders] = useState<OrderResponse[]>([])
  const [counts, setCounts] = useState<Partial<Record<OrderStatus, number>>>({})
  const [state, setState] = useState<LoadState>({ kind: 'loading' })

  const load = () => {
    setState({ kind: 'loading' })
    ordersApi
      .list({ status: status === 'All' ? undefined : status, page: 1, size: FETCH_SIZE })
      .then((result) => {
        setOrders(result.items)
        setState({ kind: 'ready' })
      })
      .catch((err: unknown) => {
        const message = err instanceof ApiError ? err.message : 'We couldn’t retrieve the order information.'
        setState({ kind: 'error', message })
      })
  }

  useEffect(() => {
    load()
    // load() is redefined every render but only its listed dependencies
    // should trigger a refetch.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status])

  // Overview counts: one cheap request per status (page size 1, totalCount only).
  useEffect(() => {
    let cancelled = false
    Promise.all(
      OVERVIEW.map((o) =>
        ordersApi
          .list({ status: o.status, page: 1, size: 1 })
          .then((r) => [o.status, r.totalCount] as const)
          .catch(() => [o.status, undefined] as const),
      ),
    ).then((entries) => {
      if (!cancelled) setCounts(Object.fromEntries(entries.filter(([, n]) => n !== undefined)))
    })
    return () => {
      cancelled = true
    }
  }, [orders])

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    const now = Date.now()
    const matching = orders.filter(
      (o) =>
        (!onlyNeedsAction || needsAction(o, now)) &&
        (!q ||
          [o.cropName, o.buyerName, o.farmerName, o.collectionCentreName, o.regionName, o.id]
            .filter(Boolean)
            .some((v) => String(v).toLowerCase().includes(q))),
    )
    return sortOrders(matching, sort)
  }, [orders, query, sort, onlyNeedsAction])

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE))
  const safePage = Math.min(page, totalPages)
  const pageItems = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE)

  return (
    <div>
      <PageHeader title="Orders" description="Review, approve and schedule the orders for your collection centre" />

      <div className="order-overview" role="group" aria-label="Order overview">
        {OVERVIEW.map((o) => (
          <button
            key={o.status}
            type="button"
            className={`order-overview-card${status === o.status ? ' active' : ''}`}
            onClick={() => {
              setStatus(status === o.status ? 'All' : o.status)
              setPage(1)
            }}
          >
            <span className="order-overview-count">{counts[o.status] ?? '–'}</span>
            <span className="order-overview-label">{o.label}</span>
            <span className="order-overview-hint">{o.hint}</span>
          </button>
        ))}
      </div>

      <div className="order-queue-filters">
        <label>
          Status
          <select
            value={status}
            onChange={(e) => {
              setStatus(e.target.value as OrderStatus | 'All')
              setPage(1)
            }}
          >
            {STATUS_OPTIONS.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>
        <label>
          Sort by
          <select
            value={sort}
            onChange={(e) => {
              setSort(e.target.value as QueueSort)
              setPage(1)
            }}
          >
            {(Object.keys(QUEUE_SORT_LABELS) as QueueSort[]).map((option) => (
              <option key={option} value={option}>
                {QUEUE_SORT_LABELS[option]}
              </option>
            ))}
          </select>
        </label>
        <label className="order-queue-toggle">
          <span>Show</span>
          <span className="order-queue-toggle-row">
            <input
              type="checkbox"
              checked={onlyNeedsAction}
              onChange={(e) => {
                setOnlyNeedsAction(e.target.checked)
                setPage(1)
              }}
            />
            Needs action only
          </span>
        </label>
        <label className="order-queue-search">
          Search
          <input
            type="search"
            value={query}
            placeholder="Crop, buyer, farmer, centre or order id"
            onChange={(e) => {
              setQuery(e.target.value)
              setPage(1)
            }}
          />
        </label>
      </div>

      <Card>
        {state.kind === 'loading' && <LoadingState label="Loading orders…" />}

        {state.kind === 'error' && <ErrorState message={state.message} onRetry={load} />}

        {state.kind === 'ready' && filtered.length === 0 && (
          <EmptyState
            title="No Orders Found"
            description={
              status === 'All' && !query && !onlyNeedsAction
                ? 'Orders placed for your centre will appear here.'
                : 'There are no orders matching your current filters.'
            }
          />
        )}

        {state.kind === 'ready' && filtered.length > 0 && (
          <>
            <OrderTable orders={pageItems} />
            <div className="order-queue-pagination">
              <button type="button" disabled={safePage <= 1} onClick={() => setPage(safePage - 1)}>
                Previous
              </button>
              <span>
                Page {safePage} of {totalPages} · {filtered.length} order{filtered.length === 1 ? '' : 's'}
              </span>
              <button type="button" disabled={safePage >= totalPages} onClick={() => setPage(safePage + 1)}>
                Next
              </button>
            </div>
          </>
        )}
      </Card>
    </div>
  )
}
