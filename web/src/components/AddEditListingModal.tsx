import React, { useState, useEffect, useRef } from 'react';
import { api, type Listing, type Crop, type Region, type PriceEstimateResult } from '../utils/api';
import { X, Plus, Trash2, ImagePlus, Sparkles, TrendingUp } from './Icons';

interface AddEditListingModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: {
    id?: string;
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
  }) => Promise<void>;
  crops: Crop[];
  regions: Region[];
  editingListing?: Listing | null;
}

const PRESET_PHOTOS: Record<string, string[]> = {
  Tomatoes: [
    'https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=800&auto=format&fit=crop',
    'https://images.unsplash.com/photo-1546470427-0d4db154ceb7?w=800&auto=format&fit=crop',
    'https://images.unsplash.com/photo-1582284540020-8acbe03f4924?w=800&auto=format&fit=crop'
  ],
  Carrots: [
    'https://images.unsplash.com/photo-1598170845058-32b9d6a5da37?w=800&auto=format&fit=crop',
    'https://images.unsplash.com/photo-1447175008436-054170c2e979?w=800&auto=format&fit=crop'
  ],
  Tea: [
    'https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop',
    'https://images.unsplash.com/photo-1544787219-7f47ccb76574?w=800&auto=format&fit=crop'
  ],
  Coconut: [
    'https://images.unsplash.com/photo-1550258987-190a2d41a8ba?w=800&auto=format&fit=crop',
    'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=800&auto=format&fit=crop'
  ],
  Chili: [
    'https://images.unsplash.com/photo-1588252303782-cb80119abd6d?w=800&auto=format&fit=crop'
  ],
  Potatoes: [
    'https://images.unsplash.com/photo-1518977676601-b53f82aba655?w=800&auto=format&fit=crop'
  ],
  Banana: [
    'https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=800&auto=format&fit=crop'
  ]
};

