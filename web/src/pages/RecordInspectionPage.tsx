import React, { useState } from 'react';
import {
  ShieldCheck,
  AlertTriangle,
  Camera,
  ArrowLeft,
  Check
} from 'lucide-react';
import { Card } from '../components/ui/Card';
import { Button } from '../components/ui/Button';
import { GradeBadge } from '../components/ui/Badge';
import { Textarea } from '../components/ui/FormControls';
import { Toast } from '../components/ui/Toast';
import type { ListingSummary, CreateInspectionPayload } from '../types/inspection';

export interface RecordInspectionPageProps {
  listing: ListingSummary;
  onBack: () => void;
  onSubmit: (payload: CreateInspectionPayload) => Promise<void>;
}

export const RecordInspectionPage: React.FC<RecordInspectionPageProps> = ({
  listing,
  onBack,
  onSubmit
}) => {
  const [confirmedGrade, setConfirmedGrade] = useState<string>(listing.claimedGrade || 'Grade A');
  const [notes, setNotes] = useState<string>('');
  const [photoUrl, setPhotoUrl] = useState<string>('');
  const [photoList, setPhotoList] = useState<string[]>(() =>
    listing.listingPhotos && listing.listingPhotos.length > 0 ? [...listing.listingPhotos] : []
  );
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  const hasDiscrepancy =
    confirmedGrade.trim().toLowerCase() !== (listing.claimedGrade || '').trim().toLowerCase();

  const handleAddPhoto = () => {
    if (photoUrl.trim() && !photoList.includes(photoUrl.trim())) {
      setPhotoList([...photoList, photoUrl.trim()]);
      setPhotoUrl('');
    }
  };

  const handleRemovePhoto = (index: number) => {
    setPhotoList(photoList.filter((_, i) => i !== index));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);
    setSuccessMsg(null);

    if (!confirmedGrade) {
      setErrorMsg('Please select a confirmed quality grade.');
      return;
    }

    if (hasDiscrepancy && (!notes || notes.trim().length < 10)) {
      setErrorMsg('A discrepancy was detected. Please provide a detailed inspection note explaining the grade deviation (at least 10 characters).');
      return;
    }

    try {
      setIsSubmitting(true);
      await onSubmit({
        listingId: listing.id,
        confirmedGrade,
        notes,
        photoUrls: photoList
      });
      setSuccessMsg(`Quality inspection recorded successfully as ${confirmedGrade}!`);
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to record quality inspection.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div>
      {/* Back Button & Title */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '20px' }}>
        <Button variant="outline" size="sm" onClick={onBack} icon={<ArrowLeft size={16} />}>
          Back to Queue
        </Button>
        <div>
          <h1 style={{ fontSize: '22px', fontWeight: 700, color: 'var(--text-primary)', margin: 0 }}>
            Quality Inspection & Grading Form (FR12)
          </h1>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', margin: 0 }}>
            Inspect physical produce, verify quality attributes against standards, and confirm grade.
          </p>
        </div>
      </div>

      {successMsg && (
        <div style={{ marginBottom: '20px' }}>
          <Toast
            type="success"
            title="Inspection Verified"
            message={successMsg}
            onClose={() => setSuccessMsg(null)}
          />
        </div>
      )}

      {errorMsg && (
        <div style={{ marginBottom: '20px' }}>
          <Toast
            type="error"
            title="Inspection Error"
            message={errorMsg}
            onClose={() => setErrorMsg(null)}
          />
        </div>
      )}

      <div style={{ display: 'grid', gridTemplateColumns: 'minmax(320px, 380px) 1fr', gap: '24px' }}>
        {/* Left Column: Produce Batch Profile */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <Card title="Produce Batch Details">
            {listing.listingPhotos && listing.listingPhotos.length > 0 && (
              <div style={{ marginBottom: '16px' }}>
                <img
                  src={listing.listingPhotos[0]}
                  alt={listing.cropName}
                  style={{
                    width: '100%',
                    height: '180px',
                    borderRadius: 'var(--radius-sm)',
                    objectFit: 'cover',
                    border: '1px solid var(--border-color)'
                  }}
                />
              </div>
            )}

            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', fontSize: '14px' }}>
              <div>
                <span style={{ fontSize: '12px', color: 'var(--text-secondary)', display: 'block' }}>Crop Name</span>
                <span style={{ fontWeight: 600, color: 'var(--text-primary)', fontSize: '16px' }}>{listing.cropName}</span>
              </div>

              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <div>
                  <span style={{ fontSize: '12px', color: 'var(--text-secondary)', display: 'block' }}>Quantity</span>
                  <span style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{listing.quantity} {listing.unit}</span>
                </div>
                <div>
                  <span style={{ fontSize: '12px', color: 'var(--text-secondary)', display: 'block' }}>Floor Price</span>
                  <span style={{ fontWeight: 600, color: 'var(--text-primary)' }}>LKR {listing.minPrice?.toFixed(2) || 'N/A'}/kg</span>
                </div>
              </div>

              <div style={{ borderTop: '1px solid var(--border-color-subtle)', paddingTop: '10px' }}>
                <span style={{ fontSize: '12px', color: 'var(--text-secondary)', display: 'block' }}>Farmer Self-Claimed Grade</span>
                <div style={{ marginTop: '4px' }}>
                  <GradeBadge grade={listing.claimedGrade} />
                </div>
              </div>

              <div style={{ borderTop: '1px solid var(--border-color-subtle)', paddingTop: '10px' }}>
                <span style={{ fontSize: '12px', color: 'var(--text-secondary)', display: 'block' }}>Farmer Information</span>
                <span style={{ fontWeight: 500, color: 'var(--text-primary)', display: 'block' }}>{listing.farmerName}</span>
                <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>{listing.farmerPhone}</span>
              </div>

              <div style={{ borderTop: '1px solid var(--border-color-subtle)', paddingTop: '10px' }}>
                <span style={{ fontSize: '12px', color: 'var(--text-secondary)', display: 'block' }}>Collection Centre & Region</span>
                <span style={{ color: 'var(--text-primary)' }}>{listing.regionName}</span>
              </div>
            </div>
          </Card>
        </div>

        {/* Right Column: Physical Quality Grading & Evaluation */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <form onSubmit={handleSubmit}>
            <Card
              title="Physical Inspection & Quality Evaluation"
              subtitle="Select the confirmed grade after inspecting size, skin condition, color, and uniformity"
            >
              {/* Grade Selector Cards */}
              <div style={{ marginBottom: '20px' }}>
                <label style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-primary)', marginBottom: '8px', display: 'block' }}>
                  Confirmed Produce Grade <span style={{ color: '#DC2626' }}>*</span>
                </label>

                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '12px' }}>
                  {[
                    {
                      id: 'Grade A',
                      title: 'Grade A (Premium Export)',
                      desc: 'Uniform shape & size, vibrant color, zero blemishes or defects (<2%).',
                      color: 'var(--status-success-bg)',
                      border: 'var(--status-success-border)'
                    },
                    {
                      id: 'Grade B',
                      title: 'Grade B (Standard Retail)',
                      desc: 'Good market quality, minor cosmetic blemishes (<15%), sound produce.',
                      color: 'var(--status-pending-bg)',
                      border: 'var(--status-pending-border)'
                    },
                    {
                      id: 'Grade C',
                      title: 'Grade C (Processing)',
                      desc: 'Irregular shape, non-uniform sizing, suitable for pulping/drying.',
                      color: 'var(--status-warning-bg)',
                      border: 'var(--status-warning-border)'
                    },
                    {
                      id: 'Rejected',
                      title: 'Rejected (Unfit)',
                      desc: 'Rot, disease, pest infestation, or excessive mechanical damage.',
                      color: 'var(--status-error-bg)',
                      border: 'var(--status-error-border)'
                    }
                  ].map((gradeOption) => {
                    const isSelected = confirmedGrade === gradeOption.id;
                    return (
                      <div
                        key={gradeOption.id}
                        onClick={() => setConfirmedGrade(gradeOption.id)}
                        style={{
                          padding: '14px',
                          borderRadius: 'var(--radius-sm)',
                          border: `2px solid ${isSelected ? 'var(--primary-green)' : 'var(--border-color)'}`,
                          backgroundColor: isSelected ? 'var(--primary-green-light)' : '#FFFFFF',
                          cursor: 'pointer',
                          transition: 'all 0.15s ease'
                        }}
                      >
                        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '4px' }}>
                          <span style={{ fontWeight: 600, fontSize: '14px', color: 'var(--text-primary)' }}>
                            {gradeOption.title}
                          </span>
                          {isSelected && <Check size={16} color="var(--primary-green)" />}
                        </div>
                        <p style={{ fontSize: '12px', color: 'var(--text-secondary)', margin: 0, lineHeight: 1.4 }}>
                          {gradeOption.desc}
                        </p>
                      </div>
                    );
                  })}
                </div>
              </div>

              {/* Real-Time Discrepancy Detection Alert (FR14) */}
              {hasDiscrepancy && (
                <div
                  style={{
                    backgroundColor: 'var(--status-warning-bg)',
                    border: '1px solid var(--status-warning-border)',
                    borderRadius: 'var(--radius-sm)',
                    padding: '14px 16px',
                    marginBottom: '20px',
                    display: 'flex',
                    alignItems: 'flex-start',
                    gap: '12px'
                  }}
                >
                  <AlertTriangle size={20} color="var(--status-warning-text)" style={{ flexShrink: 0, marginTop: '2px' }} />
                  <div>
                    <span style={{ fontWeight: 600, color: 'var(--status-warning-text)', fontSize: '14px', display: 'block' }}>
                      Grade Discrepancy Detected (FR14 Compliance Trigger)
                    </span>
                    <p style={{ fontSize: '13px', color: 'var(--text-primary)', margin: '4px 0 0 0', lineHeight: 1.4 }}>
                      The farmer claimed <strong>{listing.claimedGrade}</strong>, but you are confirming <strong>{confirmedGrade}</strong>.
                      Upon saving, this listing will be flagged in the <strong>Discrepancy Review Queue</strong> and the farmer will receive an automated notification.
                    </p>
                  </div>
                </div>
              )}

              {/* Evaluation Notes */}
              <Textarea
                label="Inspection Observations & Defect Notes"
                placeholder="Detail physical inspection observations (e.g. skin quality, average diameter, firmness, moisture content, pest checks)..."
                rows={4}
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                required={hasDiscrepancy}
                helperText={
                  hasDiscrepancy
                    ? 'Required: Explain the rationale for the grade deviation to support audit compliance (FR13/FR14).'
                    : 'Optional: Record any quality observations or batch notes.'
                }
              />

              {/* Photographic Evidence Attachment */}
              <div style={{ marginBottom: '20px' }}>
                <label style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-primary)', marginBottom: '6px', display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <Camera size={16} />
                  Inspection Photo Evidence (Optional)
                </label>
                <div style={{ display: 'flex', gap: '8px', marginBottom: '10px' }}>
                  <input
                    type="url"
                    placeholder="Enter inspection photo URL (e.g. https://...)"
                    value={photoUrl}
                    onChange={(e) => setPhotoUrl(e.target.value)}
                    style={{
                      flex: 1,
                      padding: '8px 12px',
                      fontSize: '14px',
                      borderRadius: 'var(--radius-sm)',
                      border: '1px solid var(--border-color)',
                      outline: 'none'
                    }}
                  />
                  <Button type="button" variant="secondary" size="sm" onClick={handleAddPhoto}>
                    Add Photo
                  </Button>
                </div>

                {photoList.length > 0 && (
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: '10px', marginTop: '10px' }}>
                    {photoList.map((url, idx) => (
                      <div
                        key={idx}
                        style={{
                          position: 'relative',
                          width: '80px',
                          height: '80px',
                          borderRadius: 'var(--radius-sm)',
                          overflow: 'hidden',
                          border: '1px solid var(--border-color)'
                        }}
                      >
                        <img src={url} alt="Evidence" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                        <button
                          type="button"
                          onClick={() => handleRemovePhoto(idx)}
                          style={{
                            position: 'absolute',
                            top: '2px',
                            right: '2px',
                            backgroundColor: 'rgba(0, 0, 0, 0.6)',
                            color: '#FFFFFF',
                            border: 'none',
                            borderRadius: '50%',
                            width: '20px',
                            height: '20px',
                            cursor: 'pointer',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            fontSize: '11px'
                          }}
                        >
                          ✕
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Action Buttons */}
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  borderTop: '1px solid var(--border-color)',
                  paddingTop: '16px'
                }}
              >
                <Button type="button" variant="secondary" onClick={onBack}>
                  Cancel
                </Button>

                <div style={{ display: 'flex', gap: '12px' }}>
                  <Button
                    type="submit"
                    variant="primary"
                    isLoading={isSubmitting}
                    icon={<ShieldCheck size={16} />}
                  >
                    Save Inspection Record
                  </Button>
                </div>
              </div>
            </Card>
          </form>
        </div>
      </div>
    </div>
  );
};
