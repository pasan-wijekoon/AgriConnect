import type { DevIdentity } from '../context/AuthContext'

/**
 * Typed client for Component B's Order/Scheduling/CollectionCentre endpoints
 * (plan §5.2 + the schedule-decision route added in Phase 6). One client, not
 * duplicated per page (CLAUDE.md §22).
 */

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'

export type OrderStatus = 'Pending' | 'Approved' | 'Scheduled' | 'Completed' | 'Cancelled'
export type DeliveryPreference = 'Pickup' | 'Delivery'
export type ScheduleStatus = 'Proposed' | 'Confirmed' | 'Cancelled'
export type ScheduleDecision = 'Approve' | 'Reject'

export interface OrderResponse {
  id: string
  listingId: string
  buyerId: string
  quantity: number
  status: OrderStatus
  deliveryPreference: DeliveryPreference
  createdAt: string
  updatedAt: string
  reservationExpiresAt: string | null
}

export interface PagedResult<T> {
  items: T[]
  page: number
  size: number
  totalCount: number
}

export interface CreateOrderRequest {
  listingId: string
  quantity: number
  deliveryPreference: DeliveryPreference
}

export interface ScheduleResponse {
  id: string
  orderId: string
  collectionCentreId: string
  slotStart: string
  slotEnd: string
  status: ScheduleStatus
  conflictChecked: boolean
}

export interface CreateScheduleRequest {
  collectionCentreId?: string
  preferredWindow?: { start: string; end: string }
}

export interface NearestCentreResponse {
  centreId: string
  name: string
  distanceKm: number
  etaMinutes: number | null
  capacity: number
  degraded: boolean
}

export interface CollectionCentreResponse {
  id: string
  name: string
  latitude: number
  longitude: number
  capacity: number
  regionId: string
}

/** Thrown for any non-2xx response; carries the RFC 7807 ProblemDetails body when present. */
export class ApiError extends Error {
  status: number
  detail?: string

  constructor(status: number, message: string, detail?: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.detail = detail
  }
}

async function request<T>(identity: DevIdentity, path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      'X-Dev-Role': identity.role,
      'X-Dev-UserId': identity.userId,
      ...init?.headers,
    },
  })

  if (!response.ok) {
    let detail: string | undefined
    try {
      const body = (await response.json()) as { detail?: string; title?: string }
      detail = body.detail ?? body.title
    } catch {
      // Response body wasn't JSON (or was empty) — fall through with no detail.
    }
    throw new ApiError(response.status, detail ?? `Request failed with status ${response.status}.`, detail)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export const ordersApi = {
  create: (identity: DevIdentity, body: CreateOrderRequest) =>
    request<OrderResponse>(identity, '/api/orders', { method: 'POST', body: JSON.stringify(body) }),

  list: (identity: DevIdentity, params: { status?: OrderStatus; page?: number; size?: number } = {}) => {
    const query = new URLSearchParams()
    if (params.status) query.set('status', params.status)
    query.set('page', String(params.page ?? 1))
    query.set('size', String(params.size ?? 20))
    return request<PagedResult<OrderResponse>>(identity, `/api/orders?${query.toString()}`)
  },

  getById: (identity: DevIdentity, id: string) => request<OrderResponse>(identity, `/api/orders/${id}`),

  updateStatus: (identity: DevIdentity, id: string, status: OrderStatus) =>
    request<OrderResponse>(identity, `/api/orders/${id}/status`, {
      method: 'PUT',
      body: JSON.stringify({ status }),
    }),

  cancel: (identity: DevIdentity, id: string, reason?: string) =>
    request<OrderResponse>(identity, `/api/orders/${id}/cancel`, {
      method: 'POST',
      body: JSON.stringify({ reason }),
    }),

  getSchedule: (identity: DevIdentity, orderId: string) =>
    request<ScheduleResponse>(identity, `/api/orders/${orderId}/schedule`),

  proposeSchedule: (identity: DevIdentity, orderId: string, body: CreateScheduleRequest) =>
    request<ScheduleResponse>(identity, `/api/orders/${orderId}/schedule`, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  decideSchedule: (identity: DevIdentity, orderId: string, decision: ScheduleDecision) =>
    request<ScheduleResponse>(identity, `/api/orders/${orderId}/schedule/decision`, {
      method: 'PUT',
      body: JSON.stringify({ decision }),
    }),

  nearestCentres: (identity: DevIdentity, lat: number, lng: number, regionId?: string) => {
    const query = new URLSearchParams({ lat: String(lat), lng: String(lng) })
    if (regionId) query.set('regionId', regionId)
    return request<NearestCentreResponse[]>(identity, `/api/collection-centres/nearest?${query.toString()}`)
  },

  listCentres: (identity: DevIdentity) =>
    request<CollectionCentreResponse[]>(identity, '/api/collection-centres'),

  listCentreSchedules: (identity: DevIdentity, centreId: string) =>
    request<ScheduleResponse[]>(identity, `/api/collection-centres/${centreId}/schedules`),
}
