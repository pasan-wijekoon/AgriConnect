import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Card } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { PageHeader } from '../../components/ui/PageHeader'
import { ErrorState, LoadingState } from '../../components/ui/StateViews'
import { OrderStatusBadge } from '../../components/orders/OrderStatusBadge'
import { ScheduleProposalReview } from '../../components/orders/ScheduleProposalReview'
import { ScheduleWindowModal, type ScheduleWindowValue } from '../../components/orders/ScheduleWindowModal'
import {
  ApiError,
  ordersApi,
  type CollectionCentreResponse,
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

const qty = (n: number) => Number(n.toFixed(2)).toString()

type Dialog = null | 'propose' | 'revise'

/** Officer's single-order view (FR10/FR11): details, status actions, and schedule review. */
export function OrderDetailPage() {
  const { orderId } = useParams<{ orderId: string }>()
  const navigate = useNavigate()

  const [order, setOrder] = useState<OrderResponse | null>(null)
  const [schedule, setSchedule] = useState<ScheduleResponse | null>(null)
  const [centres, setCentres] = useState<CollectionCentreResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [actionBusy, setActionBusy] = useState(false)
  const [dialog, setDialog] = useState<Dialog>(null)
  const [dialogError, setDialogError] = useState<string | null>(null)

  const load = async () => {
    if (!orderId) return
    setLoading(true)
    setError(null)
    try {
      const fetchedOrder = await ordersApi.getById(orderId)
      setOrder(fetchedOrder)
      try {
        setSchedule(await ordersApi.getSchedule(orderId))
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
  }, [orderId])

  // The centre picker only lists the Officer's own centre(s) (server-scoped).
  useEffect(() => {
    ordersApi
      .listCentres()
      .then(setCentres)
      .catch(() => setCentres([]))
  }, [])

  if (loading) return <LoadingState label="Loading order…" />
  if (error) return <ErrorState message={error} onRetry={load} />
  if (!order) return <ErrorState message="Order not found." />

  const officerActions = OFFICER_STATUS_ACTIONS[order.status]
  const canCancel = !['Completed', 'Cancelled'].includes(order.status)
  // No schedule yet, or the previous one was rejected (Cancelled) — either way
  // there's no Proposed/Confirmed schedule blocking a fresh proposal.
  const canProposeSchedule = order.status === 'Approved' && (!schedule || schedule.status === 'Cancelled')

  const runAction = async (action: () => Promise<unknown>, success?: string) => {
    setActionBusy(true)
    setNotice(null)
    try {
      await action()
      await load()
      if (success) setNotice(success)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Action failed. Please try again.')
    } finally {
      setActionBusy(false)
    }
  }

  const submitWindow = async (value: ScheduleWindowValue) => {
    setActionBusy(true)
    setDialogError(null)
    try {
      const window = { start: value.start, end: value.end }
      if (dialog === 'revise') {
        await ordersApi.decideSchedule(order.id, 'RequestRevision', {
          reason: value.reason,
          preferredWindow: window,
        })
      } else {
        await ordersApi.proposeSchedule(order.id, {
          collectionCentreId: value.centreId,
          preferredWindow: window,
        })
      }
      setDialog(null)
      await load()
      setNotice(dialog === 'revise' ? 'A revised slot was proposed — review it below.' : 'A pickup slot was proposed — review it below.')
    } catch (err) {
      setDialogError(err instanceof ApiError ? err.message : 'Could not propose that window. Please try again.')
    } finally {
      setActionBusy(false)
    }
  }

  return (
    <div>
      <PageHeader
        title={`${order.cropName ?? 'Order'} · #${order.id.slice(0, 8)}`}
        description="Full order, schedule, and status details"
        action={
          <Button variant="secondary" onClick={() => navigate('/orders')}>
            Back to Orders
          </Button>
        }
      />

      {notice && (
        <p className="order-notice" role="status">
          {notice}
        </p>
      )}

      <div className="order-detail-grid">
        <Card>
          <div className="order-detail-status-row">
            <OrderStatusBadge status={order.status} />
          </div>

          <dl className="order-detail-fields">
            <div>
              <dt>Produce</dt>
              <dd>
                {order.cropName ?? '—'}
                {order.regionName ? ` · ${order.regionName}` : ''}
              </dd>
            </div>
            <div>
              <dt>Quantity</dt>
              <dd>
                {qty(order.quantity)} {order.unit ?? 'kg'}
              </dd>
            </div>
            <div>
              <dt>Buyer</dt>
              <dd>{order.buyerName ?? order.buyerId}</dd>
            </div>
            <div>
              <dt>Farmer</dt>
              <dd>{order.farmerName ?? '—'}</dd>
            </div>
            <div>
              <dt>Collection centre</dt>
              <dd>{order.collectionCentreName ?? '—'}</dd>
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
                onClick={() =>
                  runAction(
                    () => ordersApi.updateStatus(order.id, action.next),
                    action.next === 'Approved' ? 'Order approved — a pickup slot was proposed automatically. Review it below.' : undefined,
                  )
                }
              >
                {action.label}
              </Button>
            ))}
            {canProposeSchedule && (
              <Button variant="secondary" loading={actionBusy} onClick={() => { setDialogError(null); setDialog('propose') }}>
                Propose Schedule
              </Button>
            )}
            {canCancel && (
              <Button
                variant="destructive"
                loading={actionBusy}
                onClick={() => {
                  if (window.confirm('Cancel this order? Its reserved stock will be released.')) {
                    runAction(() => ordersApi.cancel(order.id))
                  }
                }}
              >
                Cancel Order
              </Button>
            )}
          </div>
          {order.status === 'Pending' && (
            <p className="order-hint">Approving this order automatically proposes a pickup slot for you to review.</p>
          )}
          {canProposeSchedule && (
            <p className="order-hint">No pickup slot is proposed yet. Propose one, choosing the centre and window.</p>
          )}
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
              onApprove={() => runAction(() => ordersApi.decideSchedule(order.id, 'Approve'), 'Schedule confirmed.')}
              onReject={() => runAction(() => ordersApi.decideSchedule(order.id, 'Reject'), 'Proposal rejected. You can propose a new slot.')}
              onRequestRevision={() => { setDialogError(null); setDialog('revise') }}
            />
          </Card>
        )}
      </div>

      {dialog === 'propose' && (
        <ScheduleWindowModal
          title="Propose a pickup slot"
          description="Choose the centre and window. The slot is a proposal you then review and approve."
          submitLabel="Propose slot"
          centres={centres}
          defaultCentreId={order.collectionCentreId ?? undefined}
          busy={actionBusy}
          error={dialogError}
          onSubmit={submitWindow}
          onClose={() => setDialog(null)}
        />
      )}
      {dialog === 'revise' && (
        <ScheduleWindowModal
          title="Request a revision"
          description="Propose a different window at the same centre. The current proposal is replaced."
          submitLabel="Send revision"
          askReason
          busy={actionBusy}
          error={dialogError}
          onSubmit={submitWindow}
          onClose={() => setDialog(null)}
        />
      )}
    </div>
  )
}
