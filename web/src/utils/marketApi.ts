// AgriConnect Web Dashboard - API Client (Expanded with Auth)

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

// ── Types ─────────────────────────────────────────────────────

export interface User {
  id: string;
  fullName: string;
  email: string;
  role: 'Farmer' | 'Buyer' | 'Admin';
  phone?: string;
  region?: string;
  avatarUrl?: string;
  token: string;
}

export function resolveImageUrl(url?: string | null): string {
  const fallback = 'https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=800&auto=format&fit=crop';
  if (!url || !url.trim()) return fallback;
  const trimmed = url.trim();
  if (trimmed.startsWith('http://') || trimmed.startsWith('https://') || trimmed.startsWith('data:')) {
    return trimmed;
  }
  if (trimmed.startsWith('/')) {
    return trimmed;
  }
  return `/${trimmed}`;
}

export interface UserProfile {
  id: string;
  fullName: string;
  email: string;
  role: string;
  phone?: string;
  region?: string;
  avatarUrl?: string;
  createdAt: string;
}

export interface Crop {
  id: string;
  name: string;
  category: string;
}

export interface Region {
  id: string;
  name: string;
  collectionCentreId?: string;
}

export interface Photo {
  id: string;
  url: string;
  uploadedAt: string;
}

export interface PriceSuggestion {
  id: string;
  listingId: string;
  suggestedPriceMin: number;
  suggestedPriceMax: number;
  confidence: number;
  reasoningSummary: string;
  status: 'Proposed' | 'Approved' | 'Rejected' | 'Revised';
  checkpointName?: string;
  decidedByUserId?: string;
  decidedAt?: string;
  officerNote?: string;
  createdAt: string;
}

export interface TodayPriceItem {
  cropId: string;
  name: string;
  category: string;
  unit: string;
  region: string;
  grade: string;
  suggestedPriceMin: number;
  suggestedPriceMax: number;
  averagePrice: number;
  confidence: number;
  change24h: number;
  trend: 'rising' | 'falling' | 'stable';
  imageUrl: string;
  reasoning: string;
  benchmarkWholesale: number;
}

export interface TodayPriceCatalogItem {
  id: string;
  name: string;
  category: string;
  unit: string;
  defaultRegion: string;
  imageUrl?: string;
  displayOrder: number;
  isActive: boolean;
}

export interface TodayPricesResponse {
  date: string;
  totalCrops: number;
  selectedGrade: string;
  selectedRegion: string;
  marketStatus: string;
  items: TodayPriceItem[];
}

export interface PriceEstimateResult {
  crop: string;
  region: string;
  grade: string;
  suggestedPriceMin: number;
  suggestedPriceMax: number;
  averagePrice: number;
  confidence: number;
  reasoningSummary: string;
  benchmarkWholesale: number;
}

export interface Listing {
  id: string;
  farmerId: string;
  cropName: string;
  cropCategory: string;
  regionName: string;
  quantity: number;
  unit: string;
  claimedGrade: string;
  pickupWindowStart: string;
  pickupWindowEnd: string;
  status: 'Draft' | 'PendingApproval' | 'Published' | 'Withdrawn' | 'SoldOut';
  minPrice?: number;
  description?: string;
  createdAt: string;
  updatedAt: string;
  photos: Photo[];
  priceSuggestion?: PriceSuggestion;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ListingFilters {
  cropId?: string;
  regionId?: string;
  grade?: string;
  status?: string;
  minPrice?: number;
  maxPrice?: number;
  search?: string;
  sortBy?: string;
  sortDir?: string;
  page?: number;
  pageSize?: number;
}

// ── Auth Token Helper ─────────────────────────────────────────

function getAuthToken(): string | null {
  const stored = localStorage.getItem('agriconnect_user');
  if (stored) {
    try {
      const user = JSON.parse(stored);
      return user.token;
    } catch { return null; }
  }
  return null;
}

// ── Request Helper ────────────────────────────────────────────

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const token = getAuthToken();
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
    ...options?.headers as Record<string, string>,
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
  });

  if (!res.ok) {
    const errorText = await res.text();
    throw new Error(`API error ${res.status}: ${errorText || res.statusText}`);
  }

  if (res.status === 204) {
    return {} as T;
  }

  return res.json();
}

