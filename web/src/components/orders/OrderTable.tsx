import { Link } from 'react-router-dom'
import type { OrderResponse } from '../../utils/ordersApi'
import { orderFlag } from '../../utils/orderQueue'
import { OrderStatusBadge } from './OrderStatusBadge'
import './OrderTable.css'

/**
 * Design.md §19/§30 — a table on desktop, order cards on mobile. Same markup
 * both ways (data-label attributes drive the mobile layout via CSS), so there
 * is exactly one place that renders an order row, not two.
 */
export function OrderTable({ orders }: { orders: OrderResponse[] }) {
  const now = Date.now()
  return (
    <table className="order-table">
      <thead>
        <tr>
          <th>Order</th>
          <th>Produce</th>
          <th>Buyer</th>
          <th>Pickup</th>
          <th>Status</th>
          <th>Placed</th>
          <th aria-label="Actions" />
        </tr>
      </thead>
      <tbody>
        {orders.map((order) => (
          <tr key={order.id}>
            <td data-label="Order">#{order.id.slice(0, 8)}</td>
            <td data-label="Produce">
              <strong>{order.cropName ?? '—'}</strong>
              <div className="order-table-sub">
                {Number(order.quantity.toFixed(2))} {order.unit ?? 'kg'} · {order.deliveryPreference}
              </div>
            </td>
            <td data-label="Buyer">{order.buyerName ?? '—'}</td>
            <td data-label="Pickup">
              {order.slotStart ? (
                <>
                  {new Date(order.slotStart).toLocaleString(undefined, { day: 'numeric', month: 'short', hour: 'numeric', minute: '2-digit' })}
                  {order.scheduleStatus === 'Proposed' && <span className="order-table-tag"> proposed</span>}
                  {order.collectionCentreName && <div className="order-table-sub">{order.collectionCentreName}</div>}
                </>
              ) : (
                <span className="order-table-sub">{order.status === 'Approved' ? 'Needs a slot' : '—'}</span>
              )}
            </td>
            <td data-label="Status">
              <OrderStatusBadge status={order.status} />
              {(() => {
                const flag = orderFlag(order, now)
                return flag && <span className={`order-table-flag order-table-flag-${flag.tone}`}>{flag.label}</span>
              })()}
            </td>
            <td data-label="Placed">{new Date(order.createdAt).toLocaleDateString()}</td>
            <td data-label="" className="order-table-actions">
              <Link to={`/orders/${order.id}`}>Review</Link>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
