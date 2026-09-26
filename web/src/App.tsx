import { useState, useEffect, useCallback } from 'react';
import { AppShell } from './components/layout/AppShell';
import type { NavTab } from './components/layout/Sidebar';
import { InspectionQueuePage } from './pages/InspectionQueuePage';
import { RecordInspectionPage } from './pages/RecordInspectionPage';
import { DiscrepancyQueuePage } from './pages/DiscrepancyQueuePage';
import { PublishGatePage } from './pages/PublishGatePage';
import { InspectionHistoryPage } from './pages/InspectionHistoryPage';
import { LoadingState, ErrorState } from './components/ui/StateViews';
import { ApiService } from './services/api';
import type {
  ListingSummary,
  InspectionResponse,
  GradeDiscrepancy,
  QualityDashboardStats,
  CreateInspectionPayload,
  UpdateInspectionPayload
} from './types/inspection';

export function App() {
  const [activeTab, setActiveTab] = useState<NavTab>('queue');
  const [selectedListingForInspection, setSelectedListingForInspection] = useState<ListingSummary | null>(null);

  // Data state
  const [listings, setListings] = useState<ListingSummary[]>([]);
  const [inspections, setInspections] = useState<InspectionResponse[]>([]);
  const [discrepancies, setDiscrepancies] = useState<GradeDiscrepancy[]>([]);
  const [stats, setStats] = useState<QualityDashboardStats | null>(null);

  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const loadAllData = useCallback(async () => {
    try {
      setIsLoading(true);
      setLoadError(null);

      const [statsData, listingsData, inspectionsResult, discrepanciesData] = await Promise.all([
        ApiService.getDashboardStats().catch(() => null),
        ApiService.getPendingListings().catch(() => []),
        ApiService.getInspections().catch(() => ({ items: [] as InspectionResponse[], totalCount: 0, page: 1, pageSize: 20, totalPages: 1 })),
        ApiService.getDiscrepancies(false).catch(() => [])
      ]);

      if (statsData) setStats(statsData);
      setListings(listingsData);
      setInspections(inspectionsResult.items);
      setDiscrepancies(discrepanciesData);
    } catch (err: any) {
      console.error('Failed to load inspection data:', err);
      setLoadError('Failed to connect to data service.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadAllData();
  }, [loadAllData]);

  // Handlers
  const handleSelectListingToInspect = (listing: ListingSummary) => {
    setSelectedListingForInspection(listing);
    setActiveTab('new-inspection');
  };

  const handleRecordInspectionSubmit = async (payload: CreateInspectionPayload) => {
    await ApiService.recordInspection(payload);
    await loadAllData();
    setActiveTab('queue');
    setSelectedListingForInspection(null);
  };

  const handleResolveDiscrepancy = async (flagId: string, notes: string) => {
    await ApiService.resolveDiscrepancy(flagId, notes);
    await loadAllData();
  };

  const handlePublishListing = async (listingId: string) => {
    await ApiService.publishListing(listingId);
    await loadAllData();
  };

  const handleAmendInspection = async (id: string, payload: UpdateInspectionPayload) => {
    await ApiService.amendInspection(id, payload);
    await loadAllData();
  };

  const handleViewHistory = (_listingId?: string) => {
    setActiveTab('history');
  };

  const activeDiscrepanciesCount = discrepancies.filter((d) => !d.isResolved).length;
  const pendingInspectionsCount = listings.filter((l) => l.status === 'PendingApproval' && l.inspectionCount === 0).length;

  return (
    <AppShell
      activeTab={activeTab}
      onTabChange={(tab) => {
        setActiveTab(tab);
        if (tab !== 'new-inspection') {
          setSelectedListingForInspection(null);
        }
      }}
      pendingCount={pendingInspectionsCount}
      discrepancyCount={activeDiscrepanciesCount}
    >
      {isLoading ? (
        <LoadingState message="Connecting to AgriConnect verification service..." />
      ) : loadError ? (
        <ErrorState message={loadError} onRetry={loadAllData} />
      ) : (
        <>
          {activeTab === 'queue' && (
            <InspectionQueuePage
              listings={listings}
              stats={stats}
              onSelectListingToInspect={handleSelectListingToInspect}
              onViewHistory={handleViewHistory}
            />
          )}

          {activeTab === 'new-inspection' && selectedListingForInspection && (
            <RecordInspectionPage
              listing={selectedListingForInspection}
              onBack={() => {
                setActiveTab('queue');
                setSelectedListingForInspection(null);
              }}
              onSubmit={handleRecordInspectionSubmit}
            />
          )}

          {activeTab === 'discrepancies' && (
            <DiscrepancyQueuePage
              discrepancies={discrepancies}
              onResolveDiscrepancy={handleResolveDiscrepancy}
              onInspectListing={(listingId) => {
                const target = listings.find((l) => l.id === listingId);
                if (target) handleSelectListingToInspect(target);
              }}
            />
          )}

          {activeTab === 'publish-gate' && (
            <PublishGatePage
              listings={listings}
              onPublishListing={handlePublishListing}
              onInspectListing={handleSelectListingToInspect}
              onViewHistory={handleViewHistory}
            />
          )}

          {activeTab === 'history' && (
            <InspectionHistoryPage
              inspections={inspections}
              onAmendInspection={handleAmendInspection}
            />
          )}
        </>
      )}
    </AppShell>
  );
}

export default App;
