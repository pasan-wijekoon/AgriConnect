import React, { useState, useEffect } from 'react';
import { api, type Listing, type Crop, type Region, type TodayPriceCatalogItem } from '../utils/api';
import { useAuth } from '../context/AuthContext';
import { ProductDetailModal } from '../components/ProductDetailModal';
import {
  ShieldCheck, CheckCircle2, XCircle, Sparkles, RefreshCw,
  MapPin, Layers, Eye, TrendingUp, Plus, Edit, Trash2
} from '../components/Icons';

export const AdminDashboard: React.FC = () => {
  const { user } = useAuth();

  const [activeTab, setActiveTab] = useState<'queue' | 'ai-review' | 'reference' | 'catalog'>('queue');
  const [queueStatus, setQueueStatus] = useState<'PendingApproval' | 'Published' | 'Withdrawn'>('PendingApproval');

  const [listings, setListings] = useState<Listing[]>([]);
  const [crops, setCrops] = useState<Crop[]>([]);
  const [regions, setRegions] = useState<Region[]>([]);
  const [loading, setLoading] = useState(true);
  const [actionMessage, setActionMessage] = useState<string | null>(null);

  const [selectedListing, setSelectedListing] = useState<Listing | null>(null);

  // Today's Prices catalog management
  const [catalogItems, setCatalogItems] = useState<TodayPriceCatalogItem[]>([]);
  const [catalogLoading, setCatalogLoading] = useState(false);
  const [editingCatalogItem, setEditingCatalogItem] = useState<TodayPriceCatalogItem | null>(null);
  const [showCatalogForm, setShowCatalogForm] = useState(false);
  const [catalogForm, setCatalogForm] = useState({
    name: '', category: 'Vegetables', unit: 'kg', defaultRegion: '', imageUrl: '', displayOrder: 0
  });

  const fetchCatalog = async () => {
    setCatalogLoading(true);
    try {
      const items = await api.getTodayPriceCatalog();
      setCatalogItems(items);
    } catch (err) {
      console.error('Failed to fetch Today\'s Prices catalog', err);
    } finally {
      setCatalogLoading(false);
    }
  };

  useEffect(() => {
    if (activeTab === 'catalog') fetchCatalog();
  }, [activeTab]);

  const openAddCatalogForm = () => {
    setEditingCatalogItem(null);
    setCatalogForm({ name: '', category: 'Vegetables', unit: 'kg', defaultRegion: '', imageUrl: '', displayOrder: catalogItems.length + 1 });
    setShowCatalogForm(true);
  };

  const openEditCatalogForm = (item: TodayPriceCatalogItem) => {
    setEditingCatalogItem(item);
    setCatalogForm({
      name: item.name,
      category: item.category,
      unit: item.unit,
      defaultRegion: item.defaultRegion,
      imageUrl: item.imageUrl || '',
      displayOrder: item.displayOrder
    });
    setShowCatalogForm(true);
  };

  const handleSaveCatalogItem = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingCatalogItem) {
        await api.updateTodayPriceCatalogItem(editingCatalogItem.id, catalogForm);
        notify(`Updated "${catalogForm.name}" in Today's Prices.`);
      } else {
        await api.createTodayPriceCatalogItem(catalogForm);
        notify(`Added "${catalogForm.name}" to Today's Prices.`);
      }
      setShowCatalogForm(false);
      await fetchCatalog();
    } catch (err: any) {
      alert(err.message || 'Failed to save catalog item');
    }
  };

  const handleToggleCatalogActive = async (item: TodayPriceCatalogItem) => {
    try {
      await api.updateTodayPriceCatalogItem(item.id, { isActive: !item.isActive });
      await fetchCatalog();
    } catch (err: any) {
      alert(err.message || 'Failed to update catalog item');
    }
  };

  const handleDeleteCatalogItem = async (item: TodayPriceCatalogItem) => {
    if (!confirm(`Remove "${item.name}" from Today's Prices?`)) return;
    try {
      await api.deleteTodayPriceCatalogItem(item.id);
      notify(`Removed "${item.name}" from Today's Prices.`);
      await fetchCatalog();
    } catch (err: any) {
      alert(err.message || 'Failed to remove catalog item');
    }
  };

  const fetchAllData = async () => {
    setLoading(true);
    try {
      const [cropsRes, regionsRes, listingsRes] = await Promise.all([
        api.getCrops(),
        api.getRegions(),
        api.getListings({ status: queueStatus, pageSize: 50 })
      ]);

      setCrops(cropsRes);
      setRegions(regionsRes);
      setListings(listingsRes.items);
    } catch (err) {
      console.error('Failed to fetch officer dashboard data', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchAllData();
  }, [queueStatus]);

  const notify = (msg: string) => {
    setActionMessage(msg);
    setTimeout(() => setActionMessage(null), 3000);
  };

  // Approve listing
  const handleApprove = async (listing: Listing) => {
    try {
      await api.approveListing(listing.id);
      notify(`Listing ${listing.cropName} (${listing.id.substring(0, 8)}) APPROVED and published to wholesale marketplace!`);
      await fetchAllData();
    } catch (err: any) {
      alert(err.message || 'Approval failed');
    }
  };

  // Reject listing
  const handleReject = async (listing: Listing) => {
    if (confirm(`Reject produce listing for ${listing.cropName}?`)) {
      try {
        await api.rejectListing(listing.id);
        notify(`Listing ${listing.cropName} REJECTED.`);
        await fetchAllData();
      } catch (err: any) {
        alert(err.message || 'Rejection failed');
      }
    }
  };

  return (
    <div style={{ padding: '2rem', maxWidth: '1400px', margin: '0 auto', width: '100%' }}>
      {/* Toast Notification */}
      {actionMessage && (
        <div style={{
          position: 'fixed',
          bottom: '24px',
          right: '24px',
          background: 'var(--bg-hover)',
          color: 'var(--text)',
          padding: '12px 20px',
          borderRadius: '12px',
          boxShadow: 'var(--shadow-md)',
          display: 'flex',
          alignItems: 'center',
          gap: '8px',
          fontWeight: 600,
          zIndex: 100,
          animation: 'fadeIn 0.2s ease-out'
        }}>
          <CheckCircle2 size={18} />
          <span>{actionMessage}</span>
        </div>
      )}

      {/* Top Banner */}
      <div style={{
        display: 'flex',
        flexWrap: 'wrap',
        justifyContent: 'space-between',
        alignItems: 'center',
        gap: '1rem',
        marginBottom: '2rem'
      }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <h1 style={{ fontSize: '1.85rem', fontWeight: 800, color: 'var(--text)', letterSpacing: '-0.02em' }}>
              Agricultural Officer Command Portal
            </h1>
            <span style={{
              fontSize: '0.75rem',
              fontWeight: 700,
              background: 'var(--border-strong)',
              color: 'var(--text-muted)',
              padding: '3px 8px',
              borderRadius: '6px'
            }}>
              OFFICIAL VERIFICATION
            </span>
          </div>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginTop: '4px' }}>
            Officer: <strong style={{ color: 'var(--text)' }}>{user?.fullName}</strong>. Review incoming farmer produce batches, examine AI fair-price corridors, and maintain national agricultural master data.
          </p>
        </div>

        <button className="btn btn-secondary" onClick={fetchAllData} style={{ padding: '10px 16px' }}>
          <RefreshCw size={16} /> Sync Queue
        </button>
      </div>

      {/* Tabs */}
      <div style={{
        display: 'flex',
        gap: '12px',
        borderBottom: '1px solid var(--border)',
        paddingBottom: '14px',
        marginBottom: '1.5rem'
      }}>
        <button
          onClick={() => setActiveTab('queue')}
          style={{
            padding: '10px 18px',
            borderRadius: '10px',
            border: 'none',
            fontSize: '0.95rem',
            fontWeight: 700,
            cursor: 'pointer',
            background: activeTab === 'queue' ? 'var(--bg-hover)' : 'var(--border)',
            color: activeTab === 'queue' ? 'var(--text)' : 'var(--text-muted)',
            boxShadow: activeTab === 'queue' ? '0 4px 14px var(--border-strong)' : 'none',
            transition: 'all 0.2s ease',
            display: 'flex',
            alignItems: 'center',
            gap: '8px'
          }}
        >
          <ShieldCheck size={18} />
          <span>Listing Verification Queue ({listings.length})</span>
        </button>

        <button
          onClick={() => setActiveTab('ai-review')}
          style={{
            padding: '10px 18px',
            borderRadius: '10px',
            border: 'none',
            fontSize: '0.95rem',
            fontWeight: 700,
            cursor: 'pointer',
            background: activeTab === 'ai-review' ? 'var(--accent)' : 'var(--border)',
            color: activeTab === 'ai-review' ? 'var(--text)' : 'var(--text-muted)',
            boxShadow: activeTab === 'ai-review' ? '0 4px 14px var(--accent-border)' : 'none',
            transition: 'all 0.2s ease',
            display: 'flex',
            alignItems: 'center',
            gap: '8px'
          }}
        >
          <Sparkles size={18} />
          <span>AI Price Review Panel</span>
        </button>

        <button
          onClick={() => setActiveTab('reference')}
          style={{
            padding: '10px 18px',
            borderRadius: '10px',
            border: 'none',
            fontSize: '0.95rem',
            fontWeight: 700,
            cursor: 'pointer',
            background: activeTab === 'reference' ? 'var(--bg-hover)' : 'var(--border)',
            color: activeTab === 'reference' ? 'var(--text)' : 'var(--text-muted)',
            boxShadow: activeTab === 'reference' ? '0 4px 14px var(--border-strong)' : 'none',
            transition: 'all 0.2s ease',
            display: 'flex',
            alignItems: 'center',
            gap: '8px'
          }}
        >
          <Layers size={18} />
          <span>Crops & Regions Data</span>
        </button>

        <button
          onClick={() => setActiveTab('catalog')}
          style={{
            padding: '10px 18px',
            borderRadius: '10px',
            border: 'none',
            fontSize: '0.95rem',
            fontWeight: 700,
            cursor: 'pointer',
            background: activeTab === 'catalog' ? 'var(--bg-hover)' : 'var(--border)',
            color: activeTab === 'catalog' ? 'var(--text)' : 'var(--text-muted)',
            boxShadow: activeTab === 'catalog' ? '0 4px 14px var(--border-strong)' : 'none',
            transition: 'all 0.2s ease',
            display: 'flex',
            alignItems: 'center',
            gap: '8px'
          }}
        >
          <TrendingUp size={18} />
          <span>Today's Prices Catalog</span>
        </button>
      </div>

      {/* Tab 1: Listing Queue */}
      {activeTab === 'queue' && (
        <div>
          {/* Sub-Queue Status Filter */}
          <div style={{ display: 'flex', gap: '8px', marginBottom: '1.25rem' }}>
            <button
              onClick={() => setQueueStatus('PendingApproval')}
              style={{
                padding: '6px 14px',
                borderRadius: '8px',
                fontSize: '0.85rem',
                fontWeight: 600,
                border: 'none',
                cursor: 'pointer',
                background: queueStatus === 'PendingApproval' ? 'var(--warn-border)' : 'var(--border)',
                color: queueStatus === 'PendingApproval' ? '#e0ac57' : 'var(--text-muted)'
              }}
            >
              ⏳ Pending Review
            </button>
            <button
              onClick={() => setQueueStatus('Published')}
              style={{
                padding: '6px 14px',
                borderRadius: '8px',
                fontSize: '0.85rem',
                fontWeight: 600,
                border: 'none',
                cursor: 'pointer',
                background: queueStatus === 'Published' ? 'var(--accent-border)' : 'var(--border)',
                color: queueStatus === 'Published' ? 'var(--accent-text)' : 'var(--text-muted)'
              }}
            >
              ✓ Published Live
            </button>
            <button
              onClick={() => setQueueStatus('Withdrawn')}
              style={{
                padding: '6px 14px',
                borderRadius: '8px',
                fontSize: '0.85rem',
                fontWeight: 600,
                border: 'none',
                cursor: 'pointer',
                background: queueStatus === 'Withdrawn' ? 'var(--danger-border)' : 'var(--border)',
                color: queueStatus === 'Withdrawn' ? '#e08776' : 'var(--text-muted)'
              }}
            >
              ✗ Withdrawn Lots
            </button>
          </div>

          {/* Queue Table */}
          {loading ? (
            <div style={{ textAlign: 'center', padding: '4rem', color: 'var(--text-muted)' }}>
              <RefreshCw size={32} className="animate-spin" style={{ color: 'var(--text-muted)', marginBottom: '12px' }} />
              <div>Fetching verification queue items...</div>
            </div>
          ) : listings.length === 0 ? (
            <div className="glass-card" style={{ textAlign: 'center', padding: '4rem' }}>
              <ShieldCheck size={48} style={{ color: 'var(--accent)', marginBottom: '1rem' }} />
              <h3 style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--text)' }}>
                Queue is clear ({queueStatus})
              </h3>
              <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginTop: '6px' }}>
                There are no produce batches currently in this status.
              </p>
            </div>
          ) : (
            <div className="glass-card" style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid var(--border)', background: 'rgba(0, 0, 0, 0.2)' }}>
                    <th style={{ padding: '14px 18px', color: 'var(--text-faint)' }}>PRODUCE BATCH</th>
                    <th style={{ padding: '14px 18px', color: 'var(--text-faint)' }}>REGION</th>
                    <th style={{ padding: '14px 18px', color: 'var(--text-faint)' }}>BATCH SIZE</th>
                    <th style={{ padding: '14px 18px', color: 'var(--text-faint)' }}>CLAIMED GRADE</th>
                    <th style={{ padding: '14px 18px', color: 'var(--text-faint)' }}>FLOOR PRICE</th>
                    <th style={{ padding: '14px 18px', color: 'var(--text-faint)' }}>AI CORRIDOR</th>
                    <th style={{ padding: '14px 18px', color: 'var(--text-faint)', textAlign: 'right' }}>OFFICER ACTION</th>
                  </tr>
                </thead>
                <tbody>
                  {listings.map(l => (
                    <tr
                      key={l.id}
                      style={{
                        borderBottom: '1px solid var(--border)',
                        transition: 'background 0.2s',
                        cursor: 'pointer'
                      }}
                      onClick={() => setSelectedListing(l)}
                      onMouseEnter={(e) => (e.currentTarget.style.background = 'var(--border)')}
                      onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}
                    >
                      <td style={{ padding: '14px 18px' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                          <img
                            src={l.photos && l.photos[0] ? l.photos[0].url : ''}
                            alt=""
                            style={{ width: '42px', height: '42px', borderRadius: '8px', objectFit: 'cover' }}
                          />
                          <div>
                            <div style={{ fontWeight: 700, color: 'var(--text)' }}>{l.cropName}</div>
                            <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>{l.cropCategory}</div>
                          </div>
                        </div>
                      </td>
                      <td style={{ padding: '14px 18px', color: 'var(--text-muted)' }}>{l.regionName}</td>
                      <td style={{ padding: '14px 18px', fontWeight: 600, color: 'var(--text)' }}>
                        {l.quantity} {l.unit}
                      </td>
                      <td style={{ padding: '14px 18px' }}>
                        <span className={`badge badge-grade-${l.claimedGrade.toLowerCase()}`}>
                          Grade {l.claimedGrade}
                        </span>
                      </td>
                      <td style={{ padding: '14px 18px', color: 'var(--accent-text)', fontWeight: 700 }}>
                        Rs. {l.minPrice ? l.minPrice.toFixed(0) : '—'}
                      </td>
                      <td style={{ padding: '14px 18px' }}>
                        {l.priceSuggestion ? (
                          <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)', fontWeight: 600 }}>
                            Rs. {l.priceSuggestion.suggestedPriceMin.toFixed(0)} - {l.priceSuggestion.suggestedPriceMax.toFixed(0)}
                          </div>
                        ) : (
                          <span style={{ color: 'var(--text-faint)' }}>Pending AI</span>
                        )}
                      </td>
                      <td style={{ padding: '14px 18px', textAlign: 'right' }}>
                        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px' }}>
                          <button
                            className="btn btn-secondary"
                            style={{ padding: '6px 12px', fontSize: '0.75rem' }}
                            onClick={(e) => {
                              e.stopPropagation();
                              setSelectedListing(l);
                            }}
                          >
                            <Eye size={13} /> View
                          </button>
                          {l.status === 'PendingApproval' && (
                            <>
                              <button
                                className="btn btn-primary"
                                style={{ padding: '6px 12px', fontSize: '0.75rem' }}
                                onClick={(e) => {
                                  e.stopPropagation();
                                  handleApprove(l);
                                }}
                              >
                                <CheckCircle2 size={13} /> Approve
                              </button>
                              <button
                                className="btn btn-danger"
                                style={{ padding: '6px 12px', fontSize: '0.75rem' }}
                                onClick={(e) => {
                                  e.stopPropagation();
                                  handleReject(l);
                                }}
                              >
                                <XCircle size={13} /> Reject
                              </button>
                            </>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Tab 2: AI Price Review Panel */}
      {activeTab === 'ai-review' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <div className="glass-card" style={{ padding: '20px', background: 'var(--bg-hover)' }}>
            <h3 style={{ color: 'var(--text-muted)', fontSize: '1.1rem', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Sparkles size={20} /> Agentic AI Fair-Price Suggestion Workflow
            </h3>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginTop: '6px', lineHeight: 1.5 }}>
              The Agentic AI service analyzes real-time regional supply, historical Mandi/Manning market indices, weather disruptions, and grade-tier differentials to generate optimal floor-price corridors. Officers review and validate these recommendations before publishing.
            </p>
          </div>

          <div style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))',
            gap: '20px'
          }}>
            {listings.map(l => (
              <div key={l.id} className="glass-card" style={{ padding: '20px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '12px' }}>
                  <div>
                    <h4 style={{ fontSize: '1.2rem', fontWeight: 800, color: 'var(--text)' }}>{l.cropName}</h4>
                    <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                      {l.regionName} • {l.quantity} {l.unit} • Grade {l.claimedGrade}
                    </div>
                  </div>
                  <span className={`badge badge-grade-${l.claimedGrade.toLowerCase()}`}>
                    Grade {l.claimedGrade}
                  </span>
                </div>

                {/* Price Comparison */}
                <div style={{
                  background: 'rgba(0, 0, 0, 0.3)',
                  padding: '14px',
                  borderRadius: '12px',
                  marginBottom: '14px',
                  display: 'grid',
                  gridTemplateColumns: '1fr 1fr',
                  gap: '10px'
                }}>
                  <div>
                    <div style={{ fontSize: '0.7rem', color: 'var(--text-faint)', fontWeight: 600 }}>FARMER'S FLOOR PRICE</div>
                    <div style={{ fontSize: '1.25rem', fontWeight: 800, color: 'var(--text)' }}>
                      Rs. {l.minPrice ? l.minPrice.toFixed(0) : '—'}
                    </div>
                  </div>
                  <div>
                    <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)', fontWeight: 600 }}>AI SUGGESTED CORRIDOR</div>
                    <div style={{ fontSize: '1.25rem', fontWeight: 800, color: 'var(--text-muted)' }}>
                      {l.priceSuggestion
                        ? `Rs. ${l.priceSuggestion.suggestedPriceMin.toFixed(0)} - ${l.priceSuggestion.suggestedPriceMax.toFixed(0)}`
                        : 'Evaluating...'}
                    </div>
                  </div>
                </div>

                {l.priceSuggestion && (
                  <div style={{ marginBottom: '16px' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.75rem', marginBottom: '4px' }}>
                      <span style={{ color: 'var(--text-muted)' }}>AI Confidence Rating</span>
                      <span style={{ color: 'var(--accent-text)', fontWeight: 700 }}>
                        {Math.round(l.priceSuggestion.confidence * 100)}%
                      </span>
                    </div>
                    <div style={{ width: '100%', height: '6px', background: 'var(--border)', borderRadius: '3px', overflow: 'hidden' }}>
                      <div style={{
                        width: `${Math.round(l.priceSuggestion.confidence * 100)}%`,
                        height: '100%',
                        background: 'var(--accent)'
                      }} />
                    </div>
                    <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '10px', lineHeight: 1.45 }}>
                      {l.priceSuggestion.reasoningSummary}
                    </p>
                  </div>
                )}

                <div style={{ display: 'flex', gap: '8px', marginTop: 'auto' }}>
                  <button
                    className="btn btn-secondary"
                    style={{ flex: 1, padding: '8px', fontSize: '0.8rem' }}
                    onClick={() => setSelectedListing(l)}
                  >
                    Inspect Full Details
                  </button>
                  {l.status === 'PendingApproval' && (
                    <button
                      className="btn btn-primary"
                      style={{ padding: '8px 14px', fontSize: '0.8rem' }}
                      onClick={() => handleApprove(l)}
                    >
                      <CheckCircle2 size={14} /> Approve
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Tab 3: Reference Data (Crops & Regions) */}
      {activeTab === 'reference' && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(340px, 1fr))', gap: '24px' }}>
          {/* Crops Master Data */}
          <div className="glass-card" style={{ padding: '20px' }}>
            <h3 style={{ fontSize: '1.2rem', fontWeight: 700, color: 'var(--text)', marginBottom: '14px', display: 'flex', alignItems: 'center', gap: '8px' }}>
              Master Crops Catalog ({crops.length})
            </h3>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', maxHeight: '500px', overflowY: 'auto' }}>
              {crops.map(c => (
                <div key={c.id} style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  padding: '10px 14px',
                  background: 'var(--border)',
                  borderRadius: '10px',
                  border: '1px solid var(--border)'
                }}>
                  <div style={{ fontWeight: 600, color: 'var(--text)' }}>{c.name}</div>
                  <span style={{
                    fontSize: '0.75rem',
                    padding: '2px 8px',
                    borderRadius: '6px',
                    background: 'var(--accent-soft)',
                    color: 'var(--accent-text)',
                    fontWeight: 600
                  }}>
                    {c.category}
                  </span>
                </div>
              ))}
            </div>
          </div>

          {/* Regions Master Data */}
          <div className="glass-card" style={{ padding: '20px' }}>
            <h3 style={{ fontSize: '1.2rem', fontWeight: 700, color: 'var(--text)', marginBottom: '14px', display: 'flex', alignItems: 'center', gap: '8px' }}>
              Regional Collection Hubs ({regions.length})
            </h3>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', maxHeight: '500px', overflowY: 'auto' }}>
              {regions.map(r => (
                <div key={r.id} style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  padding: '10px 14px',
                  background: 'var(--border)',
                  borderRadius: '10px',
                  border: '1px solid var(--border)'
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', fontWeight: 600, color: 'var(--text)' }}>
                    <MapPin size={16} style={{ color: 'var(--text-muted)' }} />
                    <span>{r.name}</span>
                  </div>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Verified Depot</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Tab 4: Today's Prices Catalog (Admin-managed) */}
      {activeTab === 'catalog' && (
        <div className="glass-card" style={{ padding: '20px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
            <div>
              <h3 style={{ fontSize: '1.2rem', fontWeight: 700, color: 'var(--text)' }}>
                Today's Prices Catalog ({catalogItems.length})
              </h3>
              <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginTop: '4px' }}>
                Controls which crops appear on the marketplace's Today's Prices page. Prices are always computed live by the Fair-Price Estimation Agent — this only manages the list itself.
              </p>
            </div>
            <button className="btn btn-primary" onClick={openAddCatalogForm}>
              <Plus size={16} /> Add Crop
            </button>
          </div>

          {catalogLoading ? (
            <div style={{ padding: '40px', textAlign: 'center', color: 'var(--text-muted)' }}>Loading catalog…</div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {catalogItems.map(item => (
                <div key={item.id} style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '14px',
                  padding: '10px 14px',
                  background: 'var(--bg-input)',
                  borderRadius: 'var(--radius-sm)',
                  border: '1px solid var(--border)',
                  opacity: item.isActive ? 1 : 0.55
                }}>
                  {item.imageUrl && (
                    <img src={item.imageUrl} alt={item.name} style={{ width: '40px', height: '40px', borderRadius: '8px', objectFit: 'cover', flexShrink: 0 }} />
                  )}
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ fontWeight: 600, color: 'var(--text)' }}>{item.name}</div>
                    <div style={{ fontSize: '0.78rem', color: 'var(--text-faint)' }}>
                      {item.category} • {item.unit} • Default region: {item.defaultRegion} • Order {item.displayOrder}
                    </div>
                  </div>
                  <span className={`badge ${item.isActive ? 'badge-published' : 'badge-draft'}`}>
                    {item.isActive ? 'Visible' : 'Hidden'}
                  </span>
                  <button className="btn btn-ghost" style={{ padding: '6px 10px' }} onClick={() => handleToggleCatalogActive(item)}>
                    {item.isActive ? 'Hide' : 'Show'}
                  </button>
                  <button className="btn btn-secondary" style={{ padding: '6px 10px' }} onClick={() => openEditCatalogForm(item)}>
                    <Edit size={14} />
                  </button>
                  <button className="btn btn-danger" style={{ padding: '6px 10px' }} onClick={() => handleDeleteCatalogItem(item)}>
                    <Trash2 size={14} />
                  </button>
                </div>
              ))}
              {catalogItems.length === 0 && (
                <div style={{ padding: '40px', textAlign: 'center', color: 'var(--text-muted)' }}>
                  No crops in the catalog yet. Add one to populate the Today's Prices page.
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* Add/Edit Catalog Item Form */}
      {showCatalogForm && (
        <div className="modal-overlay" onClick={() => setShowCatalogForm(false)}>
          <div className="modal-content" style={{ maxWidth: '480px' }} onClick={(e) => e.stopPropagation()}>
            <div style={{ padding: '16px 20px', borderBottom: '1px solid var(--border)' }}>
              <h3 style={{ fontSize: '1rem', fontWeight: 600, color: 'var(--text)' }}>
                {editingCatalogItem ? 'Edit Crop' : 'Add Crop to Today\'s Prices'}
              </h3>
            </div>
            <form onSubmit={handleSaveCatalogItem} style={{ padding: '20px', display: 'flex', flexDirection: 'column', gap: '4px' }}>
              <div className="form-group">
                <label className="form-label">Name</label>
                <input
                  className="form-input"
                  value={catalogForm.name}
                  onChange={(e) => setCatalogForm({ ...catalogForm, name: e.target.value })}
                  placeholder="e.g. Tomatoes"
                  required
                />
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                <div className="form-group">
                  <label className="form-label">Category</label>
                  <select
                    className="form-select"
                    value={catalogForm.category}
                    onChange={(e) => setCatalogForm({ ...catalogForm, category: e.target.value })}
                  >
                    {['Vegetables', 'Fruits', 'Spices', 'Grains', 'Beverages'].map(c => (
                      <option key={c} value={c}>{c}</option>
                    ))}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">Unit</label>
                  <input
                    className="form-input"
                    value={catalogForm.unit}
                    onChange={(e) => setCatalogForm({ ...catalogForm, unit: e.target.value })}
                    placeholder="kg"
                  />
                </div>
              </div>
              <div className="form-group">
                <label className="form-label">Default Region</label>
                <input
                  className="form-input"
                  value={catalogForm.defaultRegion}
                  onChange={(e) => setCatalogForm({ ...catalogForm, defaultRegion: e.target.value })}
                  placeholder="e.g. Dambulla"
                  required
                />
              </div>
              <div className="form-group">
                <label className="form-label">Image URL</label>
                <input
                  className="form-input"
                  value={catalogForm.imageUrl}
                  onChange={(e) => setCatalogForm({ ...catalogForm, imageUrl: e.target.value })}
                  placeholder="https://..."
                />
              </div>
              <div className="form-group">
                <label className="form-label">Display Order</label>
                <input
                  type="number"
                  className="form-input"
                  value={catalogForm.displayOrder}
                  onChange={(e) => setCatalogForm({ ...catalogForm, displayOrder: Number(e.target.value) })}
                />
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '10px' }}>
                <button type="button" className="btn btn-secondary" onClick={() => setShowCatalogForm(false)}>Cancel</button>
                <button type="submit" className="btn btn-primary">{editingCatalogItem ? 'Save Changes' : 'Add Crop'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal */}
      {selectedListing && (
        <ProductDetailModal
          listing={selectedListing}
          onClose={() => setSelectedListing(null)}
          onApprove={handleApprove}
          onReject={handleReject}
        />
      )}
    </div>
  );
};
