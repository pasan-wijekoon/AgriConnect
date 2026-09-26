import React, { useState } from 'react';
import { type Listing, resolveImageUrl } from '../utils/api';
import { useAuth } from '../context/AuthContext';
import { X, MapPin, Calendar, Sparkles, CheckCircle2, AlertCircle, ShoppingBag, Edit, Trash2 } from './Icons';

interface ProductDetailModalProps {
  listing: Listing | null;
  onClose: () => void;
  onEdit?: (listing: Listing) => void;
  onWithdraw?: (listing: Listing) => void;
  onApprove?: (listing: Listing) => void;
  onReject?: (listing: Listing) => void;
}

export const ProductDetailModal: React.FC<ProductDetailModalProps> = ({
  listing,
  onClose,
  onEdit,
  onWithdraw,
  onApprove,
  onReject
}) => {
  const { user } = useAuth();
  const [selectedPhotoIndex, setSelectedPhotoIndex] = useState(0);
  const [orderQuantity, setOrderQuantity] = useState<number>(100);
  const [showOrderSuccess, setShowOrderSuccess] = useState(false);
  const [orderMode, setOrderMode] = useState(false);

  if (!listing) return null;

  const rawPhotos = listing.photos && listing.photos.length > 0
    ? listing.photos
    : [{ id: '1', url: 'https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=800&auto=format&fit=crop', uploadedAt: '' }];

  const photos = rawPhotos.map(p => ({ ...p, url: resolveImageUrl(p.url) }));
  const currentPhoto = photos[selectedPhotoIndex] || photos[0];
  const isOwner = user?.id === listing.farmerId;
  const isFarmer = user?.role === 'Farmer';
  const isBuyer = user?.role === 'Buyer';
  const isAdmin = user?.role === 'Admin';

  const formatDate = (dateStr: string) => {
    try {
      const d = new Date(dateStr);
      return d.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' });
    } catch {
      return dateStr;
    }
  };

  const handlePlaceOrder = (e: React.FormEvent) => {
    e.preventDefault();
    setShowOrderSuccess(true);
    setTimeout(() => {
      setShowOrderSuccess(false);
      setOrderMode(false);
      onClose();
    }, 2200);
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div
        className="modal-content"
        style={{ maxWidth: '820px' }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Modal Header */}
        <div style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          padding: '1.25rem 1.75rem',
          borderBottom: '1px solid var(--border)',
          background: 'var(--bg-raised)'
        }}>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <span className={`badge badge-grade-${listing.claimedGrade.toLowerCase()}`}>
                Grade {listing.claimedGrade}
              </span>
              <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                {listing.cropCategory}
              </span>
            </div>
            <h2 style={{ fontSize: '1.6rem', fontWeight: 800, color: 'var(--text)', marginTop: '2px' }}>
              {listing.cropName}
            </h2>
          </div>

          <button
            onClick={onClose}
            style={{
              background: 'var(--border)',
              border: 'none',
              borderRadius: '50%',
              width: '36px',
              height: '36px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: 'var(--text-muted)',
              cursor: 'pointer',
              transition: 'all 0.2s'
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.color = 'var(--text)';
              e.currentTarget.style.background = 'var(--border-strong)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.color = 'var(--text-muted)';
              e.currentTarget.style.background = 'var(--border)';
            }}
          >
            <X size={20} />
          </button>
        </div>

        {/* Modal Body */}
        <div style={{ padding: '1.75rem', display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
          
          {/* Photos Showcase */}
          <div>
            {/* Main Stage Image */}
            <div style={{
              width: '100%',
              height: '340px',
              borderRadius: '14px',
              overflow: 'hidden',
              background: 'var(--bg-input)',
              position: 'relative',
              boxShadow: '0 10px 25px var(--shadow-md)',
              border: '1px solid var(--border)'
            }}>
              <img
                src={currentPhoto.url}
                alt={listing.cropName}
                onError={(e) => {
                  (e.currentTarget as HTMLImageElement).src = 'https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=800&auto=format&fit=crop';
                }}
                style={{
                  width: '100%',
                  height: '100%',
                  objectFit: 'cover',
                  transition: 'opacity 0.3s ease'
                }}
              />
              <div style={{
                position: 'absolute',
                bottom: '12px',
                right: '12px',
                background: 'rgba(0, 0, 0, 0.75)',
                backdropFilter: 'blur(6px)',
                padding: '4px 10px',
                borderRadius: '8px',
                fontSize: '0.75rem',
                color: 'var(--text-muted)',
                fontWeight: 600
              }}>
                Photo {selectedPhotoIndex + 1} of {photos.length}
              </div>
            </div>

            {/* Thumbnail Row */}
            {photos.length > 1 && (
              <div style={{
                display: 'flex',
                gap: '10px',
                marginTop: '12px',
                overflowX: 'auto',
                paddingBottom: '4px'
              }}>
                {photos.map((photo, index) => (
                  <button
                    key={photo.id || index}
                    onClick={() => setSelectedPhotoIndex(index)}
                    style={{
                      width: '76px',
                      height: '56px',
                      borderRadius: '8px',
                      overflow: 'hidden',
                      border: selectedPhotoIndex === index ? '2px solid var(--accent)' : '1px solid var(--border)',
                      background: 'none',
                      padding: 0,
                      cursor: 'pointer',
                      flexShrink: 0,
                      opacity: selectedPhotoIndex === index ? 1 : 0.6,
                      transition: 'all 0.2s ease',
                      boxShadow: selectedPhotoIndex === index ? '0 0 10px var(--accent-border)' : 'none'
                    }}
                  >
                    <img src={photo.url} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Key Metrics Grid */}
          <div style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))',
            gap: '12px'
          }}>
            <div className="glass-card" style={{ padding: '14px' }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-faint)', fontWeight: 600 }}>PRICE / FLOOR</div>
              <div style={{ fontSize: '1.4rem', fontWeight: 800, color: 'var(--accent-text)', marginTop: '4px' }}>
                Rs. {listing.minPrice ? listing.minPrice.toFixed(0) : '—'}
                <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}> /{listing.unit}</span>
              </div>
            </div>

            <div className="glass-card" style={{ padding: '14px' }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-faint)', fontWeight: 600 }}>AVAILABLE BATCH</div>
              <div style={{ fontSize: '1.4rem', fontWeight: 800, color: 'var(--text)', marginTop: '4px' }}>
                {listing.quantity} <span style={{ fontSize: '0.9rem', color: 'var(--text-muted)' }}>{listing.unit}</span>
              </div>
            </div>

            <div className="glass-card" style={{ padding: '14px' }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-faint)', fontWeight: 600 }}>ORIGIN REGION</div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginTop: '6px' }}>
                <MapPin size={16} style={{ color: 'var(--accent)' }} />
                <span style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--text)' }}>{listing.regionName}</span>
              </div>
            </div>

            <div className="glass-card" style={{ padding: '14px' }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-faint)', fontWeight: 600 }}>STATUS</div>
              <div style={{ marginTop: '8px' }}>
                <span className={`badge badge-${listing.status.toLowerCase()}`}>
                  {listing.status}
                </span>
              </div>
            </div>
          </div>

          {/* Description Section */}
          <div className="glass-card" style={{ padding: '18px' }}>
            <h4 style={{ fontSize: '0.95rem', fontWeight: 700, color: 'var(--text)', marginBottom: '8px' }}>
              Harvest & Quality Description
            </h4>
            <p style={{ fontSize: '0.9rem', color: 'var(--text-muted)', lineHeight: 1.6, whiteSpace: 'pre-line' }}>
              {listing.description || 'No detailed agricultural description has been provided for this listing.'}
            </p>
          </div>

          {/* Fair-Price Suggestion */}
          {listing.priceSuggestion && (
            <div style={{
              background: 'var(--bg-card)',
              border: '1px solid var(--border)',
              borderRadius: 'var(--radius-lg)',
              padding: '18px',
              display: 'flex',
              flexDirection: 'column',
              gap: '10px'
            }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--text-muted)', fontWeight: 600, fontSize: '0.9rem' }}>
                  <Sparkles size={16} />
                  <span>Fair-price suggestion</span>
                </div>
                <span className="badge badge-published">
                  {Math.round(listing.priceSuggestion.confidence * 100)}% confidence
                </span>
              </div>

              <div style={{ margin: '4px 0' }}>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-faint)' }}>Suggested corridor</div>
                <div style={{ fontSize: '1.3rem', fontWeight: 700, color: 'var(--text)' }}>
                  Rs. {listing.priceSuggestion.suggestedPriceMin.toFixed(0)} – Rs. {listing.priceSuggestion.suggestedPriceMax.toFixed(0)}
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}> /{listing.unit}</span>
                </div>
              </div>

              <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', lineHeight: 1.5 }}>
                {listing.priceSuggestion.reasoningSummary}
              </p>
            </div>
          )}

          {/* Pickup Window Info */}
          <div style={{
            display: 'flex',
            alignItems: 'center',
            gap: '10px',
            background: 'var(--bg-input)',
            padding: '12px 16px',
            borderRadius: '12px',
            fontSize: '0.85rem',
            color: 'var(--text-muted)'
          }}>
            <Calendar size={18} style={{ color: 'var(--text-muted)' }} />
            <div>
              <strong style={{ color: 'var(--text)' }}>Wholesale Collection Window:</strong>{' '}
              {formatDate(listing.pickupWindowStart)} to {formatDate(listing.pickupWindowEnd)} at designated regional collection centre.
            </div>
          </div>

          {/* Order Placement Form for Buyers */}
          {orderMode && isBuyer && (
            <form onSubmit={handlePlaceOrder} style={{
              background: 'var(--accent-soft)',
              border: '1px solid var(--accent-border)',
              borderRadius: '14px',
              padding: '18px',
              display: 'flex',
              flexDirection: 'column',
              gap: '12px',
              animation: 'fadeIn 0.2s ease-out'
            }}>
              <h4 style={{ color: 'var(--accent-text)', fontSize: '1rem', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '6px' }}>
                <ShoppingBag size={18} /> Place Wholesale Purchase Order
              </h4>
              
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">Order Quantity ({listing.unit})</label>
                  <input
                    type="number"
                    min="1"
                    max={listing.quantity}
                    value={orderQuantity}
                    onChange={(e) => setOrderQuantity(Number(e.target.value))}
                    className="form-input"
                    required
                  />
                </div>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">Estimated Wholesale Total</label>
                  <div style={{
                    padding: '10px 14px',
                    background: 'rgba(0, 0, 0, 0.3)',
                    borderRadius: '10px',
                    color: 'var(--accent-text)',
                    fontWeight: 800,
                    fontSize: '1.1rem'
                  }}>
                    Rs. {((listing.minPrice || 0) * orderQuantity).toLocaleString()}
                  </div>
                </div>
              </div>

              <div style={{ display: 'flex', gap: '10px', marginTop: '6px' }}>
                <button type="submit" className="btn btn-primary" style={{ flex: 1 }}>
                  Confirm & Submit Order
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => setOrderMode(false)}>
                  Cancel
                </button>
              </div>
            </form>
          )}

          {showOrderSuccess && (
            <div style={{
              background: 'var(--accent-soft)',
              border: '1px solid var(--accent)',
              color: 'var(--accent-text)',
              padding: '14px',
              borderRadius: '12px',
              display: 'flex',
              alignItems: 'center',
              gap: '10px',
              fontWeight: 600
            }}>
              <CheckCircle2 size={20} />
              <span>Wholesale purchase order successfully dispatched to farmer & regional collection centre!</span>
            </div>
          )}

          {/* Role-Specific Actions Bar */}
          <div style={{
            display: 'flex',
            flexWrap: 'wrap',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: '12px',
            paddingTop: '1rem',
            borderTop: '1px solid var(--border)'
          }}>
            {/* Farmer Logic */}
            {isFarmer && (
              <>
                {isOwner ? (
                  <div style={{ display: 'flex', gap: '10px', width: '100%' }}>
                    {onEdit && (
                      <button
                        className="btn btn-primary"
                        onClick={() => {
                          onClose();
                          onEdit(listing);
                        }}
                      >
                        <Edit size={16} /> Edit My Produce
                      </button>
                    )}
                    {onWithdraw && listing.status !== 'Withdrawn' && (
                      <button
                        className="btn btn-danger"
                        onClick={() => {
                          onWithdraw(listing);
                          onClose();
                        }}
                      >
                        <Trash2 size={16} /> Withdraw Listing
                      </button>
                    )}
                  </div>
                ) : (
                  <div style={{
                    width: '100%',
                    background: 'var(--warn-soft)',
                    border: '1px solid var(--warn-border)',
                    padding: '10px 14px',
                    borderRadius: '10px',
                    color: '#e0ac57',
                    fontSize: '0.85rem',
                    display: 'flex',
                    alignItems: 'center',
                    gap: '8px'
                  }}>
                    <AlertCircle size={16} />
                    <span>
                      <strong>Farmer Policy:</strong> You are viewing a listing from another farmer. Farmers can discover regional pricing and peer produce, but are restricted from buying listings.
                    </span>
                  </div>
                )}
              </>
            )}

            {/* Buyer Logic */}
            {isBuyer && !orderMode && (
              <div style={{ display: 'flex', justifyContent: 'flex-end', width: '100%', gap: '10px' }}>
                <button
                  className="btn btn-primary"
                  style={{ padding: '12px 24px', fontSize: '1rem' }}
                  onClick={() => setOrderMode(true)}
                >
                  <ShoppingBag size={18} /> Request Wholesale Order
                </button>
              </div>
            )}

            {/* Admin / Officer Logic */}
            {isAdmin && (
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
                <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                  Officer Review Mode (Listing ID: {listing.id.substring(0, 8)}...)
                </span>
                <div style={{ display: 'flex', gap: '10px' }}>
                  {onReject && listing.status === 'PendingApproval' && (
                    <button
                      className="btn btn-danger"
                      onClick={() => {
                        onReject(listing);
                        onClose();
                      }}
                    >
                      <Trash2 size={16} /> Reject Listing
                    </button>
                  )}
                  {onApprove && listing.status === 'PendingApproval' && (
                    <button
                      className="btn btn-primary"
                      onClick={() => {
                        onApprove(listing);
                        onClose();
                      }}
                    >
                      <CheckCircle2 size={16} /> Approve & Publish Listing
                    </button>
                  )}
                </div>
              </div>
            )}
          </div>

        </div>
      </div>
    </div>
  );
};
