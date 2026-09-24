import { useEffect, useState } from 'react'
import { useAuth } from '../../context/AuthContext'
import { OrderTable } from '../../components/orders/OrderTable'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/StateViews'
import { Card } from '../../components/ui/Card'
import { ApiError, ordersApi, type OrderResponse, type OrderStatus } from '../../utils/ordersApi'
import './OrderQueuePage.css'

const STATUS_OPTIONS: (OrderStatus | 'All')[] = ['All', 'Pending', 'Approved', 'Scheduled', 'Completed', 'Cancelled']
const PAGE_SIZE = 10

type LoadState = { kind: 'loading' } | { kind: 'error'; message: string } | { kind: 'ready' }

/** Design.md §30 — centre-scoped (per role) order list, filterable by status, paginated. */
export function OrderQueuePage() {
  const { identity } = useAuth()
  const [status, setStatus] = useState<OrderStatus | 'All'>('All')
  const [page, setPage] = useState(1)
  const [orders, setOrders] = useState<OrderResponse[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [state, setState] = useState<LoadState>({ kind: 'loading' })

  const load = () => {
    setState({ kind: 'loading' })
    ordersApi
      .list(identity, { status: status === 'All' ? undefined : status, page, size: PAGE_SIZE })
      .then((result) => {
        setOrders(result.items)
        setTotalCount(result.totalCount)
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
  }, [identity, status, page])

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))

  return (
    <div>
      <PageHeader title="Orders" description="Manage and track agricultural orders" />

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
      </div>

      <Card>
        {state.kind === 'loading' && <LoadingState label="Loading orders…" />}

        {state.kind === 'error' && <ErrorState message={state.message} onRetry={load} />}

        {state.kind === 'ready' && orders.length === 0 && (
          <EmptyState
            title="No Orders Found"
            description={
              status === 'All'
                ? 'Orders placed by buyers will appear here.'
                : 'There are no orders matching your current filters.'
            }
          />
        )}

        {state.kind === 'ready' && orders.length > 0 && (
          <>
            <OrderTable orders={orders} />
            <div className="order-queue-pagination">
              <button type="button" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                Previous
              </button>
              <span>
                Page {page} of {totalPages}
              </span>
              <button type="button" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
                Next
              </button>
            </div>
          </>
        )}
      </Card>
    </div>
  )
}
