import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import { Card } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { PageHeader } from '../../components/ui/PageHeader'
import { ErrorState, LoadingState } from '../../components/ui/StateViews'
import { OrderStatusBadge } from '../../components/orders/OrderStatusBadge'
import { ScheduleProposalReview } from '../../components/orders/ScheduleProposalReview'
import {
  ApiError,
  ordersApi,
  type OrderResponse,
  type OrderStatus,
  type ScheduleResponse,
} from '../../utils/ordersApi'
import './OrderDetailPage.css'

// Mirrors the backend's OrderService allow-list (backend/src/services/OrderService.cs) —
// only the transitions an Officer can trigger directly from this screen.
// Approved -> Scheduled happens through the schedule-approval flow instead.
const OFFICER_STATUS_ACTIONS: Partial<Record<OrderStatus, { label: string; next: OrderStatus }[]>> = {
  Pending: [{ label: 'Approve Order', next: 'Approved' }],
  Scheduled: [{ label: 'Mark Completed', next: 'Completed' }],
}

const TIMELINE_STAGES: OrderStatus[] = ['Pending', 'Approved', 'Scheduled', 'Completed']

function OrderTimeline({ status }: { status: OrderStatus }) {
  if (status === 'Cancelled') {
    return <p className="order-timeline-cancelled">This order was cancelled.</p>
  }
  const currentIndex = TIMELINE_STAGES.indexOf(status)
  return (
    <ol className="order-timeline">
      {TIMELINE_STAGES.map((stage, index) => (
        <li key={stage} className={index <= currentIndex ? 'reached' : ''}>
          {stage}
        </li>
      ))}
    </ol>
  )
}

export function OrderDetailPage() {
  const { orderId } = useParams<{ orderId: string }>()
  const navigate = useNavigate()
  const { identity } = useAuth()

  const [order, setOrder] = useState<OrderResponse | null>(null)
  const [schedule, setSchedule] = useState<ScheduleResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionBusy, setActionBusy] = useState(false)

  const load = async () => {
    if (!orderId) return
    setLoading(true)
    setError(null)
    try {
      const fetchedOrder = await ordersApi.getById(identity, orderId)
      setOrder(fetchedOrder)
      try {
        const fetchedSchedule = await ordersApi.getSchedule(identity, orderId)
        setSchedule(fetchedSchedule)
      } catch (err) {
        // No schedule yet is expected and not an error — anything else is.
        if (err instanceof ApiError && err.status === 404) {
          setSchedule(null)
        } else {
          throw err
        }
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "We couldn't retrieve the order information.")
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [orderId, identity])

  if (loading) return <LoadingState label="Loading order…" />
  if (error) return <ErrorState message={error} onRetry={load} />
  if (!order) return <ErrorState message="Order not found." />

  const officerActions = identity.role === 'Officer' ? OFFICER_STATUS_ACTIONS[order.status] : undefined
  const canCancel =
    (identity.role === 'Buyer' && !['Scheduled', 'Completed', 'Cancelled'].includes(order.status)) ||
    (identity.role === 'Officer' && !['Completed', 'Cancelled'].includes(order.status))
  // No schedule yet, or the previous one was rejected (Cancelled) — either way
  // there's no Proposed/Confirmed schedule blocking a fresh proposal.
  const canProposeSchedule =
    identity.role === 'Officer' && order.status === 'Approved' && (!schedule || schedule.status === 'Cancelled')

  const runAction = async (action: () => Promise<unknown>) => {
    setActionBusy(true)
    try {
      await action()
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Action failed. Please try again.')
    } finally {
      setActionBusy(false)
    }
  }

  return (
    <div>
      <PageHeader
        title={`Order #${order.id.slice(0, 8)}`}
        description="Full order, schedule, and status details"
        action={
          <Button variant="secondary" onClick={() => navigate('/orders')}>
            Back to Orders
          </Button>
        }
      />

      <div className="order-detail-grid">
        <Card>
          <div className="order-detail-status-row">
            <OrderStatusBadge status={order.status} />
          </div>

          <dl className="order-detail-fields">
            <div>
              <dt>Quantity</dt>
              <dd>{order.quantity} kg</dd>
            </div>
            <div>
              <dt>Delivery Preference</dt>
              <dd>{order.deliveryPreference}</dd>
            </div>
            <div>
              <dt>Placed</dt>
              <dd>{new Date(order.createdAt).toLocaleString()}</dd>
            </div>
            <div>
              <dt>Last Updated</dt>
              <dd>{new Date(order.updatedAt).toLocaleString()}</dd>
            </div>
            {order.reservationExpiresAt && (
              <div>
                <dt>Reservation Expires</dt>
                <dd>{new Date(order.reservationExpiresAt).toLocaleString()}</dd>
              </div>
            )}
          </dl>

          <div className="order-detail-actions">
            {officerActions?.map((action) => (
              <Button
                key={action.next}
                loading={actionBusy}
                onClick={() => runAction(() => ordersApi.updateStatus(identity, order.id, action.next))}
              >
                {action.label}
              </Button>
            ))}
            {canCancel && (
              <Button
                variant="destructive"
                loading={actionBusy}
                onClick={() => runAction(() => ordersApi.cancel(identity, order.id))}
              >
                Cancel Order
              </Button>
            )}
            {canProposeSchedule && (
              <Button
                variant="secondary"
                loading={actionBusy}
                onClick={() => runAction(() => ordersApi.proposeSchedule(identity, order.id, {}))}
              >
                Propose Schedule
              </Button>
            )}
          </div>
        </Card>

        <Card>
          <h3 className="order-detail-section-title">Order Timeline</h3>
          <OrderTimeline status={order.status} />
        </Card>

        {schedule && (
          <Card>
            <ScheduleProposalReview
              schedule={schedule}
              busy={actionBusy}
              onApprove={() => runAction(() => ordersApi.decideSchedule(identity, order.id, 'Approve'))}
              onReject={() => runAction(() => ordersApi.decideSchedule(identity, order.id, 'Reject'))}
            />
          </Card>
        )}
      </div>
    </div>
  )
}
