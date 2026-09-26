import React, { useState } from 'react';
import {
  User,
  Calendar,
  Eye,
  Edit3,
  ShieldCheck
} from 'lucide-react';
import { Card } from '../components/ui/Card';
import { Button } from '../components/ui/Button';
import { GradeBadge } from '../components/ui/Badge';
import { Modal } from '../components/ui/Modal';
import { Textarea, Select } from '../components/ui/FormControls';
import { Table } from '../components/ui/Table';
import type { Column } from '../components/ui/Table';
import { SearchFilterBar } from '../components/ui/SearchBar';
import { Toast } from '../components/ui/Toast';
import type { InspectionResponse, UpdateInspectionPayload } from '../types/inspection';

export interface InspectionHistoryPageProps {
  inspections: InspectionResponse[];
  onAmendInspection: (id: string, payload: UpdateInspectionPayload) => Promise<void>;
}

export const InspectionHistoryPage: React.FC<InspectionHistoryPageProps> = ({
  inspections,
  onAmendInspection
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedGrade, setSelectedGrade] = useState('');
  const [viewDetail, setViewDetail] = useState<InspectionResponse | null>(null);
  const [amendTarget, setAmendTarget] = useState<InspectionResponse | null>(null);

  // Amendment form state
  const [amendGrade, setAmendGrade] = useState('Grade A');
  const [amendNotes, setAmendNotes] = useState('');
  const [amendReason, setAmendReason] = useState('');
  const [isAmending, setIsAmending] = useState(false);
  const [toastMsg, setToastMsg] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const filtered = inspections.filter((i) => {
    const matchesSearch =
      i.cropName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      i.farmerName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      i.officerName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      (i.notes && i.notes.toLowerCase().includes(searchTerm.toLowerCase()));

    const matchesGrade = !selectedGrade || i.confirmedGrade.toLowerCase() === selectedGrade.toLowerCase();

    return matchesSearch && matchesGrade;
  });

  const handleOpenAmend = (inspection: InspectionResponse) => {
    setAmendTarget(inspection);
    setAmendGrade(inspection.confirmedGrade);
    setAmendNotes(inspection.notes || '');
    setAmendReason('');
  };

  const handleAmendSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!amendTarget) return;

    if (!amendReason || amendReason.trim().length < 5) {
      setToastMsg({ type: 'error', text: 'Please enter a justification reason for amending this inspection (min 5 chars).' });
      return;
    }

    try {
      setIsAmending(true);
      await onAmendInspection(amendTarget.id, {
        confirmedGrade: amendGrade,
        notes: amendNotes,
        reasonForAmendment: amendReason.trim(),
        photoUrls: amendTarget.photos.map((p) => p.url)
      });
      setToastMsg({
        type: 'success',
        text: `Inspection record for ${amendTarget.cropName} amended successfully with audit trail.`
      });
      setAmendTarget(null);
    } catch (err: any) {
      setToastMsg({ type: 'error', text: err.message || 'Failed to amend inspection.' });
    } finally {
      setIsAmending(false);
    }
  };

  const columns: Column<InspectionResponse>[] = [
    {
      header: 'Produce & Batch',
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
      header: 'Confirmed Grade',
      render: (row) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
          <GradeBadge grade={row.confirmedGrade} />
          {row.hasDiscrepancy && (
            <span style={{ fontSize: '11px', color: 'var(--status-warning-text)', fontWeight: 600 }}>
              (Mismatch)
            </span>
          )}
        </div>
      )
    },
    {
      header: 'Inspector Officer',
      render: (row) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '13px' }}>
          <User size={14} color="var(--text-secondary)" />
          <span style={{ fontWeight: 500 }}>{row.officerName}</span>
        </div>
      )
    },
    {
      header: 'Inspection Date',
      render: (row) => (
        <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            <Calendar size={12} />
            <span>{new Date(row.inspectedAt).toLocaleString()}</span>
          </div>
        </div>
      )
    },
    {
      header: 'Actions',
      align: 'right',
      render: (row) => (
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px' }}>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setViewDetail(row)}
            icon={<Eye size={14} />}
          >
            Details
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => handleOpenAmend(row)}
            icon={<Edit3 size={14} />}
            style={{ color: 'var(--text-secondary)' }}
          >
            Amend
          </Button>
        </div>
      )
    }
  ];

  return (
    <div>
      {/* Page Header */}
      <div style={{ marginBottom: '24px' }}>
        <h1 style={{ fontSize: '24px', fontWeight: 700, color: 'var(--text-primary)' }}>
          Inspection History & Digital Audit Trail (FR13)
        </h1>
        <p style={{ fontSize: '14px', color: 'var(--text-secondary)', marginTop: '4px' }}>
          Full auditable log of physical quality inspections, officer verifications, and amendment histories.
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

      {/* Search & Filter */}
      <SearchFilterBar
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        selectedGrade={selectedGrade}
        onGradeChange={setSelectedGrade}
        placeholder="Search inspection history by crop, farmer, officer, or notes..."
        onReset={() => {
          setSearchTerm('');
          setSelectedGrade('');
        }}
      />

      {/* History Table */}
      <Card
        title={`All Recorded Inspections (${filtered.length})`}
        subtitle="Chronological audit records maintained for compliance tracking"
      >
        <Table
          columns={columns}
          data={filtered}
          keyExtractor={(row) => row.id}
          emptyMessage="No inspection records found matching filter."
        />
      </Card>

      {/* Inspection Details Modal */}
      {viewDetail && (
        <Modal
          isOpen={!!viewDetail}
          onClose={() => setViewDetail(null)}
          title={`Inspection Detail: ${viewDetail.cropName}`}
          maxWidth="600px"
          footer={
            <Button variant="secondary" onClick={() => setViewDetail(null)}>
              Close
            </Button>
          }
        >
          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', fontSize: '14px' }}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', backgroundColor: '#F9FAFB', padding: '16px', borderRadius: 'var(--radius-sm)' }}>
              <div>
                <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>Crop & Quantity</span>
                <p style={{ fontWeight: 600, color: 'var(--text-primary)', margin: 0 }}>
                  {viewDetail.cropName} ({viewDetail.quantity} {viewDetail.unit})
                </p>
              </div>
              <div>
                <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>Farmer</span>
                <p style={{ fontWeight: 600, color: 'var(--text-primary)', margin: 0 }}>
                  {viewDetail.farmerName}
                </p>
              </div>
              <div>
                <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>Claimed vs Confirmed</span>
                <div style={{ display: 'flex', gap: '8px', marginTop: '4px' }}>
                  <GradeBadge grade={viewDetail.confirmedGrade} size="sm" />
                </div>
              </div>
              <div>
                <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>Inspector Officer</span>
                <p style={{ fontWeight: 600, color: 'var(--text-primary)', margin: 0 }}>
                  {viewDetail.officerName}
                </p>
              </div>
              <div>
                <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>Inspected At</span>
                <p style={{ color: 'var(--text-primary)', margin: 0 }}>
                  {new Date(viewDetail.inspectedAt).toLocaleString()}
                </p>
              </div>
              <div>
                <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>Collection Centre</span>
                <p style={{ color: 'var(--text-primary)', margin: 0 }}>
                  {viewDetail.regionName}
                </p>
              </div>
            </div>

            <div>
              <span style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-primary)', display: 'block', marginBottom: '6px' }}>
                Inspection Observations & Notes:
              </span>
              <p style={{ backgroundColor: '#FFFFFF', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-sm)', padding: '12px', color: 'var(--text-primary)', margin: 0, lineHeight: 1.5 }}>
                {viewDetail.notes || 'No specific defect notes recorded.'}
              </p>
            </div>

            {viewDetail.photos && viewDetail.photos.length > 0 && (
              <div>
                <span style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-primary)', display: 'block', marginBottom: '8px' }}>
                  Inspection Photographic Evidence:
                </span>
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: '10px' }}>
                  {viewDetail.photos.map((photo) => (
                    <img
                      key={photo.id}
                      src={photo.url}
                      alt="Inspection Evidence"
                      style={{
                        width: '120px',
                        height: '120px',
                        borderRadius: 'var(--radius-sm)',
                        objectFit: 'cover',
                        border: '1px solid var(--border-color)'
                      }}
                    />
                  ))}
                </div>
              </div>
            )}
          </div>
        </Modal>
      )}

      {/* Amend Inspection Modal */}
      {amendTarget && (
        <Modal
          isOpen={!!amendTarget}
          onClose={() => setAmendTarget(null)}
          title={`Amend Inspection: ${amendTarget.cropName}`}
          maxWidth="540px"
          footer={
            <>
              <Button variant="secondary" onClick={() => setAmendTarget(null)} disabled={isAmending}>
                Cancel
              </Button>
              <Button
                variant="primary"
                onClick={handleAmendSubmit}
                isLoading={isAmending}
                icon={<ShieldCheck size={16} />}
              >
                Save Amendment (FR13 Audit Logged)
              </Button>
            </>
          }
        >
          <form onSubmit={handleAmendSubmit}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
              <div style={{ backgroundColor: '#FEF3C7', border: '1px solid #FDE68A', padding: '12px', borderRadius: 'var(--radius-sm)', fontSize: '13px', color: '#B45309' }}>
                <strong>Audit Compliance Notice (FR13):</strong> Any change to this confirmed grade or notes will be permanently logged in the system audit trail along with your officer ID and reason.
              </div>

              <Select
                label="Updated Confirmed Grade"
                value={amendGrade}
                onChange={(e) => setAmendGrade(e.target.value)}
                required
                options={[
                  { value: 'Grade A', label: 'Grade A (Premium Export)' },
                  { value: 'Grade B', label: 'Grade B (Standard Retail)' },
                  { value: 'Grade C', label: 'Grade C (Processing Only)' },
                  { value: 'Rejected', label: 'Rejected (Unfit / Damaged)' }
                ]}
              />

              <Textarea
                label="Updated Observations & Defect Notes"
                rows={3}
                value={amendNotes}
                onChange={(e) => setAmendNotes(e.target.value)}
              />

              <Textarea
                label="Mandatory Reason for Amendment"
                placeholder="Explain why this inspection is being modified (e.g. secondary re-test conducted, sample moisture re-checked, farmer appeal reviewed)..."
                rows={3}
                value={amendReason}
                onChange={(e) => setAmendReason(e.target.value)}
                required
                helperText="Required for audit trail compliance."
              />
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
};
