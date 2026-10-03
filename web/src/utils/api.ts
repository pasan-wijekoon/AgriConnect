import { authHeaders } from '../context/session.ts'
import type { AnomalyStatus } from './anomalies.ts'
import { networkError, toApiError } from './problem.ts'
import type { NamedItem, Severity, ShortageEvent, SupplyType } from './shortages.ts'
import type { Bucket, TrendPoint } from './trend.ts'

export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000').replace(/\/$/, '')

// ---- Response shapes (documentation/API_Contract.md) --------------------------------

export interface AnalyticsFilters {
  crops: (NamedItem & { hasPriceHistory?: boolean })[]
  regions: NamedItem[]
}

export interface PriceTrendResponse {
  cropId: string
  regionId: string | null
  bucket: Bucket
  points: TrendPoint[]
}

export interface AnomalyFlag {
  id: string
  listingId: string
  cropId: string
  regionId: string
  listingPrice: number
  deviationPercent: number
  flaggedAt: string
  status: AnomalyStatus
}

export interface Paged<T> {
  items: T[]
  page: number
  size: number
  total: number
}

export interface ExternalContext {
  available: boolean
  reason: string | null
}

export interface Investigation {
  listingId: string
  flag: { deviationPercent: number; flaggedAt: string; status: AnomalyStatus }
  priceContext: { regionalAvgPrice: number | null; listingPrice: number; percentileInRegion: number }
  inspectionContext: ExternalContext
  orderContext: ExternalContext
  likelyCauses: { cause: string; confidence: 'High' | 'Medium' | 'Low'; explanation: string }[]
}

export type ReportType = 'PriceTrends' | 'Listings' | 'Orders'

export interface ReportExport {
  id: string
  type: ReportType
  generatedAt: string
  fileUrl: string
  dateRangeStart?: string
  dateRangeEnd?: string
}

export interface ReportPage { items: ReportExport[]; page: number; size: number; total: number }

export interface SnapshotRefresh {
  periodsProcessed: number
  snapshotsUpserted: number
}

// ---- Transport -----------------------------------------------------------------------

type Query = Record<string, string | number | null | undefined>

async function request<T>(method: string, path: string, options: { query?: Query; body?: unknown } = {}): Promise<T> {
  const url = new URL(`${API_BASE_URL}${path}`)
  for (const [key, value] of Object.entries(options.query ?? {})) {
    if (value !== null && value !== undefined && value !== '') url.searchParams.set(key, String(value))
  }

  let response: Response
  try {
    response = await fetch(url, {
      method,
      headers: {
        Accept: 'application/json',
        ...(options.body === undefined ? {} : { 'Content-Type': 'application/json' }),
        ...authHeaders(),
      },
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
    })
  } catch {
    throw networkError()
  }

  const text = await response.text()
  let data: unknown = null
  try {
    data = text ? JSON.parse(text) : null
  } catch {
    // Non-JSON body (e.g. a proxy error page): handled as a plain status below.
  }
  if (!response.ok) throw toApiError(response.status, data)
  return data as T
}

// ---- Endpoints -------------------------------------------------------------------------

export const getFilters = () => request<AnalyticsFilters>('GET', '/api/analytics/filters')

export const getPriceTrends = (query: { cropId: string; regionId?: string | null; from: string; to: string; bucket: Bucket }) =>
  request<PriceTrendResponse>('GET', '/api/analytics/price-trends', { query })

export const getAnomalies = (query: { status?: AnomalyStatus | null; cropId?: string | null; page: number; size: number }) =>
  request<Paged<AnomalyFlag>>('GET', '/api/analytics/anomalies', { query })

export const investigateListing = (listingId: string) =>
  request<Investigation>('GET', `/api/analytics/anomalies/${encodeURIComponent(listingId)}/investigate`)

export const updateAnomalyStatus = (id: string, status: 'Reviewed' | 'Dismissed') =>
  request<AnomalyFlag>('PATCH', `/api/analytics/anomalies/${encodeURIComponent(id)}`, { body: { status } })

export const getShortages = (query: { type?: SupplyType | null; severity?: Severity | null }) =>
  request<{ items: ShortageEvent[] }>('GET', '/api/analytics/shortages', { query })

export const refreshSnapshots = () => request<SnapshotRefresh>('POST', '/api/analytics/snapshots/refresh')

export const exportReport = (body: { type: ReportType; dateRangeStart: string; dateRangeEnd: string }) =>
  request<ReportExport>('POST', '/api/reports/export', { body })

export const getReport = (id: string) => request<ReportExport>('GET', `/api/reports/${encodeURIComponent(id)}`)
export const getReports = (query: { page?: number; size?: number } = {}) =>
  request<ReportPage>('GET', '/api/reports', { query })

export const downloadUrl = (fileUrl: string) => `${API_BASE_URL}${fileUrl}`

export interface SchedulingPreviewRequest {
  centreId: string
  preferredWindow: { start: string; end: string }
  existingBookings: { slotStart: string; slotEnd: string }[]
}

export interface SchedulingPreview {
  proposedSlotStart: string
  proposedSlotEnd: string
  conflictChecked: boolean
  reasoning: string
}

/** Asks the Logistics Scheduling Agent (via the API) for a slot. Preview only; nothing is saved. */
export const previewSchedule = (body: SchedulingPreviewRequest) =>
  request<SchedulingPreview>('POST', '/api/analytics/scheduling-preview', { body })