// ── API ───────────────────────────────────────────────────────

export const api = {
  // Auth
  login: (email: string, password: string) =>
    request<User>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),

  register: (data: {
    fullName: string;
    email: string;
    password: string;
    role: string;
    phone?: string;
    region?: string;
  }) =>
    request<User>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  getMe: () => request<UserProfile>('/auth/me'),

  // Reference data
  getCrops: () => request<Crop[]>('/crops'),
  getRegions: () => request<Region[]>('/regions'),

  // Listings
  getListings: (filters: ListingFilters = {}) => {
    const params = new URLSearchParams();
    if (filters.cropId) params.append('cropId', filters.cropId);
    if (filters.regionId) params.append('regionId', filters.regionId);
    if (filters.grade) params.append('grade', filters.grade);
    if (filters.status) params.append('status', filters.status);
    if (filters.minPrice !== undefined) params.append('minPrice', filters.minPrice.toString());
    if (filters.maxPrice !== undefined) params.append('maxPrice', filters.maxPrice.toString());
    if (filters.search) params.append('search', filters.search);
    if (filters.sortBy) params.append('sortBy', filters.sortBy);
    if (filters.sortDir) params.append('sortDir', filters.sortDir);
    if (filters.page) params.append('page', filters.page.toString());
    if (filters.pageSize) params.append('pageSize', filters.pageSize.toString());

    return request<PagedResult<Listing>>(`/listings?${params.toString()}`);
  },

  getMyListings: (filters: ListingFilters = {}) => {
    const params = new URLSearchParams();
    if (filters.status) params.append('status', filters.status);
    if (filters.search) params.append('search', filters.search);
    if (filters.sortBy) params.append('sortBy', filters.sortBy);
    if (filters.sortDir) params.append('sortDir', filters.sortDir);
    if (filters.page) params.append('page', filters.page.toString());
    if (filters.pageSize) params.append('pageSize', filters.pageSize.toString());

    return request<PagedResult<Listing>>(`/listings/my?${params.toString()}`);
  },

  getListingById: (id: string) => request<Listing>(`/listings/${id}`),

  createListing: (data: {
    cropId: string;
    regionId: string;
    quantity: number;
    unit: string;
    claimedGrade: string;
    pickupWindowStart: string;
    pickupWindowEnd: string;
    minPrice?: number;
    description?: string;
    photoUrls: string[];
  }) =>
    request<Listing>('/listings', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  updateListing: (id: string, data: {
    cropId?: string;
    regionId?: string;
    quantity?: number;
    unit?: string;
    claimedGrade?: string;
    pickupWindowStart?: string;
    pickupWindowEnd?: string;
    minPrice?: number;
    description?: string;
    photoUrls?: string[];
  }) =>
    request<Listing>(`/listings/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),

  // Business-specific endpoint: get or trigger AI Fair-Price Suggestion
  getPriceSuggestion: (listingId: string) =>
    request<PriceSuggestion>(`/listings/${listingId}/price-suggestion`),

  // Withdraw listing (FR7)
  withdrawListing: (id: string) =>
    request<void>(`/listings/${id}`, { method: 'DELETE' }),

  // Admin actions
  approveListing: (id: string) =>
    request<Listing>(`/listings/${id}/approve`, { method: 'PATCH' }),

  rejectListing: (id: string) =>
    request<Listing>(`/listings/${id}/reject`, { method: 'PATCH' }),

  // Officer decision on the AI-suggested price itself (FR: Approve / Reject / Request Revision)
  approvePriceSuggestion: (listingId: string) =>
    request<PriceSuggestion>(`/listings/${listingId}/price-suggestion/approve`, { method: 'PATCH' }),

  rejectPriceSuggestion: (listingId: string, officerNote?: string) =>
    request<PriceSuggestion>(`/listings/${listingId}/price-suggestion/reject`, {
      method: 'PATCH',
      body: JSON.stringify({ officerNote }),
    }),

  revisePriceSuggestion: (listingId: string, revisedPriceMin: number, revisedPriceMax: number, officerNote?: string) =>
    request<PriceSuggestion>(`/listings/${listingId}/price-suggestion/revise`, {
      method: 'PATCH',
      body: JSON.stringify({ revisedPriceMin, revisedPriceMax, officerNote }),
    }),

  // Photos
  addPhotos: (listingId: string, photoUrls: string[]) =>
    request<Photo[]>(`/listings/${listingId}/photos`, {
      method: 'POST',
      body: JSON.stringify({ photoUrls }),
    }),

  // Upload photo from device
  uploadPhoto: async (file: File): Promise<string> => {
    try {
      const formData = new FormData();
      formData.append('file', file);
      const res = await fetch(`${API_BASE}/upload`, {
        method: 'POST',
        body: formData,
      });
      if (res.ok) {
        const data = await res.json();
        if (data.url) {
          return data.url.startsWith('/') ? `${API_BASE.replace('/api', '')}${data.url}` : data.url;
        }
      }
    } catch (err) {
      console.warn('Backend photo upload unavailable, falling back to local base64:', err);
    }
    return new Promise<string>((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result as string);
      reader.onerror = reject;
      reader.readAsDataURL(file);
    });
  },

  // Today Prices Discovery (Component A Agentic AI)
  getTodayPrices: (region?: string, grade: string = 'A'): Promise<TodayPricesResponse> => {
    const params = new URLSearchParams();
    if (region && region !== 'All') params.append('region', region);
    if (grade) params.append('grade', grade);
    return request<TodayPricesResponse>(`/prices/today?${params.toString()}`);
  },

  // Live Quick Price Estimation (Farmer Add Modal)
  getQuickPriceEstimate: (params: {
    cropId: string;
    regionId: string;
    cropName?: string;
    regionName?: string;
    grade?: string;
    quantity?: number;
  }): Promise<PriceEstimateResult> => {
    const q = new URLSearchParams();
    q.append('cropId', params.cropId);
    q.append('regionId', params.regionId);
    if (params.cropName) q.append('cropName', params.cropName);
    if (params.regionName) q.append('regionName', params.regionName);
    if (params.grade) q.append('grade', params.grade);
    if (params.quantity) q.append('quantity', params.quantity.toString());
    return request<PriceEstimateResult>(`/prices/estimate?${q.toString()}`);
  },

  // Run LangGraph StateGraph Orchestration
  runOrchestration: (data: {
    objectiveText: string;
    triggerType?: string;
    triggerEntityId?: string;
    listingContext: Record<string, unknown>;
  }): Promise<Record<string, unknown>> =>
    request<Record<string, unknown>>('/prices/orchestrate', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  // Admin: Today's Prices catalog management (which crops appear on the
  // discovery page — prices themselves are always computed live)
  getTodayPriceCatalog: () =>
    request<TodayPriceCatalogItem[]>('/admin/today-prices-catalog'),

  createTodayPriceCatalogItem: (data: {
    name: string;
    category: string;
    unit?: string;
    defaultRegion: string;
    imageUrl?: string;
    displayOrder?: number;
  }) =>
    request<TodayPriceCatalogItem>('/admin/today-prices-catalog', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  updateTodayPriceCatalogItem: (id: string, data: {
    name?: string;
    category?: string;
    unit?: string;
    defaultRegion?: string;
    imageUrl?: string;
    displayOrder?: number;
    isActive?: boolean;
  }) =>
    request<TodayPriceCatalogItem>(`/admin/today-prices-catalog/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),

  deleteTodayPriceCatalogItem: (id: string) =>
    request<void>(`/admin/today-prices-catalog/${id}`, { method: 'DELETE' }),
};
