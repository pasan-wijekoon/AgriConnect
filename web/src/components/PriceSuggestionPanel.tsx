import React, { useState } from 'react';
import { type Listing, type PriceSuggestion, api } from '../utils/api';
import { Sparkles, RefreshCw, ShieldCheck } from './Icons';

interface Props {
  listing: Listing;
  onClose: () => void;
  onUpdate: (updated: Listing) => void;
}

const statusBadgeClass: Record<string, string> = {
  Proposed: 'badge-pending',
  Approved: 'badge-published',
  Rejected: 'badge-withdrawn',
  Revised: 'badge-grade-b',
};

export const PriceSuggestionPanel: React.FC<Props> = ({ listing, onClose, onUpdate }) => {
  const [suggestion, setSuggestion] = useState<PriceSuggestion | undefined>(listing.priceSuggestion);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleRefreshSuggestion = async () => {
    setLoading(true);
    setError(null);
    try {
      const refreshed = await api.getPriceSuggestion(listing.id);
      setSuggestion(refreshed);
      onUpdate({ ...listing, priceSuggestion: refreshed });
    } catch (err: any) {
      setError(err.message || 'Failed to refresh the price suggestion.');
    } finally {
      setLoading(false);
    }
  };

  const confidencePct = suggestion ? Math.round(suggestion.confidence * 100) : 0;

  const row = (label: string, value: React.ReactNode) => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '2px' }}>
      <span style={{ fontSize: '0.72rem', color: 'var(--text-faint)' }}>{label}</span>
      <span style={{ fontSize: '0.88rem', color: 'var(--text)' }}>{value}</span>
    </div>
  );

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: '560px' }}>
        {/* Header */}
        <div style={{
          padding: '16px 20px',
          borderBottom: '1px solid var(--border)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Sparkles size={16} />
            <h3 style={{ fontSize: '0.98rem', fontWeight: 600, color: 'var(--text)' }}>
              Fair-Price Review
            </h3>
          </div>
          <button className="btn btn-ghost" style={{ padding: '4px 8px' }} onClick={onClose}>&times;</button>
        </div>

        <div style={{ padding: '20px', display: 'flex', flexDirection: 'column', gap: '18px' }}>
          {/* Listing Summary */}
          <div style={{
            background: 'var(--bg-input)',
            border: '1px solid var(--border)',
            borderRadius: 'var(--radius-md)',
            padding: '14px 16px',
            display: 'grid',
            gridTemplateColumns: '1fr 1fr',
            gap: '12px 20px',
          }}>
            {row('Crop', `${listing.cropName} (${listing.cropCategory})`)}
            {row('Region', listing.regionName)}
            {row('Quantity', `${listing.quantity} ${listing.unit}`)}
            {row('Claimed grade', `Grade ${listing.claimedGrade}`)}
            {row('Farmer floor price', listing.minPrice ? `LKR ${listing.minPrice} / ${listing.unit}` : 'Not specified')}
          </div>

          {error && (
            <div style={{
              background: 'var(--danger-soft)',
              border: '1px solid var(--danger-border)',
              color: '#e08776',
              borderRadius: 'var(--radius-sm)',
              padding: '10px 12px',
              fontSize: '0.85rem',
            }}>
              {error}
            </div>
          )}

          {suggestion ? (
            <div style={{
              background: 'var(--bg-card)',
              border: '1px solid var(--border)',
              borderRadius: 'var(--radius-md)',
              padding: '18px',
              display: 'flex',
              flexDirection: 'column',
              gap: '14px',
            }}>
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', fontWeight: 500 }}>
                  Agentic AI proposal
                </span>
                <span className={`badge ${statusBadgeClass[suggestion.status] || 'badge-draft'}`}>
                  {suggestion.status}
                </span>
              </div>

              <div>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block', marginBottom: '4px' }}>
                  Suggested fair-price corridor
                </span>
                <div style={{ fontSize: '1.6rem', fontWeight: 700, color: 'var(--text)' }}>
                  LKR {suggestion.suggestedPriceMin.toLocaleString()} – {suggestion.suggestedPriceMax.toLocaleString()}
                  <span style={{ fontSize: '0.85rem', fontWeight: 500, color: 'var(--text-faint)' }}> / {listing.unit}</span>
                </div>
              </div>

              <div>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.78rem', color: 'var(--text-muted)', marginBottom: '4px' }}>
                  <span>Confidence</span>
                  <span style={{ fontWeight: 600, color: 'var(--text)' }}>{confidencePct}%</span>
                </div>
                <div style={{ width: '100%', height: '6px', background: 'var(--bg-input)', borderRadius: '3px', overflow: 'hidden' }}>
                  <div style={{
                    width: `${confidencePct}%`,
                    height: '100%',
                    borderRadius: '3px',
                    background: confidencePct >= 70 ? 'var(--accent)' : 'var(--warn)',
                  }} />
                </div>
              </div>

              <div style={{
                background: 'var(--bg-input)',
                border: '1px solid var(--border)',
                borderRadius: 'var(--radius-sm)',
                padding: '12px 14px',
              }}>
                <div style={{ fontSize: '0.72rem', fontWeight: 600, color: 'var(--text-muted)', marginBottom: '4px' }}>
                  Market reasoning
                </div>
                <p style={{ fontSize: '0.82rem', color: 'var(--text-muted)', lineHeight: 1.55 }}>
                  {suggestion.reasoningSummary}
                </p>
              </div>

              {suggestion.officerNote && (
                <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                  <strong style={{ color: 'var(--text)' }}>Officer note:</strong> {suggestion.officerNote}
                </div>
              )}

              <div style={{
                display: 'flex',
                alignItems: 'flex-start',
                gap: '8px',
                fontSize: '0.78rem',
                color: 'var(--text-faint)',
              }}>
                <ShieldCheck size={14} style={{ flexShrink: 0, marginTop: '2px' }} />
                <span>
                  Generated by the Fair-Price Estimation Agent from recent market data for {listing.regionName}.
                  It becomes final only after a collection-centre officer approves, rejects, or revises it.
                </span>
              </div>
            </div>
          ) : (
            <div style={{
              textAlign: 'center',
              padding: '24px',
              border: '1px dashed var(--border-strong)',
              borderRadius: 'var(--radius-md)',
              color: 'var(--text-muted)',
            }}>
              <p style={{ marginBottom: '12px', fontSize: '0.88rem' }}>No price suggestion generated yet.</p>
              <button className="btn btn-primary" onClick={handleRefreshSuggestion} disabled={loading}>
                {loading ? 'Generating…' : 'Generate fair-price estimate'}
              </button>
            </div>
          )}
        </div>

        <div style={{
          padding: '14px 20px',
          borderTop: '1px solid var(--border)',
          display: 'flex',
          justifyContent: 'flex-end',
          gap: '10px',
        }}>
          <button className="btn btn-secondary" onClick={handleRefreshSuggestion} disabled={loading}>
            <RefreshCw size={14} />
            {loading ? 'Refreshing…' : 'Re-evaluate'}
          </button>
          <button className="btn btn-primary" onClick={onClose}>
            Done
          </button>
        </div>
      </div>
    </div>
  );
};
