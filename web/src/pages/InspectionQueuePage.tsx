import React, { useState } from 'react';
import {
  ClipboardCheck,
  CheckCircle2,
  AlertTriangle,
  FileCheck2,
  Calendar,
  User,
  MapPin,
  Eye
} from 'lucide-react';
import { Card, MetricCard } from '../components/ui/Card';
import { Button } from '../components/ui/Button';
import { GradeBadge, Badge } from '../components/ui/Badge';
import { Table } from '../components/ui/Table';
import type { Column } from '../components/ui/Table';
import { SearchFilterBar } from '../components/ui/SearchBar';
import type { ListingSummary, QualityDashboardStats } from '../types/inspection';

export interface InspectionQueuePageProps {
  listings: ListingSummary[];
  stats: QualityDashboardStats | null;
  onSelectListingToInspect: (listing: ListingSummary) => void;
  onViewHistory: (listingId: string) => void;
}

export const InspectionQueuePage: React.FC<InspectionQueuePageProps> = ({
  listings,
  stats,
  onSelectListingToInspect,
  onViewHistory
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedGrade, setSelectedGrade] = useState('');

  const pendingListings = listings.filter((l) => {
    const matchesSearch =
      l.cropName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      l.farmerName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      l.regionName.toLowerCase().includes(searchTerm.toLowerCase());

    const matchesGrade = !selectedGrade || l.claimedGrade.toLowerCase() === selectedGrade.toLowerCase();

    return matchesSearch && matchesGrade;
  });

  const columns: Column<ListingSummary>[] = [
    {
      header: 'Produce & Crop',
      render: (row) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          {row.listingPhotos && row.listingPhotos.length > 0 ? (
            <img
              src={row.listingPhotos[0]}
              alt={row.cropName}
              style={{
                width: '44px',
                height: '44px',
                borderRadius: 'var(--radius-sm)',
                objectFit: 'cover',
                border: '1px solid var(--border-color)'
              }}
            />
          ) : (
            <div
              style={{
                width: '44px',
                height: '44px',
                borderRadius: 'var(--radius-sm)',
                backgroundColor: 'var(--primary-green-light)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'var(--primary-green)',
                fontWeight: 600
              }}
            >
              {row.cropName.slice(0, 2)}
            </div>
          )}
          <div>
            <div style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{row.cropName}</div>
            <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
              Category: {row.category} • {row.quantity} {row.unit}
            </div>
          </div>
        </div>
      )
    },
    {
      header: 'Farmer Details',
      render: (row) => (
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '4px', fontWeight: 500 }}>
            <User size={14} color="var(--text-secondary)" />
            <span>{row.farmerName}</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '4px', fontSize: '12px', color: 'var(--text-secondary)' }}>
            <MapPin size={12} />
            <span>{row.regionName}</span>
          </div>
        </div>
      )
    },
    {
      header: 'Claimed Grade',
      render: (row) => <GradeBadge grade={row.claimedGrade} />
    },
    {
      header: 'Verification Status',
      render: (row) => {
        if (row.hasUnresolvedDiscrepancy) {
          return (
            <Badge variant="warning" icon={<AlertTriangle size={12} />}>
              Discrepancy Flagged
            </Badge>
          );
        }
        if (row.inspectionCount > 0 && row.latestConfirmedGrade) {
          return (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '2px' }}>
              <Badge variant="success" icon={<CheckCircle2 size={12} />}>
                Inspected: {row.latestConfirmedGrade}
              </Badge>
              {row.status === 'Published' && (
                <span style={{ fontSize: '11px', color: 'var(--status-success-text)', fontWeight: 600 }}>
                  Published to Buyers
                </span>
              )}
            </div>
          );
        }
        return (
          <Badge variant="pending">
            Awaiting Quality Inspection
          </Badge>
        );
      }
    },
    {
      header: 'Pickup Window',
      render: (row) => (
        <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            <Calendar size={12} />
            <span>{new Date(row.pickupWindowStart).toLocaleDateString()}</span>
          </div>
        </div>
      )
    },
    {
      header: 'Actions',
      align: 'right',
      render: (row) => (
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: '8px' }}>
          <Button
            variant="primary"
            size="sm"
            onClick={() => onSelectListingToInspect(row)}
            icon={<ClipboardCheck size={14} />}
          >
            {row.inspectionCount > 0 ? 'Re-Inspect' : 'Inspect'}
          </Button>
          {row.inspectionCount > 0 && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => onViewHistory(row.id)}
              icon={<Eye size={14} />}
            >
              History
            </Button>
          )}
        </div>
      )
    }
  ];

  return (
    <div>
      {/* Page Header */}
      <div style={{ marginBottom: '24px' }}>
        <h1 style={{ fontSize: '24px', fontWeight: 700, color: 'var(--text-primary)' }}>
          Quality Inspection Queue
        </h1>
        <p style={{ fontSize: '14px', color: 'var(--text-secondary)', marginTop: '4px' }}>
          Verify and grade agricultural produce submitted by farmers prior to marketplace publication.
        </p>
      </div>

      {/* Metrics Row */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
          gap: '16px',
          marginBottom: '24px'
        }}
      >
        <MetricCard
          label="Pending Inspections"
          value={stats?.pendingInspections ?? 2}
          subtext="Produce batches awaiting review"
          icon={<ClipboardCheck size={20} />}
        />
        <MetricCard
          label="Inspected Today"
          value={stats?.completedToday ?? 1}
          subtext="Verified by centre officers"
          icon={<CheckCircle2 size={20} />}
        />
        <MetricCard
          label="Active Discrepancies (FR14)"
          value={stats?.activeDiscrepancies ?? 1}
          subtext="Claimed vs confirmed mismatch"
          icon={<AlertTriangle size={20} />}
          trendPositive={false}
        />
        <MetricCard
          label="Published to Buyers (FR5)"
          value={stats?.publishedListings ?? 1}
          subtext="Quality verified & approved"
          icon={<FileCheck2 size={20} />}
          trendPositive={true}
        />
      </div>

      {/* Search & Filters */}
      <SearchFilterBar
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        selectedGrade={selectedGrade}
        onGradeChange={setSelectedGrade}
        placeholder="Search queue by crop, farmer name, or region..."
        onReset={() => {
          setSearchTerm('');
          setSelectedGrade('');
        }}
      />

      {/* Queue Table */}
      <Card
        title={`Pending Produce Batches (${pendingListings.length})`}
        subtitle="Batches require physical inspection and grade confirmation before publication"
      >
        <Table
          columns={columns}
          data={pendingListings}
          keyExtractor={(row) => row.id}
          emptyMessage="No pending inspection batches matching current filter."
        />
      </Card>
    </div>
  );
};
