import { useEffect, useRef, useState } from 'react'
import { ApiError, ordersApi, type OrderActivityItem } from '../../utils/ordersApi'
import './OrderActivity.css'

interface OrderActivityProps {
  orderId: string
  /** Changes whenever the order/schedule changes, so the timeline reloads. */
  refreshKey?: string
  /** Gives the loaded entries to the parent (e.g. to show "why this centre"). */
  onLoaded?: (items: OrderActivityItem[]) => void
}

type State = { kind: 'loading' } | { kind: 'error'; message: string } | { kind: 'ready'; items: OrderActivityItem[] }

const fmt = (iso: string) =>
  new Date(iso).toLocaleString(undefined, { day: 'numeric', month: 'short', hour: 'numeric', minute: '2-digit' })

/** FR11/FR20 — who did what to this order, oldest first. */
export function OrderActivity({ orderId, refreshKey, onLoaded }: OrderActivityProps) {
  const [state, setState] = useState<State>({ kind: 'loading' })
  const [attempt, setAttempt] = useState(0)
  const onLoadedRef = useRef(onLoaded)
  onLoadedRef.current = onLoaded

  useEffect(() => {
    let cancelled = false
    setState({ kind: 'loading' })
    ordersApi
      .activity(orderId)
      .then((items) => {
        if (cancelled) return
        setState({ kind: 'ready', items })
        onLoadedRef.current?.(items)
      })
      .catch((err: unknown) => {
        if (cancelled) return
        setState({ kind: 'error', message: err instanceof ApiError ? err.message : 'We couldn’t load the activity.' })
      })
    return () => {
      cancelled = true
    }
  }, [orderId, refreshKey, attempt])

  if (state.kind === 'loading') {
    return <p className="order-activity-state" role="status">Loading activity…</p>
  }

  if (state.kind === 'error') {
    return (
      <p className="order-activity-state" role="alert">
        {state.message}
        <button type="button" onClick={() => setAttempt((n) => n + 1)}>Retry</button>
      </p>
    )
  }

  if (state.items.length === 0) {
    return <p className="order-activity-state">No activity recorded yet.</p>
  }

  return (
    <ol className="order-activity">
      {state.items.map((item, index) => (
        <li key={`${item.timestamp}-${index}`}>
          {item.summary}
          {item.explanation && <span className="order-activity-why">{item.explanation}</span>}
          <span className="order-activity-meta">
            {fmt(item.timestamp)} · {item.actorName}
          </span>
        </li>
      ))}
    </ol>
  )
}
