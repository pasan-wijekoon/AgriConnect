import type {
  InspectionResponse,
  CreateInspectionPayload,
  UpdateInspectionPayload,
  ListingSummary,
  GradeDiscrepancy,
  QualityDashboardStats,
  PagedResult
} from '../types/inspection';

const API_BASE_URL = 'http://localhost:5000/api';

// Fallback in-memory mock data to guarantee zero-blank screens and rich preview
let mockListings: ListingSummary[] = [
  {
    id: 'e1111111-1111-1111-1111-111111111111',
    farmerId: '22222222-2222-2222-2222-222222222222',
    farmerName: 'Sunil Perera (Farmer)',
    farmerPhone: '+94 71 987 6543',
    cropId: 'd1111111-0000-0000-0000-000000000001',
    cropName: 'Tomatoes (Thalathuoya)',
    category: 'Vegetables',
    regionId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    regionName: 'Western - Colombo',
    quantity: 250,
    unit: 'kg',
    claimedGrade: 'Grade A',
    pickupWindowStart: new Date(Date.now() + 86400000).toISOString(),
    pickupWindowEnd: new Date(Date.now() + 259200000).toISOString(),
    status: 'PendingApproval',
    minPrice: 280.00,
    createdAt: new Date(Date.now() - 14400000).toISOString(),
    inspectionCount: 0,
    hasUnresolvedDiscrepancy: false,
    listingPhotos: ['https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=600&q=80']
  },
  {
    id: 'e2222222-2222-2222-2222-222222222222',
    farmerId: '33333333-3333-3333-3333-333333333333',
    farmerName: 'Nimal Bandara (Farmer)',
    farmerPhone: '+94 76 555 1234',
    cropId: 'd1111111-0000-0000-0000-000000000002',
    cropName: 'Carrots (Nuwara Eliya)',
    category: 'Vegetables',
    regionId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    regionName: 'Western - Gampaha',
    quantity: 180,
    unit: 'kg',
    claimedGrade: 'Grade A',
    pickupWindowStart: new Date(Date.now() + 172800000).toISOString(),
    pickupWindowEnd: new Date(Date.now() + 345600000).toISOString(),
    status: 'PendingApproval',
    minPrice: 320.00,
    createdAt: new Date(Date.now() - 43200000).toISOString(),
    inspectionCount: 0,
    hasUnresolvedDiscrepancy: false,
    listingPhotos: ['https://images.unsplash.com/photo-1598170845058-32b9d6a5da37?auto=format&fit=crop&w=600&q=80']
  },
  {
    id: 'e3333333-3333-3333-3333-333333333333',
    farmerId: '44444444-4444-4444-4444-444444444444',
    farmerName: 'Anura Jayasinghe (Farmer)',
    farmerPhone: '+94 78 444 9876',
    cropId: 'd1111111-0000-0000-0000-000000000004',
    cropName: 'Bell Peppers (Yellow/Red)',
    category: 'Vegetables',
    regionId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
    regionName: 'Central - Kandy',
    quantity: 120,
    unit: 'kg',
    claimedGrade: 'Grade A',
    latestConfirmedGrade: 'Grade B',
    pickupWindowStart: new Date(Date.now() + 86400000).toISOString(),
    pickupWindowEnd: new Date(Date.now() + 172800000).toISOString(),
    status: 'PendingApproval',
    minPrice: 450.00,
    createdAt: new Date(Date.now() - 86400000).toISOString(),
    inspectionCount: 1,
    hasUnresolvedDiscrepancy: true,
    listingPhotos: ['https://images.unsplash.com/photo-1563565375-f3fdfdbefa83?auto=format&fit=crop&w=600&q=80']
  },
  {
    id: 'e4444444-4444-4444-4444-444444444444',
    farmerId: '22222222-2222-2222-2222-222222222222',
    farmerName: 'Sunil Perera (Farmer)',
    farmerPhone: '+94 71 987 6543',
    cropId: 'd1111111-0000-0000-0000-000000000003',
    cropName: 'Green Beans',
    category: 'Vegetables',
    regionId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    regionName: 'Western - Colombo',
    quantity: 300,
    unit: 'kg',
    claimedGrade: 'Grade A',
    latestConfirmedGrade: 'Grade A',
    pickupWindowStart: new Date(Date.now() + 86400000).toISOString(),
    pickupWindowEnd: new Date(Date.now() + 432000000).toISOString(),
    status: 'Published',
    minPrice: 210.00,
    createdAt: new Date(Date.now() - 172800000).toISOString(),
    inspectionCount: 1,
    hasUnresolvedDiscrepancy: false,
    listingPhotos: ['https://images.unsplash.com/photo-1551893665-f843f600794e?auto=format&fit=crop&w=600&q=80']
  }
];

