import { API_BASE_URL, request } from './ordersApi'

/**
 * Typed client for Component C's Quality Grading & Inspection endpoints
 * (FR5/FR12–FR14). Mirrors ordersApi.ts's conventions exactly (same
 * X-Dev-Role/X-Dev-UserId auth, same request()/ApiError) — one client, not a
 * per-page reimplementation (CLAUDE.md §22). Replaces this page set's original
 * services/api.ts, which called a mocked X-Officer-Id header and fell back to
 * in-memory mock data on any failure; that mock fallback is deliberately not
 * kept, so a real backend error surfaces as a real error, not a masked demo.
 */

export interface InspectionPhoto {
  id: string
  url: string
  uploadedAt: string
}

export interface InspectionResponse {
  id: string
  listingId: string
  cropName: string
  quantity: number
  unit: string
  claimedGrade: string
  confirmedGrade: string
  notes?: string
  inspectedAt: string
  officerId: string
  officerName: string
  farmerName: string
  regionName: string
  listingStatus: string
  hasDiscrepancy: boolean
  photos: InspectionPhoto[]
}

export interface ListingSummary {
  id: string
  farmerId: string
  farmerName: string
  farmerPhone: string
  cropId: string
  cropName: string
  category: string
  regionId: string
  regionName: string
  quantity: number
  unit: string
  claimedGrade: string
  latestConfirmedGrade?: string
  pickupWindowStart: string
  pickupWindowEnd: string
  status: string
  minPrice?: number
  createdAt: string
  inspectionCount: number
  hasUnresolvedDiscrepancy: boolean
  listingPhotos: string[]
}

export interface GradeDiscrepancy {
  id: string
  listingId: string
  cropName: string
  farmerName: string
  quantity: number
  unit: string
  claimedGrade: string
  confirmedGrade: string
  flaggedAt: string
  resolvedAt?: string
  resolutionNotes?: string
  resolvedByOfficerName?: string
  isResolved: boolean
}

export interface QualityDashboardStats {
  pendingInspections: number
  completedToday: number
  totalInspections: number
  activeDiscrepancies: number
  publishedListings: number
  gradeAComplianceRate: number
}

export interface InspectionPagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface AgentQualityValidation {
  passed: boolean
  failedChecks: string[]
  flags: string[]
  gradeConfidence: number
  assessedGrade: string
  reasoningSummary: string
  recommendedAction: string
  toolCallLog: Array<{ tool: string }>
  workflowId: string
}

export const QUALITY_GRADES = ['Grade A', 'Grade B', 'Grade C', 'Rejected'] as const

async function get<T>(path: string): Promise<T> {
  return request<T>(path)
}

export const qualityApi = {
  getDashboardStats: () => get<QualityDashboardStats>('/api/inspections/stats'),

  getPendingListings: () => get<ListingSummary[]>('/api/listings/pending-inspection'),

  getInspections: (params: { search?: string; grade?: string; page?: number } = {}) => {
    const qs = new URLSearchParams()
    if (params.search) qs.set('search', params.search)
    if (params.grade) qs.set('grade', params.grade)
    qs.set('page', String(params.page ?? 1))
    return get<InspectionPagedResult<InspectionResponse>>(`/api/inspections?${qs.toString()}`)
  },

  getListingInspections: (listingId: string) => get<InspectionResponse[]>(`/api/listings/${listingId}/inspections`),

  recordInspection: (
    payload: { listingId: string; confirmedGrade: string; notes?: string; photoUrls: string[] },
  ) =>
    request<InspectionResponse>('/api/inspections', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),

  getDiscrepancies: (onlyUnresolved = true) => get<GradeDiscrepancy[]>(`/api/inspections/discrepancies?onlyUnresolved=${onlyUnresolved}`),

  resolveDiscrepancy: (flagId: string, resolutionNotes: string) => request<GradeDiscrepancy>(`/api/inspections/discrepancies/${flagId}/resolve`, {
      method: 'POST',
      body: JSON.stringify({ resolutionNotes }),
    }),

  publishListing: (listingId: string) => request<ListingSummary>(`/api/listings/${listingId}/publish`, { method: 'POST' }),

  evaluateListingCompliance: (listingId: string) => request<AgentQualityValidation>(`/api/listings/${listingId}/evaluate-compliance`, { method: 'POST' }),
}

export { API_BASE_URL }
