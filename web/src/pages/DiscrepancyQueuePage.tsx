import React, { useState } from 'react';
import {
  AlertTriangle,
  CheckCircle2,
  ArrowRight,
  ShieldCheck,
  Check
} from 'lucide-react';
import { Card } from '../components/ui/Card';
import { Button } from '../components/ui/Button';
import { GradeBadge, Badge } from '../components/ui/Badge';
import { Modal } from '../components/ui/Modal';
import { Textarea } from '../components/ui/FormControls';
import { Table } from '../components/ui/Table';
import type { Column } from '../components/ui/Table';
import { SearchFilterBar } from '../components/ui/SearchBar';
import { Toast } from '../components/ui/Toast';
import type { GradeDiscrepancy } from '../types/inspection';

export interface DiscrepancyQueuePageProps {
  discrepancies: GradeDiscrepancy[];
  onResolveDiscrepancy: (flagId: string, notes: string) => Promise<void>;
  onInspectListing?: (listingId: string) => void;
}

export const DiscrepancyQueuePage: React.FC<DiscrepancyQueuePageProps> = ({
  discrepancies,
  onResolveDiscrepancy
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [activeTab, setActiveTab] = useState<'open' | 'resolved'>('open');
  const [selectedFlag, setSelectedFlag] = useState<GradeDiscrepancy | null>(null);
  const [resolutionNotes, setResolutionNotes] = useState('');
  const [isResolving, setIsResolving] = useState(false);
  const [toastMsg, setToastMsg] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const filtered = discrepancies.filter((d) => {
    const matchesTab = activeTab === 'open' ? !d.isResolved : d.isResolved;
    const matchesSearch =
      d.cropName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      d.farmerName.toLowerCase().includes(searchTerm.toLowerCase());
    return matchesTab && matchesSearch;
  });

  const handleResolveSubmit = async () => {
    if (!selectedFlag) return;
    if (!resolutionNotes || resolutionNotes.trim().length < 5) {
      setToastMsg({ type: 'error', text: 'Please enter meaningful resolution notes (min 5 chars).' });
      return;
    }

    try {
      setIsResolving(true);
      await onResolveDiscrepancy(selectedFlag.id, resolutionNotes.trim());
      setToastMsg({ type: 'success', text: `Discrepancy for ${selectedFlag.cropName} marked as resolved.` });
      setSelectedFlag(null);
      setResolutionNotes('');
    } catch (err: any) {
      setToastMsg({ type: 'error', text: err.message || 'Failed to resolve discrepancy.' });
    } finally {
      setIsResolving(false);
    }
  };

  const columns: Column<GradeDiscrepancy>[] = [
    {
      header: 'Produce & Farmer',
      render: (row) => (
        <div>
          <div style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{row.cropName}</div>
          <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
            {row.quantity} {row.unit} • Farmer: {row.farmerName}
          </div>
        </div>
      )
    },
    {
      header: 'Claimed vs Confirmed Grade',
      render: (row) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <div>
            <span style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'block' }}>Claimed</span>
            <GradeBadge grade={row.claimedGrade} size="sm" />
          </div>
          <ArrowRight size={14} color="var(--text-muted)" style={{ marginTop: '12px' }} />
          <div>
            <span style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'block' }}>Inspected</span>
            <GradeBadge grade={row.confirmedGrade} size="sm" />
          </div>
        </div>
      )
    },
    {
      header: 'Flagged Date',
      render: (row) => (
        <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
          {new Date(row.flaggedAt).toLocaleString()}
        </div>
      )
    },
    {
      header: 'Resolution Status',
      render: (row) =>
        row.isResolved ? (
          <div>
            <Badge variant="success" icon={<CheckCircle2 size={12} />}>
              Resolved
            </Badge>
            {row.resolvedByOfficerName && (
              <span style={{ display: 'block', fontSize: '11px', color: 'var(--text-muted)', marginTop: '2px' }}>
                By {row.resolvedByOfficerName}
              </span>
            )}
          </div>
        ) : (
          <Badge variant="warning" icon={<AlertTriangle size={12} />}>
            Requires Action
          </Badge>
        )
    },
    {
      header: 'Actions',
      align: 'right',
      render: (row) => (
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px' }}>
          {!row.isResolved ? (
            <Button
              variant="primary"
              size="sm"
              onClick={() => {
                setSelectedFlag(row);
                setResolutionNotes('');
              }}
              icon={<ShieldCheck size={14} />}
            >
              Resolve Flag
            </Button>
          ) : (
            <Button
              variant="outline"
              size="sm"
              onClick={() => setSelectedFlag(row)}
            >
              View Notes
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
          Grade Discrepancy Review Queue (FR14)
        </h1>
        <p style={{ fontSize: '14px', color: 'var(--text-secondary)', marginTop: '4px' }}>
          Review and resolve listings where the farmer's self-claimed grade differs from the officer's physical verification.
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

      {/* Tab Switcher */}
      <div style={{ display: 'flex', gap: '8px', marginBottom: '16px' }}>
        <Button
          variant={activeTab === 'open' ? 'primary' : 'outline'}
          size="sm"
          onClick={() => setActiveTab('open')}
        >
          Active Flags ({discrepancies.filter((d) => !d.isResolved).length})
        </Button>
        <Button
          variant={activeTab === 'resolved' ? 'primary' : 'outline'}
          size="sm"
          onClick={() => setActiveTab('resolved')}
        >
          Resolved Archive ({discrepancies.filter((d) => d.isResolved).length})
        </Button>
      </div>

      {/* Search Bar */}
      <SearchFilterBar
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        placeholder="Filter discrepancy flags by crop or farmer..."
        onReset={() => setSearchTerm('')}
      />

      {/* Discrepancy Table */}
      <Card
        title={activeTab === 'open' ? 'Active Grade Discrepancies' : 'Resolved Grade Discrepancies'}
        subtitle="Each flag requires human officer review and documented resolution notes"
      >
        <Table
          columns={columns}
          data={filtered}
          keyExtractor={(row) => row.id}
          emptyMessage={
            activeTab === 'open'
              ? 'No active grade discrepancies found. All produce inspections match farmer declarations.'
              : 'No resolved discrepancies found in archive.'
          }
        />
      </Card>

      {/* Resolve Discrepancy Modal */}
      {selectedFlag && (
        <Modal
          isOpen={!!selectedFlag}
          onClose={() => setSelectedFlag(null)}
          title={`Grade Discrepancy: ${selectedFlag.cropName}`}
          maxWidth="520px"
          footer={
            selectedFlag.isResolved ? (
              <Button variant="secondary" onClick={() => setSelectedFlag(null)}>
                Close
              </Button>
            ) : (
              <>
                <Button variant="secondary" onClick={() => setSelectedFlag(null)} disabled={isResolving}>
                  Cancel
                </Button>
                <Button
                  variant="primary"
                  onClick={handleResolveSubmit}
                  isLoading={isResolving}
                  icon={<Check size={16} />}
                >
                  Confirm & Resolve Flag
                </Button>
              </>
            )
          }
        >
          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            {/* Comparison Box */}
            <div
              style={{
                backgroundColor: '#F9FAFB',
                padding: '16px',
                borderRadius: 'var(--radius-sm)',
                border: '1px solid var(--border-color)'
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '12px' }}>
                <div>
                  <span style={{ fontSize: '11px', color: 'var(--text-secondary)', display: 'block' }}>Farmer</span>
                  <span style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{selectedFlag.farmerName}</span>
                </div>
                <div>
                  <span style={{ fontSize: '11px', color: 'var(--text-secondary)', display: 'block' }}>Quantity</span>
                  <span style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{selectedFlag.quantity} {selectedFlag.unit}</span>
                </div>
              </div>

              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-around', borderTop: '1px solid var(--border-color-subtle)', paddingTop: '12px' }}>
                <div style={{ textAlign: 'center' }}>
                  <span style={{ fontSize: '12px', color: 'var(--text-secondary)', display: 'block', marginBottom: '4px' }}>
                    Claimed Grade
                  </span>
                  <GradeBadge grade={selectedFlag.claimedGrade} />
                </div>
                <ArrowRight size={18} color="var(--text-muted)" />
                <div style={{ textAlign: 'center' }}>
                  <span style={{ fontSize: '12px', color: 'var(--text-secondary)', display: 'block', marginBottom: '4px' }}>
                    Confirmed Grade
                  </span>
                  <GradeBadge grade={selectedFlag.confirmedGrade} />
                </div>
              </div>
            </div>

            {selectedFlag.isResolved ? (
              <div>
                <span style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-primary)', display: 'block', marginBottom: '4px' }}>
                  Resolution Notes:
                </span>
                <p style={{ fontSize: '14px', color: 'var(--text-secondary)', backgroundColor: '#F3F4F6', padding: '12px', borderRadius: 'var(--radius-sm)' }}>
                  {selectedFlag.resolutionNotes || 'No notes entered.'}
                </p>
                <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                  Resolved on: {selectedFlag.resolvedAt ? new Date(selectedFlag.resolvedAt).toLocaleString() : 'N/A'}
                </span>
              </div>
            ) : (
              <div>
                <Textarea
                  label="Officer Resolution Notes (FR14)"
                  placeholder="Record reconciliation details (e.g. farmer agreed to re-pricing tier; catalog updated; batch re-assorted)..."
                  rows={4}
                  value={resolutionNotes}
                  onChange={(e) => setResolutionNotes(e.target.value)}
                  required
                  helperText="These notes will be logged in the immutable audit trail and sent to the farmer."
                />
              </div>
            )}
          </div>
        </Modal>
      )}
    </div>
  );
};