export const AddEditListingModal: React.FC<AddEditListingModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
  crops,
  regions,
  editingListing
}) => {
  const [cropId, setCropId] = useState('');
  const [regionId, setRegionId] = useState('');
  const [quantity, setQuantity] = useState(500);
  const [unit, setUnit] = useState('kg');
  const [claimedGrade, setClaimedGrade] = useState('A');
  const [pickupWindowStart, setPickupWindowStart] = useState('');
  const [pickupWindowEnd, setPickupWindowEnd] = useState('');
  const [minPrice, setMinPrice] = useState<number | undefined>(250);
  const [description, setDescription] = useState('');
  const [photoUrls, setPhotoUrls] = useState<string[]>([]);
  const [customPhotoUrl, setCustomPhotoUrl] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const [priceEstimate, setPriceEstimate] = useState<PriceEstimateResult | null>(null);
  const [isLoadingEstimate, setIsLoadingEstimate] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleDeviceFiles = async (files: FileList | null) => {
    if (!files || files.length === 0) return;
    setIsUploading(true);
    setError(null);
    try {
      const fileList = Array.from(files);

      // Instant preview: generate local base64/data URLs immediately (0ms delay)
      const localDataUrls = await Promise.all(
        fileList.map(
          (file) =>
            new Promise<string>((resolve, reject) => {
              const reader = new FileReader();
              reader.onload = () => resolve(reader.result as string);
              reader.onerror = reject;
              reader.readAsDataURL(file);
            })
        )
      );

      // Check if current list only has the default single preset photo
      const isPresetPhoto =
        photoUrls.length === 1 &&
        Object.values(PRESET_PHOTOS).some((urls) => urls.includes(photoUrls[0]));

      // If just preset photo, replace with user's uploaded photo(s) so their photo is COVER!
      if (isPresetPhoto) {
        setPhotoUrls([...localDataUrls]);
      } else {
        setPhotoUrls((prev) => [...localDataUrls, ...prev]);
      }

      // Concurrently upload to server in background to get server path if available
      try {
        const uploadPromises = fileList.map((file) => api.uploadPhoto(file));
        const serverUrls = await Promise.all(uploadPromises);

        // Replace the local data URLs with server URLs
        setPhotoUrls((current) => {
          return current.map((url) => {
            const index = localDataUrls.indexOf(url);
            if (index !== -1 && serverUrls[index]) {
              return serverUrls[index];
            }
            return url;
          });
        });
      } catch (uploadErr) {
        console.warn('Backend upload fell back to local image data:', uploadErr);
      }
    } catch (err: any) {
      setError(err?.message || 'Failed to process photo from device');
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  useEffect(() => {
    if (editingListing) {
      setCropId(crops.find(c => c.name === editingListing.cropName)?.id || crops[0]?.id || '');
      setRegionId(regions.find(r => r.name === editingListing.regionName)?.id || regions[0]?.id || '');
      setQuantity(editingListing.quantity);
      setUnit(editingListing.unit);
      setClaimedGrade(editingListing.claimedGrade);
      setPickupWindowStart(editingListing.pickupWindowStart.substring(0, 10));
      setPickupWindowEnd(editingListing.pickupWindowEnd.substring(0, 10));
      setMinPrice(editingListing.minPrice);
      setDescription(editingListing.description || '');
      setPhotoUrls(editingListing.photos ? editingListing.photos.map(p => p.url) : []);
    } else {
      // Defaults for new listing
      if (crops.length > 0) setCropId(crops[0].id);
      if (regions.length > 0) setRegionId(regions[0].id);
      setQuantity(500);
      setUnit('kg');
      setClaimedGrade('A');
      const today = new Date();
      const nextWeek = new Date();
      nextWeek.setDate(today.getDate() + 5);
      setPickupWindowStart(today.toISOString().substring(0, 10));
      setPickupWindowEnd(nextWeek.toISOString().substring(0, 10));
      setMinPrice(250);
      setDescription('Premium quality wholesale produce freshly harvested under standard Good Agricultural Practices (GAP). Inspected for size uniformity, color, and moisture content.');
      setPhotoUrls([PRESET_PHOTOS['Tomatoes'][0]]);
    }
  }, [editingListing, crops, regions, isOpen]);

  // Live Query: Fetch Today's Fair Price on that crop in farmer's area
  useEffect(() => {
    if (!cropId || !regionId) return;
    const selectedCrop = crops.find(c => c.id === cropId);
    const selectedRegion = regions.find(r => r.id === regionId);
    if (!selectedCrop || !selectedRegion) return;

    let isCurrent = true;
    setIsLoadingEstimate(true);
    api.getQuickPriceEstimate({
      cropId,
      regionId,
      cropName: selectedCrop.name,
      regionName: selectedRegion.name,
      grade: claimedGrade,
      quantity: Number(quantity) || 100
    })
      .then(res => {
        if (isCurrent) {
          setPriceEstimate(res);
          setIsLoadingEstimate(false);
        }
      })
      .catch(err => {
        console.warn('Quick price estimate failed:', err);
        if (isCurrent) setIsLoadingEstimate(false);
      });

    return () => { isCurrent = false; };
  }, [cropId, regionId, claimedGrade, quantity, crops, regions]);

  // When crop changes, update sample photos if empty
  const handleCropChange = (selectedCropId: string) => {
    setCropId(selectedCropId);
    const selectedCrop = crops.find(c => c.id === selectedCropId);
    if (selectedCrop && PRESET_PHOTOS[selectedCrop.name] && photoUrls.length <= 1) {
      setPhotoUrls([...PRESET_PHOTOS[selectedCrop.name]]);
    }
  };

  const handleApplyRecommendedPrice = () => {
    if (priceEstimate) {
      setMinPrice(Math.round(priceEstimate.suggestedPriceMin));
    }
  };

  const handleAddCustomPhoto = () => {
    if (customPhotoUrl.trim() && !photoUrls.includes(customPhotoUrl.trim())) {
      setPhotoUrls([...photoUrls, customPhotoUrl.trim()]);
      setCustomPhotoUrl('');
    }
  };

  const handleRemovePhoto = (index: number) => {
    setPhotoUrls(photoUrls.filter((_, i) => i !== index));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (photoUrls.length === 0) {
      setError('Please provide at least one photo of the produce.');
      return;
    }

    if (new Date(pickupWindowEnd) <= new Date(pickupWindowStart)) {
      setError('Pickup window end date must be after start date.');
      return;
    }

    setIsSubmitting(true);
    try {
      await onSubmit({
        id: editingListing?.id,
        cropId,
        regionId,
        quantity: Number(quantity),
        unit,
        claimedGrade,
        pickupWindowStart: new Date(pickupWindowStart).toISOString(),
        pickupWindowEnd: new Date(pickupWindowEnd).toISOString(),
        minPrice: minPrice ? Number(minPrice) : undefined,
        description,
        photoUrls
      });
      onClose();
    } catch (err: any) {
      setError(err.message || 'Failed to save listing');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div
        className="modal-content"
        style={{ maxWidth: '720px' }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          padding: '1.25rem 1.75rem',
          borderBottom: '1px solid var(--border)',
          background: 'var(--bg-raised)'
        }}>
          <div>
            <h2 style={{ fontSize: '1.4rem', fontWeight: 800, color: 'var(--text)' }}>
              {editingListing ? 'Edit Produce Listing' : 'Create New Produce Listing'}
            </h2>
            <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '2px' }}>
              List your wholesale batch with high-res photos and automated AI price review.
            </p>
          </div>
          <button
            onClick={onClose}
            style={{
              background: 'var(--border)',
              border: 'none',
              borderRadius: '50%',
              width: '34px',
              height: '34px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: 'var(--text-muted)',
              cursor: 'pointer'
            }}
          >
            <X size={18} />
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} style={{ padding: '1.75rem', display: 'flex', flexDirection: 'column', gap: '16px' }}>
          {error && (
            <div style={{
              background: 'var(--danger-soft)',
              border: '1px solid var(--danger-border)',
              color: '#e08776',
              padding: '10px 14px',
              borderRadius: '10px',
              fontSize: '0.85rem'
            }}>
              {error}
            </div>
          )}

          {/* Row 1: Crop & Region */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
            <div className="form-group">
              <label className="form-label">Crop / Produce *</label>
              <select
                className="form-select"
                value={cropId}
                onChange={(e) => handleCropChange(e.target.value)}
                required
              >
                {crops.map(c => (
                  <option key={c.id} value={c.id} style={{ background: 'var(--bg-raised)' }}>
                    {c.name} ({c.category})
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label className="form-label">Origin / Region *</label>
              <select
                className="form-select"
                value={regionId}
                onChange={(e) => setRegionId(e.target.value)}
                required
              >
                {regions.map(r => (
                  <option key={r.id} value={r.id} style={{ background: 'var(--bg-raised)' }}>
                    {r.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Row 2: Quantity, Unit, Grade */}
          <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 1fr 1fr', gap: '16px' }}>
            <div className="form-group">
              <label className="form-label">Quantity *</label>
              <input
                type="number"
                min="0.01"
                step="any"
                className="form-input"
                value={quantity}
                onChange={(e) => setQuantity(Number(e.target.value))}
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label">Unit</label>
              <select
                className="form-select"
                value={unit}
                onChange={(e) => setUnit(e.target.value)}
              >
                <option value="kg" style={{ background: 'var(--bg-raised)' }}>Kilograms (kg)</option>
                <option value="pieces" style={{ background: 'var(--bg-raised)' }}>Pieces</option>
                <option value="crates" style={{ background: 'var(--bg-raised)' }}>Crates</option>
                <option value="bags" style={{ background: 'var(--bg-raised)' }}>Bags (50kg)</option>
              </select>
            </div>

            <div className="form-group">
              <label className="form-label">Claimed Grade *</label>
              <select
                className="form-select"
                value={claimedGrade}
                onChange={(e) => setClaimedGrade(e.target.value)}
                required
              >
                <option value="A" style={{ background: 'var(--bg-raised)' }}>Grade A (Export / Supermarket)</option>
                <option value="B" style={{ background: 'var(--bg-raised)' }}>Grade B (Standard Wholesale)</option>
                <option value="C" style={{ background: 'var(--bg-raised)' }}>Grade C (Processing / Bulk)</option>
              </select>
            </div>
          </div>

          {/* Today's AI Fair Price Discovery Card (Component A Agentic AI) */}
          <div style={{
            background: 'var(--bg-card)',
            border: '1px solid var(--accent-border)',
            borderRadius: '12px',
            padding: '14px 18px',
            position: 'relative',
            overflow: 'hidden'
          }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '8px', flexWrap: 'wrap', gap: '8px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <span style={{
                  padding: '3px 8px',
                  borderRadius: '4px',
                  background: 'var(--accent-border)',
                  color: 'var(--accent-text)',
                  fontSize: '0.72rem',
                  fontWeight: 800,
                  display: 'flex',
                  alignItems: 'center',
                  gap: '4px'
                }}>
                  <Sparkles size={12} /> AGENTIC AI LIVE PRICE DISCOVERY
                </span>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                  Today's Fair Price in <strong>{regions.find(r => r.id === regionId)?.name || 'Your Area'}</strong>
                </span>
              </div>

              {priceEstimate && (
                <div style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-muted)' }}>
                  {Math.round(priceEstimate.confidence * 100)}% Market Confidence
                </div>
              )}
            </div>

            {isLoadingEstimate ? (
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', padding: '6px 0', color: 'var(--text-muted)', fontSize: '0.85rem' }}>
                <div style={{ width: '16px', height: '16px', borderRadius: '50%', border: '2px solid var(--accent)', borderTopColor: 'transparent', animation: 'spin 0.8s linear infinite' }} />
                <span>Evaluating wholesale auctions & historical momentum for this crop...</span>
              </div>
            ) : priceEstimate ? (
              <div>
                <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', flexWrap: 'wrap', gap: '10px', marginBottom: '8px' }}>
                  <div>
                    <span style={{ fontSize: '1.4rem', fontWeight: 800, color: 'var(--text)' }}>
                      Rs. {priceEstimate.suggestedPriceMin.toFixed(0)} – {priceEstimate.suggestedPriceMax.toFixed(0)}
                    </span>
                    <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginLeft: '4px' }}>/ {unit}</span>
                    <span style={{ fontSize: '0.78rem', color: 'var(--text-faint)', marginLeft: '10px' }}>
                      (Wholesale Benchmark: Rs. {priceEstimate.benchmarkWholesale.toFixed(0)}/{unit})
                    </span>
                  </div>

                  <button
                    type="button"
                    onClick={handleApplyRecommendedPrice}
                    style={{
                      background: 'var(--accent-border)',
                      border: '1px solid var(--accent-border)',
                      color: 'var(--accent-text)',
                      padding: '5px 12px',
                      borderRadius: '6px',
                      fontSize: '0.78rem',
                      fontWeight: 700,
                      cursor: 'pointer',
                      display: 'flex',
                      alignItems: 'center',
                      gap: '5px',
                      transition: 'all 0.2s'
                    }}
                    onMouseEnter={(e) => { e.currentTarget.style.background = 'var(--accent)'; e.currentTarget.style.color = 'var(--text)'; }}
                    onMouseLeave={(e) => { e.currentTarget.style.background = 'var(--accent-border)'; e.currentTarget.style.color = 'var(--accent-text)'; }}
                  >
                    <TrendingUp size={13} /> Apply Suggested Floor (Rs. {priceEstimate.suggestedPriceMin.toFixed(0)})
                  </button>
                </div>

                <div style={{ fontSize: '0.78rem', color: 'var(--text-muted)', lineHeight: 1.4 }}>
                  {priceEstimate.reasoningSummary}
                </div>
              </div>
            ) : (
              <div style={{ fontSize: '0.8rem', color: 'var(--text-faint)' }}>
                Select a crop and region above to view today's AI-estimated fair trading price.
              </div>
            )}
          </div>

          {/* Row 3: Floor Price & Pickup Window */}
          <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 1fr 1fr', gap: '16px' }}>
            <div className="form-group">
              <label className="form-label">Floor Price (Rs. / {unit})</label>
              <input
                type="number"
                min="1"
                step="any"
                className="form-input"
                placeholder="e.g. 250"
                value={minPrice || ''}
                onChange={(e) => setMinPrice(e.target.value ? Number(e.target.value) : undefined)}
              />
            </div>

            <div className="form-group">
              <label className="form-label">Pickup Start Date *</label>
              <input
                type="date"
                className="form-input"
                value={pickupWindowStart}
                onChange={(e) => setPickupWindowStart(e.target.value)}
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label">Pickup End Date *</label>
              <input
                type="date"
                className="form-input"
                value={pickupWindowEnd}
                onChange={(e) => setPickupWindowEnd(e.target.value)}
                required
              />
            </div>
          </div>

          {/* Description */}
          <div className="form-group">
            <label className="form-label">Produce Description & Specifications</label>
            <textarea
              className="form-textarea"
              rows={3}
              placeholder="Describe variety, harvest conditions, post-harvest handling, packaging details..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>

          {/* Multi-Photo Manager */}
          <div className="form-group">
            <label className="form-label" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span>Produce Photos ({photoUrls.length} attached) *</span>
              <span style={{ color: 'var(--accent)', textTransform: 'none', fontWeight: 500, fontSize: '0.8rem' }}>Min 1 photo required</span>
            </label>

            {/* Hidden Device File Picker */}
            <input
              type="file"
              ref={fileInputRef}
              accept="image/*"
              multiple
              style={{ display: 'none' }}
              onChange={(e) => handleDeviceFiles(e.target.files)}
            />

            {/* Device Upload Drag & Drop Box */}
            <div
              onClick={() => fileInputRef.current?.click()}
              onDragOver={(e) => {
                e.preventDefault();
                setIsDragging(true);
              }}
              onDragLeave={() => setIsDragging(false)}
              onDrop={(e) => {
                e.preventDefault();
                setIsDragging(false);
                handleDeviceFiles(e.dataTransfer.files);
              }}
              style={{
                border: isDragging ? '2px dashed var(--accent)' : '2px dashed var(--accent-border)',
                background: isDragging ? 'var(--accent-soft)' : 'var(--accent-soft)',
                borderRadius: '12px',
                padding: '1.2rem',
                textAlign: 'center',
                cursor: 'pointer',
                transition: 'all 0.2s',
                marginBottom: '12px',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                justifyContent: 'center',
                gap: '8px'
              }}
            >
              <div style={{
                width: '42px',
                height: '42px',
                borderRadius: '10px',
                background: 'var(--accent-border)',
                color: 'var(--accent-text)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center'
              }}>
                <ImagePlus size={22} />
              </div>
              <div>
                <div style={{ fontWeight: 700, fontSize: '0.9rem', color: 'var(--text)' }}>
                  {isUploading ? 'Uploading selected photos...' : 'Choose Photos from Your Device'}
                </div>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '2px' }}>
                  Click to browse from computer/phone or drag & drop (JPG, PNG, WebP)
                </div>
              </div>
            </div>

            {/* Secondary URL Input */}
            <div style={{ display: 'flex', gap: '8px', marginBottom: '10px' }}>
              <input
                type="url"
                placeholder="Or paste external photo URL (https://...)"
                className="form-input"
                style={{ flex: 1, fontSize: '0.85rem' }}
                value={customPhotoUrl}
                onChange={(e) => setCustomPhotoUrl(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    e.preventDefault();
                    handleAddCustomPhoto();
                  }
                }}
              />
              <button
                type="button"
                className="btn btn-secondary"
                onClick={handleAddCustomPhoto}
                style={{ whiteSpace: 'nowrap', fontSize: '0.85rem' }}
              >
                <Plus size={15} /> Add URL
              </button>
            </div>

            {/* Photo Preview Strip */}
            <div style={{
              display: 'flex',
              gap: '12px',
              overflowX: 'auto',
              padding: '8px 4px',
              minHeight: '90px'
            }}>
              {photoUrls.map((url, idx) => (
                <div
                  key={idx}
                  onClick={() => {
                    if (idx !== 0) {
                      const reordered = [photoUrls[idx], ...photoUrls.filter((_, i) => i !== idx)];
                      setPhotoUrls(reordered);
                    }
                  }}
                  title={idx === 0 ? 'Main Cover Photo' : 'Click to set as main Cover photo'}
                  style={{
                    position: 'relative',
                    width: '100px',
                    height: '82px',
                    borderRadius: '10px',
                    overflow: 'hidden',
                    flexShrink: 0,
                    border: idx === 0 ? '2px solid var(--accent)' : '1px solid var(--border)',
                    boxShadow: idx === 0 ? '0 0 14px var(--accent-border)' : '0 4px 10px rgba(0, 0, 0, 0.4)',
                    cursor: idx === 0 ? 'default' : 'pointer',
                    transition: 'all 0.2s ease'
                  }}
                >
                  <img
                    src={url}
                    alt={`Produce photo ${idx + 1}`}
                    onError={(e) => {
                      (e.currentTarget as HTMLImageElement).src = 'https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=800&auto=format&fit=crop';
                    }}
                    style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                  />
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleRemovePhoto(idx);
                    }}
                    title="Remove photo"
                    style={{
                      position: 'absolute',
                      top: '4px',
                      right: '4px',
                      background: 'rgba(0, 0, 0, 0.75)',
                      border: 'none',
                      borderRadius: '50%',
                      width: '22px',
                      height: '22px',
                      color: '#e08776',
                      cursor: 'pointer',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      zIndex: 2
                    }}
                  >
                    <Trash2 size={12} />
                  </button>
                  {idx === 0 ? (
                    <span style={{
                      position: 'absolute',
                      bottom: '3px',
                      left: '3px',
                      fontSize: '0.62rem',
                      background: 'var(--accent)',
                      color: 'var(--text)',
                      padding: '2px 5px',
                      borderRadius: '4px',
                      fontWeight: 700,
                      boxShadow: '0 2px 5px rgba(0, 0, 0, 0.5)'
                    }}>
                      COVER
                    </span>
                  ) : (
                    <span style={{
                      position: 'absolute',
                      bottom: '3px',
                      left: '3px',
                      fontSize: '0.58rem',
                      background: 'rgba(0, 0, 0, 0.65)',
                      color: 'var(--text-muted)',
                      padding: '1px 4px',
                      borderRadius: '4px',
                      fontWeight: 600
                    }}>
                      Set Cover
                    </span>
                  )}
                </div>
              ))}
            </div>
          </div>

          {/* Actions */}
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px', marginTop: '12px' }}>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={onClose}
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={isSubmitting}
              style={{ minWidth: '160px' }}
            >
              {isSubmitting ? 'Saving...' : editingListing ? 'Update Listing' : 'Publish Listing'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
