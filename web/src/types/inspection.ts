export interface InspectionPhoto {
  id: string;
  url: string;
  uploadedAt: string;
}

export interface InspectionResponse {
  id: string;
  listingId: string;
  cropName: string;
  quantity: number;
  unit: string;
  claimedGrade: string;
  confirmedGrade: string;
  notes?: string;
  inspectedAt: string;
  officerId: string;
  officerName: string;
  farmerName: string;
  regionName: string;
  listingStatus: string;
  hasDiscrepancy: boolean;
  photos: InspectionPhoto[];
}

export interface CreateInspectionPayload {
  listingId: string;
  confirmedGrade: string;
  notes?: string;
  photoUrls: string[];
}

export interface UpdateInspectionPayload {
  confirmedGrade: string;
  notes?: string;
  photoUrls: string[];
  reasonForAmendment: string;
}

export interface ListingSummary {
  id: string;
  farmerId: string;
  farmerName: string;
  farmerPhone: string;
  cropId: string;
  cropName: string;
  category: string;
  regionId: string;
  regionName: string;
  quantity: number;
  unit: string;
  claimedGrade: string;
  latestConfirmedGrade?: string;
  pickupWindowStart: string;
  pickupWindowEnd: string;
  status: string;
  minPrice?: number;
  createdAt: string;
  inspectionCount: number;
  hasUnresolvedDiscrepancy: boolean;
  listingPhotos: string[];
}

export interface GradeDiscrepancy {
  id: string;
  listingId: string;
  cropName: string;
  farmerName: string;
  quantity: number;
  unit: string;
  claimedGrade: string;
  confirmedGrade: string;
  flaggedAt: string;
  resolvedAt?: string;
  resolutionNotes?: string;
  resolvedByOfficerName?: string;
  isResolved: boolean;
}

export interface QualityDashboardStats {
  pendingInspections: number;
  completedToday: number;
  totalInspections: number;
  activeDiscrepancies: number;
  publishedListings: number;
  gradeAComplianceRate: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
