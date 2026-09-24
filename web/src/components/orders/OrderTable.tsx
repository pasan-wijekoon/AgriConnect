import { Link } from 'react-router-dom'
import type { OrderResponse } from '../../utils/ordersApi'
import { OrderStatusBadge } from './OrderStatusBadge'
import './OrderTable.css'

/**
 * Design.md §19/§30 — a table on desktop, order cards on mobile. Same markup
 * both ways (data-label attributes drive the mobile layout via CSS), so there
 * is exactly one place that renders an order row, not two.
 */
export function OrderTable({ orders }: { orders: OrderResponse[] }) {
  return (
    <table className="order-table">
      <thead>
        <tr>
          <th>Order</th>
          <th>Quantity</th>
          <th>Delivery</th>
          <th>Status</th>
          <th>Placed</th>
          <th aria-label="Actions" />
        </tr>
      </thead>
      <tbody>
        {orders.map((order) => (
          <tr key={order.id}>
            <td data-label="Order">#{order.id.slice(0, 8)}</td>
            <td data-label="Quantity">{order.quantity} kg</td>
            <td data-label="Delivery">{order.deliveryPreference}</td>
            <td data-label="Status">
              <OrderStatusBadge status={order.status} />
            </td>
            <td data-label="Placed">{new Date(order.createdAt).toLocaleDateString()}</td>
            <td data-label="" className="order-table-actions">
              <Link to={`/orders/${order.id}`}>View Details</Link>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
