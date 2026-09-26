import React from 'react';
import { type Listing, resolveImageUrl } from '../utils/api';
import { useAuth } from '../context/AuthContext';
import { MapPin, Calendar, Layers, Eye, Sparkles, ImagePlus } from './Icons';

interface ProductCard3DProps {
  listing: Listing;
  onSelect: (listing: Listing) => void;
  onEdit?: (listing: Listing) => void;
  onWithdraw?: (listing: Listing) => void;
}

export const ProductCard3D: React.FC<ProductCard3DProps> = ({
  listing,
  onSelect,
  onEdit: _onEdit,
  onWithdraw: _onWithdraw
}) => {
  const { user } = useAuth();
  const isOwner = user?.id === listing.farmerId;
  const rawPhoto = listing.photos && listing.photos.length > 0 ? listing.photos[0].url : null;
  const primaryPhoto = resolveImageUrl(rawPhoto);
  const photoCount = listing.photos ? listing.photos.length : 0;

  const formatDate = (dateStr: string) => {
    try {
      const d = new Date(dateStr);
      return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    } catch {
      return dateStr;
    }
  };

  const getStatusBadge = () => {
    switch (listing.status) {
      case 'Published':
        return <span className="badge badge-published">Published</span>;
      case 'PendingApproval':
        return <span className="badge badge-pending">Pending Review</span>;
      case 'Draft':
        return <span className="badge badge-draft">Draft</span>;
      case 'Withdrawn':
        return <span className="badge badge-withdrawn">Withdrawn</span>;
      default:
        return <span className="badge badge-draft">{listing.status}</span>;
    }
  };

  const getGradeBadge = () => {
    switch (listing.claimedGrade) {
      case 'A':
        return <span className="badge badge-grade-a">Grade A</span>;
      case 'B':
        return <span className="badge badge-grade-b">Grade B</span>;
      default:
        return <span className="badge badge-grade-c">Grade {listing.claimedGrade || 'C'}</span>;
    }
  };

  return (
    <div
      className="glass-card"
      style={{
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
        cursor: 'pointer',
        borderColor: isOwner ? 'var(--accent-border)' : 'var(--border)',
      }}
      onClick={() => onSelect(listing)}
    >
      {/* Top Image */}
      <div style={{ position: 'relative', width: '100%', height: '180px', overflow: 'hidden', background: 'var(--bg-input)' }}>
        <img
          src={primaryPhoto}
          alt={listing.cropName}
          onError={(e) => {
            (e.currentTarget as HTMLImageElement).src = 'https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=800&auto=format&fit=crop';
          }}
          style={{ width: '100%', height: '100%', objectFit: 'cover' }}
        />

        {/* Badges Overlay */}
        <div style={{
          position: 'absolute',
          top: '10px',
          left: '10px',
          right: '10px',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          gap: '8px',
        }}>
          <div style={{ display: 'flex', gap: '6px', alignItems: 'center' }}>
            {getGradeBadge()}
            <span style={{
              fontSize: '0.7rem',
              fontWeight: 500,
              padding: '3px 8px',
              borderRadius: '6px',
              background: 'rgba(11, 15, 14, 0.75)',
              color: 'var(--text-muted)',
            }}>
              {listing.cropCategory || 'Produce'}
            </span>
          </div>
          {getStatusBadge()}
        </div>

        {/* Owner Tag or Multi-Photo Pill */}
        <div style={{
          position: 'absolute',
          bottom: '10px',
          left: '10px',
          right: '10px',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
        }}>
          {isOwner && (
            <span style={{
              fontSize: '0.7rem',
              fontWeight: 600,
              background: 'var(--accent)',
              color: '#06120c',
              padding: '3px 8px',
              borderRadius: '6px',
            }}>
              Your Listing
            </span>
          )}

          {photoCount > 1 && (
            <span style={{
              fontSize: '0.7rem',
              fontWeight: 500,
              background: 'rgba(11, 15, 14, 0.75)',
              color: 'var(--text-muted)',
              padding: '2px 8px',
              borderRadius: '9999px',
              marginLeft: 'auto',
              display: 'inline-flex',
              alignItems: 'center',
              gap: '4px',
            }}>
              <ImagePlus size={11} /> {photoCount}
            </span>
          )}
        </div>
      </div>

      {/* Card Content */}
      <div style={{ padding: '16px', display: 'flex', flexDirection: 'column', flex: 1, gap: '12px' }}>
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
            <h3 style={{ fontSize: '1.1rem', fontWeight: 600, color: 'var(--text)' }}>
              {listing.cropName}
            </h3>
            <div style={{ textAlign: 'right' }}>
              <span style={{ fontSize: '1.15rem', fontWeight: 700, color: 'var(--text)' }}>
                Rs. {listing.minPrice ? listing.minPrice.toFixed(0) : '—'}
              </span>
              <span style={{ fontSize: '0.75rem', color: 'var(--text-faint)' }}> /{listing.unit}</span>
            </div>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginTop: '4px', color: 'var(--text-muted)', fontSize: '0.85rem' }}>
            <MapPin size={14} />
            <span>{listing.regionName || 'Sri Lanka'}</span>
            <span style={{ color: 'var(--border-strong)' }}>•</span>
            <Layers size={14} />
            <span>{listing.quantity} {listing.unit}</span>
          </div>
        </div>

        {listing.description && (
          <p style={{
            fontSize: '0.82rem',
            color: 'var(--text-muted)',
            lineHeight: 1.45,
            display: '-webkit-box',
            WebkitLineClamp: 2,
            WebkitBoxOrient: 'vertical',
            overflow: 'hidden',
            textOverflow: 'ellipsis'
          }}>
            {listing.description}
          </p>
        )}

        {/* AI Suggested Price Indicator */}
        {listing.priceSuggestion && (
          <div style={{
            background: 'var(--bg-hover)',
            border: '1px solid var(--border)',
            borderRadius: 'var(--radius-sm)',
            padding: '8px 10px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            fontSize: '0.75rem'
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: 'var(--text-muted)' }}>
              <Sparkles size={13} />
              <span>Fair price</span>
            </div>
            <span style={{ color: 'var(--text)', fontWeight: 600 }}>
              Rs. {listing.priceSuggestion.suggestedPriceMin.toFixed(0)} – {listing.priceSuggestion.suggestedPriceMax.toFixed(0)}
            </span>
          </div>
        )}

        {/* Footer */}
        <div style={{
          marginTop: 'auto',
          paddingTop: '12px',
          borderTop: '1px solid var(--border)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between'
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: 'var(--text-faint)', fontSize: '0.75rem' }}>
            <Calendar size={13} />
            <span>{formatDate(listing.pickupWindowStart)} – {formatDate(listing.pickupWindowEnd)}</span>
          </div>

          <button
            className="btn btn-ghost"
            style={{ padding: '6px 10px', fontSize: '0.75rem' }}
            onClick={(e) => {
              e.stopPropagation();
              onSelect(listing);
            }}
          >
            <Eye size={13} /> Details
          </button>
        </div>
      </div>
    </div>
  );
};
