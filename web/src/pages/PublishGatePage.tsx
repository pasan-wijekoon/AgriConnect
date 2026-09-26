import React, { useState } from 'react';
import {
  ShieldCheck,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Send,
  Eye,
  Lock,
  Unlock,
  Check
} from 'lucide-react';
import { Card } from '../components/ui/Card';
import { Button } from '../components/ui/Button';
import { GradeBadge, Badge } from '../components/ui/Badge';
import { ConfirmDialog } from '../components/ui/Modal';
import { Table } from '../components/ui/Table';
import type { Column } from '../components/ui/Table';
import { SearchFilterBar } from '../components/ui/SearchBar';
import { Toast } from '../components/ui/Toast';
import type { ListingSummary } from '../types/inspection';

export interface PublishGatePageProps {
  listings: ListingSummary[];
  onPublishListing: (listingId: string) => Promise<void>;
  onInspectListing?: (listing: ListingSummary) => void;
  onViewHistory: (listingId: string) => void;
}

export const PublishGatePage: React.FC<PublishGatePageProps> = ({
  listings,
  onPublishListing,
  onViewHistory
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedListingToPublish, setSelectedListingToPublish] = useState<ListingSummary | null>(null);
  const [isPublishing, setIsPublishing] = useState(false);
  const [toastMsg, setToastMsg] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const filtered = listings.filter((l) =>
    l.cropName.toLowerCase().includes(searchTerm.toLowerCase()) ||
    l.farmerName.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const handlePublishConfirm = async () => {
    if (!selectedListingToPublish) return;
    try {
      setIsPublishing(true);
      await onPublishListing(selectedListingToPublish.id);
      setToastMsg({
        type: 'success',
        text: `Listing for ${selectedListingToPublish.cropName} (${selectedListingToPublish.quantity}${selectedListingToPublish.unit}) successfully published to buyer marketplace!`
      });
      setSelectedListingToPublish(null);
    } catch (err: any) {
      setToastMsg({ type: 'error', text: err.message || 'Failed to publish listing.' });
      setSelectedListingToPublish(null);
    } finally {
      setIsPublishing(false);
    }
  };

  const columns: Column<ListingSummary>[] = [
    {
      header: 'Produce Listing',
      render: (row) => (
        <div>
          <div style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{row.cropName}</div>
          <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
            {row.quantity} {row.unit} • {row.farmerName} ({row.regionName})
          </div>
        </div>
      )
    },
    {
      header: 'Quality Inspection',
      render: (row) => {
        if (row.inspectionCount === 0) {
          return (
            <Badge variant="error" icon={<XCircle size={12} />}>
              Not Inspected
            </Badge>
          );
        }
        return (
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
            <GradeBadge grade={row.latestConfirmedGrade || 'N/A'} />
            <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>({row.inspectionCount} check)</span>
          </div>
        );
      }
    },
    {
      header: 'FR14 Discrepancy',
      render: (row) => {
        if (row.hasUnresolvedDiscrepancy) {
          return (
            <Badge variant="warning" icon={<AlertTriangle size={12} />}>
              Unresolved Mismatch
            </Badge>
          );
        }
        return (
          <Badge variant="success" icon={<Check size={12} />}>
            Cleared
          </Badge>
        );
      }
    },
    {
      header: 'Publication Gate Status (FR5)',
      render: (row) => {
        const isPublished = row.status === 'Published';
        const isInspected = row.inspectionCount > 0;
        const isGradeValid = isInspected && row.latestConfirmedGrade !== 'Rejected';
        const isDiscrepancyClean = !row.hasUnresolvedDiscrepancy;
        const canPublish = isInspected && isGradeValid && isDiscrepancyClean;

        if (isPublished) {
          return (
            <Badge variant="success" icon={<CheckCircle2 size={12} />}>
              Active in Marketplace
            </Badge>
          );
        }

        if (canPublish) {
          return (
            <Badge variant="info" icon={<Unlock size={12} />}>
              Ready for Publication
            </Badge>
          );
        }

        return (
          <Badge variant="error" icon={<Lock size={12} />}>
            Gate Blocked
          </Badge>
        );
      }
    },
    {
      header: 'Actions',
      align: 'right',
      render: (row) => {
        const isPublished = row.status === 'Published';
        const isInspected = row.inspectionCount > 0;
        const isGradeValid = isInspected && row.latestConfirmedGrade !== 'Rejected';
        const isDiscrepancyClean = !row.hasUnresolvedDiscrepancy;
        const canPublish = isInspected && isGradeValid && isDiscrepancyClean;

        return (
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px' }}>
            {!isPublished ? (
              <Button
                variant={canPublish ? 'primary' : 'outline'}
                size="sm"
                disabled={!canPublish}
                onClick={() => setSelectedListingToPublish(row)}
                icon={<Send size={14} />}
              >
                Approve & Publish
              </Button>
            ) : (
              <Button
                variant="outline"
                size="sm"
                onClick={() => onViewHistory(row.id)}
                icon={<Eye size={14} />}
              >
                Audit Log
              </Button>
            )}
          </div>
        );
      }
    }
  ];

  return (
    <div>
      {/* Page Header */}
      <div style={{ marginBottom: '24px' }}>
        <h1 style={{ fontSize: '24px', fontWeight: 700, color: 'var(--text-primary)' }}>
          Marketplace Publication Gate (FR5)
        </h1>
        <p style={{ fontSize: '14px', color: 'var(--text-secondary)', marginTop: '4px' }}>
          Strict enforcement gate: listings become visible to buyers only after passing quality verification and receiving officer approval.
        </p>
      </div>

      {toastMsg && (
        <div style={{ marginBottom: '20px' }}>
          <Toast
            type={toastMsg.type}
            message={toastMsg.text}
            onClose={() => setToastMsg(null)}
          />
        </div>
      )}

      {/* FR5 Compliance Gate Criteria Card */}
      <div
        style={{
          backgroundColor: '#FFFFFF',
          border: '1px solid var(--border-color)',
          borderRadius: 'var(--radius-md)',
          padding: '20px',
          marginBottom: '24px',
          boxShadow: 'var(--shadow-card)'
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '12px' }}>
          <ShieldCheck size={20} color="var(--primary-green)" />
          <h3 style={{ fontSize: '16px', fontWeight: 600, color: 'var(--text-primary)', margin: 0 }}>
            Verification Gate Rules
          </h3>
        </div>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))',
            gap: '16px',
            fontSize: '13px',
            color: 'var(--text-secondary)'
          }}
        >
          <div style={{ display: 'flex', alignItems: 'flex-start', gap: '8px' }}>
            <CheckCircle2 size={16} color="var(--primary-green)" style={{ flexShrink: 0, marginTop: '2px' }} />
            <div>
              <strong style={{ color: 'var(--text-primary)', display: 'block' }}>1. Physical Inspection</strong>
              A recorded physical inspection by an authorized collection centre officer must exist.
            </div>
          </div>
          <div style={{ display: 'flex', alignItems: 'flex-start', gap: '8px' }}>
            <CheckCircle2 size={16} color="var(--primary-green)" style={{ flexShrink: 0, marginTop: '2px' }} />
            <div>
              <strong style={{ color: 'var(--text-primary)', display: 'block' }}>2. Acceptable Grade</strong>
              Confirmed grade must be Grade A, B, or C (cannot be Rejected/Unfit).
            </div>
          </div>
          <div style={{ display: 'flex', alignItems: 'flex-start', gap: '8px' }}>
            <CheckCircle2 size={16} color="var(--primary-green)" style={{ flexShrink: 0, marginTop: '2px' }} />
            <div>
              <strong style={{ color: 'var(--text-primary)', display: 'block' }}>3. Discrepancy Reconciliation</strong>
              Any claimed-vs-confirmed grade mismatch (FR14) must be formally resolved.
            </div>
          </div>
        </div>
      </div>

      <SearchFilterBar
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        placeholder="Search listings by produce name or farmer..."
        onReset={() => setSearchTerm('')}
      />

      {/* Listings Gate Table */}
      <Card
        title="Marketplace Gate Evaluation Table"
        subtitle="Listings evaluated in real-time against FR5 gate criteria"
      >
        <Table
          columns={columns}
          data={filtered}
          keyExtractor={(row) => row.id}
          emptyMessage="No listings found matching filter."
        />
      </Card>

      {/* Publish Confirmation Dialog */}
      {selectedListingToPublish && (
        <ConfirmDialog
          isOpen={!!selectedListingToPublish}
          onClose={() => setSelectedListingToPublish(null)}
          onConfirm={handlePublishConfirm}
          title={`Publish ${selectedListingToPublish.cropName}?`}
          message={`Are you sure you want to publish this listing (${selectedListingToPublish.quantity}${selectedListingToPublish.unit}, confirmed as ${selectedListingToPublish.latestConfirmedGrade}) to the public buyer marketplace? Once published, registered buyers will be able to search, view, and place orders against this batch.`}
          confirmLabel="Authorize & Publish"
          isLoading={isPublishing}
        />
      )}
    </div>
  );
};
