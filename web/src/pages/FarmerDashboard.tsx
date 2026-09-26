import React, { useState, useEffect } from 'react';
import { api, type Listing, type Crop, type Region } from '../utils/api';
import { useAuth } from '../context/AuthContext';
import { ProductCard3D } from '../components/ProductCard3D';
import { ProductDetailModal } from '../components/ProductDetailModal';
import { AddEditListingModal } from '../components/AddEditListingModal';
import {
  Sprout, Plus, Search, Layers, TrendingUp, AlertCircle,
  CheckCircle2, RefreshCw, Grid, ListIcon, Sparkles, Eye, Edit, Trash2
} from '../components/Icons';

interface FarmerDashboardProps {
  onOpenTodayPrices?: () => void;
}

export const FarmerDashboard: React.FC<FarmerDashboardProps> = ({ onOpenTodayPrices }) => {
  const { user } = useAuth();

  // Tabs: 'my' = My Produce, 'marketplace' = Public Marketplace
  const [activeTab, setActiveTab] = useState<'my' | 'marketplace'>('my');
  const [viewMode, setViewMode] = useState<'grid' | 'table'>('grid');

  // Data states
  const [myListings, setMyListings] = useState<Listing[]>([]);
  const [allListings, setAllListings] = useState<Listing[]>([]);
  const [crops, setCrops] = useState<Crop[]>([]);
  const [regions, setRegions] = useState<Region[]>([]);
  const [loading, setLoading] = useState(true);

  // Filters
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCrop, setSelectedCrop] = useState('');
  const [selectedRegion, setSelectedRegion] = useState('');
  const [selectedStatus, setSelectedStatus] = useState('');

  // Modals
  const [selectedListing, setSelectedListing] = useState<Listing | null>(null);
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [editingListing, setEditingListing] = useState<Listing | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const fetchData = async () => {
    setLoading(true);
    try {
      const [cropsRes, regionsRes, myRes, allRes] = await Promise.all([
        api.getCrops(),
        api.getRegions(),
        api.getMyListings({ pageSize: 50 }),
        api.getListings({ status: 'Published', pageSize: 50 })
      ]);

      setCrops(cropsRes);
      setRegions(regionsRes);
      setMyListings(myRes.items);
      setAllListings(allRes.items);
    } catch (err) {
      console.error('Failed to load farmer dashboard data', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const showNotification = (msg: string) => {
    setSuccessMessage(msg);
    setTimeout(() => setSuccessMessage(null), 3000);
  };

  // Create or Update Listing
  const handleSaveListing = async (data: {
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
  }) => {
    if (data.id) {
      // Update
      await api.updateListing(data.id, {
        cropId: data.cropId,
        regionId: data.regionId,
        quantity: data.quantity,
        unit: data.unit,
        claimedGrade: data.claimedGrade,
        pickupWindowStart: data.pickupWindowStart,
        pickupWindowEnd: data.pickupWindowEnd,
        minPrice: data.minPrice,
        description: data.description,
        photoUrls: data.photoUrls
      });
      showNotification('Produce listing updated successfully!');
    } else {
      // Create
      await api.createListing({
        cropId: data.cropId,
        regionId: data.regionId,
        quantity: data.quantity,
        unit: data.unit,
        claimedGrade: data.claimedGrade,
        pickupWindowStart: data.pickupWindowStart,
        pickupWindowEnd: data.pickupWindowEnd,
        minPrice: data.minPrice,
        description: data.description,
        photoUrls: data.photoUrls
      });
      showNotification('New produce listing published to marketplace!');
    }
    await fetchData();
  };

  // Withdraw Listing
  const handleWithdrawListing = async (listing: Listing) => {
    if (confirm(`Withdraw listing for ${listing.cropName}? It will no longer be available for wholesale discovery.`)) {
      try {
        await api.withdrawListing(listing.id);
        showNotification(`${listing.cropName} listing withdrawn.`);
        await fetchData();
      } catch (err: any) {
        alert(err.message || 'Failed to withdraw listing');
      }
    }
  };

  // Filter listings based on active tab
  const displayedListings = (activeTab === 'my' ? myListings : allListings).filter(l => {
    if (searchQuery && !l.cropName.toLowerCase().includes(searchQuery.toLowerCase())) return false;
    if (selectedCrop && l.cropName !== selectedCrop) return false;
    if (selectedRegion && l.regionName !== selectedRegion) return false;
    if (selectedStatus && l.status !== selectedStatus) return false;
    return true;
  });

  // Calculate Metrics
  const totalVolume = myListings.reduce((acc, curr) => acc + curr.quantity, 0);
  const activeCount = myListings.filter(l => l.status === 'Published').length;
  const estimatedValuation = myListings.reduce((acc, curr) => acc + (curr.minPrice || 0) * curr.quantity, 0);

  return (
    <div style={{ padding: '2rem', maxWidth: '1400px', margin: '0 auto', width: '100%' }}>

      {/* Success Notification Toast */}
      {successMessage && (
        <div style={{
          position: 'fixed',
          bottom: '24px',
          right: '24px',
          background: 'var(--accent)',
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
          <span>{successMessage}</span>
        </div>
      )}

      {/* Top Welcome Header */}
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
              Farmer Wholesale Workspace
            </h1>
            <span style={{
              fontSize: '0.75rem',
              fontWeight: 700,
              background: 'var(--accent-border)',
              color: 'var(--accent-text)',
              padding: '3px 8px',
              borderRadius: '6px'
            }}>
              LIVE DB
            </span>
          </div>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginTop: '4px' }}>
            Logged in as <strong style={{ color: 'var(--text)' }}>{user?.fullName}</strong> ({user?.region || 'Central Province'}). Manage your produce inventory and monitor national price corridors.
          </p>
        </div>

        <div style={{ display: 'flex', gap: '10px', alignItems: 'center', flexWrap: 'wrap' }}>
          {onOpenTodayPrices && (
            <button
              className="btn btn-secondary"
              style={{
                padding: '12px 18px',
                fontSize: '0.92rem',
                background: 'var(--accent-soft)',
                borderColor: 'var(--accent-border)',
                color: 'var(--accent-text)',
                display: 'flex',
                alignItems: 'center',
                gap: '8px'
              }}
              onClick={onOpenTodayPrices}
            >
              <TrendingUp size={16} /> Today's Fair Prices
            </button>
          )}

          <button
            className="btn btn-primary"
            style={{ padding: '12px 22px', fontSize: '0.95rem' }}
            onClick={() => {
              setEditingListing(null);
              setIsAddModalOpen(true);
            }}
          >
            <Plus size={18} /> Add New Produce Listing
          </button>
        </div>
      </div>

      {/* 3D KPI Metric Cards */}
      <div style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
        gap: '16px',
        marginBottom: '2rem'
      }}>
        <div className="glass-card" style={{ padding: '20px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span style={{ fontSize: '0.8rem', fontWeight: 700, color: 'var(--text-faint)' }}>TOTAL PRODUCE BATCHES</span>
            <div style={{ padding: '8px', borderRadius: '10px', background: 'var(--accent-soft)', color: 'var(--accent-text)' }}>
              <Sprout size={20} />
            </div>
          </div>
          <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--text)', marginTop: '8px' }}>
            {myListings.length}
          </div>
          <div style={{ fontSize: '0.75rem', color: 'var(--accent)', marginTop: '4px', fontWeight: 600 }}>
            {activeCount} active in national market
          </div>
        </div>

        <div className="glass-card" style={{ padding: '20px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span style={{ fontSize: '0.8rem', fontWeight: 700, color: 'var(--text-faint)' }}>ACTIVE IN MARKETPLACE</span>
            <div style={{ padding: '8px', borderRadius: '10px', background: 'var(--accent-soft)', color: 'var(--accent-text)' }}>
              <TrendingUp size={20} />
            </div>
          </div>
          <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--text)', marginTop: '8px' }}>
            {activeCount}
          </div>
          <div style={{ fontSize: '0.75rem', color: 'var(--accent-text)', marginTop: '4px', fontWeight: 600 }}>
            Published & visible to buyers
          </div>
        </div>

        <div className="glass-card" style={{ padding: '20px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span style={{ fontSize: '0.8rem', fontWeight: 700, color: 'var(--text-faint)' }}>TOTAL REGISTERED VOLUME</span>
            <div style={{ padding: '8px', borderRadius: '10px', background: 'var(--bg-hover)', color: 'var(--text-muted)' }}>
              <Layers size={20} />
            </div>
          </div>
          <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--text)', marginTop: '8px' }}>
            {totalVolume.toLocaleString()} <span style={{ fontSize: '1rem', color: 'var(--text-muted)' }}>kg/pcs</span>
          </div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '4px', fontWeight: 600 }}>
            Combined across all regional lots
          </div>
        </div>

        <div className="glass-card" style={{ padding: '20px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span style={{ fontSize: '0.8rem', fontWeight: 700, color: 'var(--text-faint)' }}>ESTIMATED BATCH VALUATION</span>
            <div style={{ padding: '8px', borderRadius: '10px', background: 'var(--bg-hover)', color: 'var(--text-muted)' }}>
              <Sparkles size={20} />
            </div>
          </div>
          <div style={{ fontSize: '2rem', fontWeight: 800, color: 'var(--accent-text)', marginTop: '8px' }}>
            Rs. {estimatedValuation.toLocaleString()}
          </div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '4px', fontWeight: 600 }}>
            Based on current floor prices
          </div>
        </div>
      </div>

      {/* Navigation Tabs (My Produce vs Public Marketplace) */}
      <div style={{
        display: 'flex',
        flexWrap: 'wrap',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: '1rem',
        marginBottom: '1.5rem',
        borderBottom: '1px solid var(--border)',
        paddingBottom: '12px'
      }}>
        <div style={{ display: 'flex', gap: '12px' }}>
          <button
            onClick={() => setActiveTab('my')}
            style={{
              padding: '10px 18px',
              borderRadius: '10px',
              border: 'none',
              fontSize: '0.95rem',
              fontWeight: 700,
              cursor: 'pointer',
              background: activeTab === 'my' ? 'var(--accent)' : 'var(--border)',
              color: activeTab === 'my' ? 'var(--text)' : 'var(--text-muted)',
              boxShadow: activeTab === 'my' ? '0 4px 14px var(--accent-border)' : 'none',
              transition: 'all 0.2s ease',
              display: 'flex',
              alignItems: 'center',
              gap: '8px'
            }}
          >
            <Sprout size={18} />
            <span>My Produce & Batches ({myListings.length})</span>
          </button>

          <button
            onClick={() => setActiveTab('marketplace')}
            style={{
              padding: '10px 18px',
              borderRadius: '10px',
              border: 'none',
              fontSize: '0.95rem',
              fontWeight: 700,
              cursor: 'pointer',
              background: activeTab === 'marketplace' ? 'var(--bg-hover)' : 'var(--border)',
              color: activeTab === 'marketplace' ? 'var(--text)' : 'var(--text-muted)',
              boxShadow: activeTab === 'marketplace' ? '0 4px 14px var(--border-strong)' : 'none',
              transition: 'all 0.2s ease',
              display: 'flex',
              alignItems: 'center',
              gap: '8px'
            }}
          >
            <span>Public Marketplace ({allListings.length})</span>
          </button>
        </div>

        {/* View Mode & Refresh */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <div style={{
            display: 'flex',
            background: 'rgba(0, 0, 0, 0.3)',
            borderRadius: '8px',
            padding: '2px',
            border: '1px solid var(--border)'
          }}>
            <button
              onClick={() => setViewMode('grid')}
              style={{
                padding: '6px 10px',
                borderRadius: '6px',
                border: 'none',
                background: viewMode === 'grid' ? 'var(--border)' : 'transparent',
                color: viewMode === 'grid' ? 'var(--text)' : 'var(--text-faint)',
                cursor: 'pointer'
              }}
              title="Grid View"
            >
              <Grid size={16} />
            </button>
            <button
              onClick={() => setViewMode('table')}
              style={{
                padding: '6px 10px',
                borderRadius: '6px',
                border: 'none',
                background: viewMode === 'table' ? 'var(--border)' : 'transparent',
                color: viewMode === 'table' ? 'var(--text)' : 'var(--text-faint)',
                cursor: 'pointer'
              }}
              title="Table View"
            >
              <ListIcon size={16} />
            </button>
          </div>

          <button
            className="btn btn-secondary"
            onClick={fetchData}
            title="Refresh Data"
            style={{ padding: '8px 12px' }}
          >
            <RefreshCw size={15} />
          </button>
        </div>
      </div>

      {/* Public Marketplace Guidance Banner (Farmer cannot buy own) */}
      {activeTab === 'marketplace' && (
        <div style={{
          background: 'var(--bg-hover)',
          border: '1px solid var(--border-strong)',
          borderRadius: '12px',
          padding: '14px 18px',
          marginBottom: '1.5rem',
          display: 'flex',
          alignItems: 'center',
          gap: '12px',
          color: 'var(--text-muted)',
          fontSize: '0.875rem'
        }}>
          <AlertCircle size={20} style={{ color: 'var(--text-muted)', flexShrink: 0 }} />
          <div>
            <strong>National Wholesale Marketplace View:</strong> You can explore live listings and market rates posted by other registered farmers across Sri Lanka. As a registered producer, purchasing produce is restricted to buyers.
          </div>
        </div>
      )}

      {/* Filter and Search Bar */}
      <div className="glass-card" style={{ padding: '16px', marginBottom: '1.5rem' }}>
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
          gap: '12px'
        }}>
          {/* Search */}
          <div style={{ position: 'relative' }}>
            <input
              type="text"
              placeholder="Search produce name..."
              className="form-input"
              style={{ width: '100%', paddingLeft: '36px' }}
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
            <Search size={16} style={{ position: 'absolute', left: '12px', top: '12px', color: 'var(--text-faint)' }} />
          </div>

          {/* Crop Filter */}
          <select
            className="form-select"
            value={selectedCrop}
            onChange={(e) => setSelectedCrop(e.target.value)}
          >
            <option value="" style={{ background: 'var(--bg-raised)' }}>All Crops</option>
            {crops.map(c => (
              <option key={c.id} value={c.name} style={{ background: 'var(--bg-raised)' }}>
                {c.name}
              </option>
            ))}
          </select>

          {/* Region Filter */}
          <select
            className="form-select"
            value={selectedRegion}
            onChange={(e) => setSelectedRegion(e.target.value)}
          >
            <option value="" style={{ background: 'var(--bg-raised)' }}>All Regions</option>
            {regions.map(r => (
              <option key={r.id} value={r.name} style={{ background: 'var(--bg-raised)' }}>
                {r.name}
              </option>
            ))}
          </select>

          {/* Status Filter (only for My Produce tab) */}
          {activeTab === 'my' && (
            <select
              className="form-select"
              value={selectedStatus}
              onChange={(e) => setSelectedStatus(e.target.value)}
            >
              <option value="" style={{ background: 'var(--bg-raised)' }}>All Statuses</option>
              <option value="Published" style={{ background: 'var(--bg-raised)' }}>Published</option>
              <option value="Withdrawn" style={{ background: 'var(--bg-raised)' }}>Withdrawn</option>
            </select>
          )}
        </div>
      </div>

      {/* Main Content Area */}
      {loading ? (
        <div style={{ textAlign: 'center', padding: '4rem 1rem', color: 'var(--text-muted)' }}>
          <RefreshCw size={32} className="animate-spin" style={{ color: 'var(--accent)', marginBottom: '12px' }} />
          <div>Synchronizing real produce records from database...</div>
        </div>
      ) : displayedListings.length === 0 ? (
        <div className="glass-card" style={{ textAlign: 'center', padding: '4rem 2rem' }}>
          <Sprout size={48} style={{ color: 'var(--text-faint)', marginBottom: '1rem' }} />
          <h3 style={{ fontSize: '1.2rem', fontWeight: 700, color: 'var(--text)' }}>
            No produce listings found
          </h3>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginTop: '6px', maxWidth: '400px', margin: '6px auto 16px' }}>
            {activeTab === 'my'
              ? 'You have not added any listings matching the filter. Create your first listing to start fair-price discovery!'
              : 'No listings match the current search filters.'}
          </p>
          {activeTab === 'my' && (
            <button className="btn btn-primary" onClick={() => setIsAddModalOpen(true)}>
              <Plus size={16} /> Create Produce Listing
            </button>
          )}
        </div>
      ) : viewMode === 'grid' ? (
        /* 3D Grid View */
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
          gap: '24px'
        }}>
          {displayedListings.map(listing => (
            <ProductCard3D
              key={listing.id}
              listing={listing}
              onSelect={setSelectedListing}
              onEdit={listing.farmerId === user?.id ? (l) => {
                setEditingListing(l);
                setIsAddModalOpen(true);
              } : undefined}
              onWithdraw={listing.farmerId === user?.id ? handleWithdrawListing : undefined}
            />
          ))}
        </div>
      ) : (
        /* Table View */
        <div className="glass-card" style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border)', background: 'rgba(0, 0, 0, 0.2)' }}>
                <th style={{ padding: '14px 18px', color: 'var(--text-faint)', fontWeight: 600 }}>PRODUCE</th>
                <th style={{ padding: '14px 18px', color: 'var(--text-faint)', fontWeight: 600 }}>CATEGORY</th>
                <th style={{ padding: '14px 18px', color: 'var(--text-faint)', fontWeight: 600 }}>REGION</th>
                <th style={{ padding: '14px 18px', color: 'var(--text-faint)', fontWeight: 600 }}>QUANTITY</th>
                <th style={{ padding: '14px 18px', color: 'var(--text-faint)', fontWeight: 600 }}>GRADE</th>
                <th style={{ padding: '14px 18px', color: 'var(--text-faint)', fontWeight: 600 }}>FLOOR PRICE</th>
                <th style={{ padding: '14px 18px', color: 'var(--text-faint)', fontWeight: 600 }}>STATUS</th>
                <th style={{ padding: '14px 18px', color: 'var(--text-faint)', fontWeight: 600, textAlign: 'right' }}>ACTIONS</th>
              </tr>
            </thead>
            <tbody>
              {displayedListings.map(listing => (
                <tr
                  key={listing.id}
                  style={{
                    borderBottom: '1px solid var(--border)',
                    transition: 'background 0.2s',
                    cursor: 'pointer'
                  }}
                  onClick={() => setSelectedListing(listing)}
                  onMouseEnter={(e) => (e.currentTarget.style.background = 'var(--border)')}
                  onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}
                >
                  <td style={{ padding: '14px 18px', fontWeight: 700, color: 'var(--text)' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                      <img
                        src={listing.photos && listing.photos[0] ? listing.photos[0].url : ''}
                        alt=""
                        style={{ width: '38px', height: '38px', borderRadius: '8px', objectFit: 'cover' }}
                      />
                      <span>{listing.cropName}</span>
                    </div>
                  </td>
                  <td style={{ padding: '14px 18px', color: 'var(--text-muted)' }}>{listing.cropCategory}</td>
                  <td style={{ padding: '14px 18px', color: 'var(--text-muted)' }}>{listing.regionName}</td>
                  <td style={{ padding: '14px 18px', color: 'var(--text)', fontWeight: 600 }}>
                    {listing.quantity} {listing.unit}
                  </td>
                  <td style={{ padding: '14px 18px' }}>
                    <span className={`badge badge-grade-${listing.claimedGrade.toLowerCase()}`}>
                      Grade {listing.claimedGrade}
                    </span>
                  </td>
                  <td style={{ padding: '14px 18px', color: 'var(--accent-text)', fontWeight: 700 }}>
                    Rs. {listing.minPrice ? listing.minPrice.toFixed(0) : '—'}
                  </td>
                  <td style={{ padding: '14px 18px' }}>
                    <span className={`badge badge-${listing.status.toLowerCase()}`}>
                      {listing.status}
                    </span>
                  </td>
                  <td style={{ padding: '14px 18px', textAlign: 'right' }}>
                    <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px' }}>
                      <button
                        className="btn btn-secondary"
                        style={{ padding: '6px 10px', fontSize: '0.75rem' }}
                        onClick={(e) => {
                          e.stopPropagation();
                          setSelectedListing(listing);
                        }}
                      >
                        <Eye size={14} />
                      </button>
                      {listing.farmerId === user?.id && (
                        <>
                          <button
                            className="btn btn-secondary"
                            style={{ padding: '6px 10px', fontSize: '0.75rem' }}
                            onClick={(e) => {
                              e.stopPropagation();
                              setEditingListing(listing);
                              setIsAddModalOpen(true);
                            }}
                          >
                            <Edit size={14} />
                          </button>
                          {listing.status !== 'Withdrawn' && (
                            <button
                              className="btn btn-danger"
                              style={{ padding: '6px 10px', fontSize: '0.75rem' }}
                              onClick={(e) => {
                                e.stopPropagation();
                                handleWithdrawListing(listing);
                              }}
                            >
                              <Trash2 size={14} />
                            </button>
                          )}
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

      {/* Product Detail Modal */}
      {selectedListing && (
        <ProductDetailModal
          listing={selectedListing}
          onClose={() => setSelectedListing(null)}
          onEdit={selectedListing.farmerId === user?.id ? (l) => {
            setEditingListing(l);
            setIsAddModalOpen(true);
          } : undefined}
          onWithdraw={selectedListing.farmerId === user?.id ? handleWithdrawListing : undefined}
        />
      )}

      {/* Add / Edit Listing Modal */}
      <AddEditListingModal
        isOpen={isAddModalOpen}
        onClose={() => {
          setIsAddModalOpen(false);
          setEditingListing(null);
        }}
        onSubmit={handleSaveListing}
        crops={crops}
        regions={regions}
        editingListing={editingListing}
      />
    </div>
  );
};