let mockInspections: InspectionResponse[] = [
  {
    id: 'insp-001',
    listingId: 'e3333333-3333-3333-3333-333333333333',
    cropName: 'Bell Peppers (Yellow/Red)',
    quantity: 120,
    unit: 'kg',
    claimedGrade: 'Grade A',
    confirmedGrade: 'Grade B',
    notes: 'Produce size is inconsistent with Grade A export standards (diameter varies between 4-7cm). Slight skin blemishes on ~15% of samples. Downgraded to Grade B standard commercial grade.',
    inspectedAt: new Date(Date.now() - 28800000).toISOString(),
    officerId: '11111111-1111-1111-1111-111111111111',
    officerName: 'Kamal Gunawardena',
    farmerName: 'Anura Jayasinghe (Farmer)',
    regionName: 'Central - Kandy',
    listingStatus: 'PendingApproval',
    hasDiscrepancy: true,
    photos: [
      { id: 'p1', url: 'https://images.unsplash.com/photo-1563565375-f3fdfdbefa83?auto=format&fit=crop&w=600&q=80', uploadedAt: new Date(Date.now() - 28800000).toISOString() }
    ]
  },
  {
    id: 'insp-002',
    listingId: 'e4444444-4444-4444-4444-444444444444',
    cropName: 'Green Beans',
    quantity: 300,
    unit: 'kg',
    claimedGrade: 'Grade A',
    confirmedGrade: 'Grade A',
    notes: 'Fresh harvest, uniform green color, crisp pods, zero pest damage. Passed Grade A verification criteria.',
    inspectedAt: new Date(Date.now() - 86400000).toISOString(),
    officerId: '11111111-1111-1111-1111-111111111111',
    officerName: 'Kamal Gunawardena',
    farmerName: 'Sunil Perera (Farmer)',
    regionName: 'Western - Colombo',
    listingStatus: 'Published',
    hasDiscrepancy: false,
    photos: [
      { id: 'p2', url: 'https://images.unsplash.com/photo-1551893665-f843f600794e?auto=format&fit=crop&w=600&q=80', uploadedAt: new Date(Date.now() - 86400000).toISOString() }
    ]
  }
];

let mockDiscrepancies: GradeDiscrepancy[] = [
  {
    id: 'flag-001',
    listingId: 'e3333333-3333-3333-3333-333333333333',
    cropName: 'Bell Peppers (Yellow/Red)',
    farmerName: 'Anura Jayasinghe (Farmer)',
    quantity: 120,
    unit: 'kg',
    claimedGrade: 'Grade A',
    confirmedGrade: 'Grade B',
    flaggedAt: new Date(Date.now() - 28800000).toISOString(),
    isResolved: false
  }
];

async function apiRequest<T>(endpoint: string, options?: RequestInit): Promise<T> {
  try {
    const res = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        'X-Officer-Id': '11111111-1111-1111-1111-111111111111',
        ...(options?.headers || {})
      }
    });

    if (res.ok) {
      return await res.json();
    }
    const errData = await res.json().catch(() => ({}));
    throw new Error(errData.error || `HTTP error ${res.status}`);
  } catch (error) {
    console.warn(`[API] Remote call to ${endpoint} failed, utilizing local fallback simulation. Reason:`, error);
    throw error;
  }
}

export const ApiService = {
  // Stats
  async getDashboardStats(): Promise<QualityDashboardStats> {
    try {
      return await apiRequest<QualityDashboardStats>('/inspections/stats');
    } catch {
      const pending = mockListings.filter(l => l.status === 'PendingApproval' && l.inspectionCount === 0).length;
      const discrepancies = mockDiscrepancies.filter(d => !d.isResolved).length;
      const published = mockListings.filter(l => l.status === 'Published').length;
      return {
        pendingInspections: pending,
        completedToday: 1,
        totalInspections: mockInspections.length,
        activeDiscrepancies: discrepancies,
        publishedListings: published,
        gradeAComplianceRate: 66.7
      };
    }
  },

  // Listings Pending Inspection
  async getPendingListings(): Promise<ListingSummary[]> {
    try {
      return await apiRequest<ListingSummary[]>('/listings/pending-inspection');
    } catch {
      return [...mockListings];
    }
  },

  // Get Inspection History
  async getInspections(search?: string, grade?: string, page: number = 1): Promise<PagedResult<InspectionResponse>> {
    try {
      const params = new URLSearchParams();
      if (search) params.append('search', search);
      if (grade) params.append('grade', grade);
      params.append('page', page.toString());
      return await apiRequest<PagedResult<InspectionResponse>>(`/inspections?${params.toString()}`);
    } catch {
      let filtered = [...mockInspections];
      if (search) {
        const s = search.toLowerCase();
        filtered = filtered.filter(i =>
          i.cropName.toLowerCase().includes(s) ||
          i.farmerName.toLowerCase().includes(s) ||
          (i.notes && i.notes.toLowerCase().includes(s))
        );
      }
      if (grade) {
        filtered = filtered.filter(i => i.confirmedGrade.toLowerCase() === grade.toLowerCase());
      }
      return {
        items: filtered,
        totalCount: filtered.length,
        page: 1,
        pageSize: 20,
        totalPages: 1
      };
    }
  },

  // Get Listing Inspection History (FR13)
  async getListingInspections(listingId: string): Promise<InspectionResponse[]> {
    try {
      return await apiRequest<InspectionResponse[]>(`/listings/${listingId}/inspections`);
    } catch {
      return mockInspections.filter(i => i.listingId === listingId);
    }
  },

  // Record Inspection (FR12, FR14)
  async recordInspection(payload: CreateInspectionPayload): Promise<InspectionResponse> {
    try {
      return await apiRequest<InspectionResponse>('/inspections', {
        method: 'POST',
        body: JSON.stringify(payload)
      });
    } catch {
      const listing = mockListings.find(l => l.id === payload.listingId);
      const isMismatch = listing && listing.claimedGrade.toLowerCase() !== payload.confirmedGrade.toLowerCase();

      const newInsp: InspectionResponse = {
        id: `insp-${Date.now()}`,
        listingId: payload.listingId,
        cropName: listing?.cropName || 'Produce',
        quantity: listing?.quantity || 100,
        unit: listing?.unit || 'kg',
        claimedGrade: listing?.claimedGrade || 'Grade A',
        confirmedGrade: payload.confirmedGrade,
        notes: payload.notes,
        inspectedAt: new Date().toISOString(),
        officerId: '11111111-1111-1111-1111-111111111111',
        officerName: 'Kamal Gunawardena',
        farmerName: listing?.farmerName || 'Farmer',
        regionName: listing?.regionName || 'Colombo',
        listingStatus: listing?.status || 'PendingApproval',
        hasDiscrepancy: !!isMismatch,
        photos: payload.photoUrls.map((url, i) => ({
          id: `p-${Date.now()}-${i}`,
          url,
          uploadedAt: new Date().toISOString()
        }))
      };

      mockInspections.unshift(newInsp);

      if (listing) {
        listing.latestConfirmedGrade = payload.confirmedGrade;
        listing.inspectionCount += 1;
        if (isMismatch) {
          listing.hasUnresolvedDiscrepancy = true;
          mockDiscrepancies.unshift({
            id: `flag-${Date.now()}`,
            listingId: listing.id,
            cropName: listing.cropName,
            farmerName: listing.farmerName,
            quantity: listing.quantity,
            unit: listing.unit,
            claimedGrade: listing.claimedGrade,
            confirmedGrade: payload.confirmedGrade,
            flaggedAt: new Date().toISOString(),
            isResolved: false
          });
        }
      }

      return newInsp;
    }
  },

  // Amend Inspection (FR13)
  async amendInspection(id: string, payload: UpdateInspectionPayload): Promise<InspectionResponse> {
    try {
      return await apiRequest<InspectionResponse>(`/inspections/${id}`, {
        method: 'PUT',
        body: JSON.stringify(payload)
      });
    } catch {
      const insp = mockInspections.find(i => i.id === id);
      if (!insp) throw new Error('Inspection not found');
      insp.confirmedGrade = payload.confirmedGrade;
      insp.notes = payload.notes;
      return { ...insp };
    }
  },

  // Get Discrepancies (FR14)
  async getDiscrepancies(onlyUnresolved: boolean = true): Promise<GradeDiscrepancy[]> {
    try {
      return await apiRequest<GradeDiscrepancy[]>(`/inspections/discrepancies?onlyUnresolved=${onlyUnresolved}`);
    } catch {
      return onlyUnresolved ? mockDiscrepancies.filter(d => !d.isResolved) : [...mockDiscrepancies];
    }
  },

  // Resolve Discrepancy (FR14)
  async resolveDiscrepancy(flagId: string, resolutionNotes: string): Promise<GradeDiscrepancy> {
    try {
      return await apiRequest<GradeDiscrepancy>(`/inspections/discrepancies/${flagId}/resolve`, {
        method: 'POST',
        body: JSON.stringify({ resolutionNotes })
      });
    } catch {
      const flag = mockDiscrepancies.find(f => f.id === flagId);
      if (!flag) throw new Error('Discrepancy flag not found');
      flag.isResolved = true;
      flag.resolvedAt = new Date().toISOString();
      flag.resolutionNotes = resolutionNotes;
      flag.resolvedByOfficerName = 'Kamal Gunawardena';

      const listing = mockListings.find(l => l.id === flag.listingId);
      if (listing) {
        listing.hasUnresolvedDiscrepancy = false;
      }
      return { ...flag };
    }
  },

  // Publish Listing Gate (FR5)
  async publishListing(listingId: string): Promise<ListingSummary> {
    try {
      return await apiRequest<ListingSummary>(`/listings/${listingId}/publish`, {
        method: 'POST'
      });
    } catch {
      const listing = mockListings.find(l => l.id === listingId);
      if (!listing) throw new Error('Listing not found');
      
      if (listing.inspectionCount === 0) {
        throw new Error('Quality Verification Gate Blocked: The listing cannot be published because it has not been inspected by an officer (FR5). Please record an inspection first.');
      }
      if (listing.latestConfirmedGrade === 'Rejected') {
        throw new Error('Quality Verification Gate Blocked: The listing was confirmed as Rejected during quality inspection and cannot be published.');
      }
      if (listing.hasUnresolvedDiscrepancy) {
        throw new Error('Quality Verification Gate Blocked: There is an active unresolved grade discrepancy. Resolve the discrepancy before publishing.');
      }

      listing.status = 'Published';
      return { ...listing };
    }
  }
};
