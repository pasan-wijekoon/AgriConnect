import React, { useState, useEffect } from 'react';
import { api, type Listing, type Region } from '../utils/api';
import { ProductCard3D } from '../components/ProductCard3D';
import { ProductDetailModal } from '../components/ProductDetailModal';
import { ShoppingBag, Search, RefreshCw, TrendingUp } from '../components/Icons';

interface BuyerDashboardProps {
  onOpenTodayPrices?: () => void;
}

export const BuyerDashboard: React.FC<BuyerDashboardProps> = ({ onOpenTodayPrices }) => {
  const [listings, setListings] = useState<Listing[]>([]);
  const [regions, setRegions] = useState<Region[]>([]);
  const [loading, setLoading] = useState(true);

  // Filters
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('All');
  const [selectedRegion, setSelectedRegion] = useState<string>('');
  const [selectedGrade, setSelectedGrade] = useState<string>('');
  const [sortBy, setSortBy] = useState<string>('date');
  const [sortDir, setSortDir] = useState<string>('desc');

  // Selected for modal
  const [selectedListing, setSelectedListing] = useState<Listing | null>(null);

  const categories = ['All', 'Vegetables', 'Fruits', 'Grains', 'Spices', 'Beverages'];

  const fetchListings = async () => {
    setLoading(true);
    try {
      const [regionsRes, listingsRes] = await Promise.all([
        api.getRegions(),
        api.getListings({
          status: 'Published',
          search: searchQuery || undefined,
          regionId: selectedRegion ? regions.find(r => r.name === selectedRegion)?.id : undefined,
          grade: selectedGrade || undefined,
          sortBy,
          sortDir,
          pageSize: 50
        })
      ]);

      setRegions(regionsRes);
      setListings(listingsRes.items);
    } catch (err) {
      console.error('Failed to load marketplace listings', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchListings();
  }, [selectedRegion, selectedGrade, sortBy, sortDir]);

  // Client-side category filtering
  const filteredListings = listings.filter(l => {
    if (selectedCategory !== 'All' && l.cropCategory !== selectedCategory) return false;
    if (searchQuery && !l.cropName.toLowerCase().includes(searchQuery.toLowerCase())) return false;
    return true;
  });

  return (
    <div style={{ padding: '2rem', maxWidth: '1400px', margin: '0 auto', width: '100%' }}>
      {/* Hero Banner with Depth Effect */}
      <div
        className="glass-card"
        style={{
          padding: '2.5rem',
          borderRadius: '20px',
          background: 'var(--bg-card)',
          border: '1px solid var(--accent-border)',
          marginBottom: '2rem',
          position: 'relative',
          overflow: 'hidden'
        }}
      >
        <div style={{
          position: 'absolute',
          right: '-40px',
          bottom: '-40px',
          width: '260px',
          height: '260px',
          background: 'radial-gradient(circle, var(--accent-border) 0%, transparent 70%)',
          filter: 'blur(30px)',
          pointerEvents: 'none'
        }} />

        <div style={{ maxWidth: '750px', position: 'relative', zIndex: 1 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px' }}>
            <span style={{
              fontSize: '0.75rem',
              fontWeight: 700,
              background: 'var(--border)',
              color: 'var(--text-muted)',
              padding: '4px 10px',
              borderRadius: '9999px',
              border: '1px solid var(--border-strong)'
            }}>
              DIRECT FARMER WHOLESALE MARKETPLACE
            </span>
            <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Verified Sri Lankan Produce</span>
          </div>

          <h1 style={{ fontSize: '2.4rem', fontWeight: 800, color: 'var(--text)', letterSpacing: '-0.02em', lineHeight: 1.15 }}>
            Discover Fresh Produce with <span style={{ color: 'var(--accent-text)' }}>Fair-Price Assurance</span>
          </h1>

          <p style={{ color: 'var(--text-muted)', fontSize: '1rem', marginTop: '12px', lineHeight: 1.5 }}>
            Procure verified grade batches directly from regional farm collection centres across Nuwara Eliya, Kandy, Kurunegala, and Jaffna. Inspect high-definition photos and transparent market rates.
          </p>

          {onOpenTodayPrices && (
            <div style={{ marginTop: '18px' }}>
              <button
                className="btn btn-primary"
                style={{
                  padding: '10px 20px',
                  fontSize: '0.9rem',
                  display: 'inline-flex',
                  alignItems: 'center',
                  gap: '8px'
                }}
                onClick={onOpenTodayPrices}
              >
                <TrendingUp size={16} /> View Today's Fair Price Benchmarks
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Category Pills Bar */}
      <div style={{
        display: 'flex',
        gap: '10px',
        overflowX: 'auto',
        paddingBottom: '8px',
        marginBottom: '1.5rem'
      }}>
        {categories.map(cat => (
          <button
            key={cat}
            onClick={() => setSelectedCategory(cat)}
            style={{
              padding: '8px 18px',
              borderRadius: '9999px',
              border: selectedCategory === cat ? '1px solid var(--accent)' : '1px solid var(--border)',
              background: selectedCategory === cat ? 'var(--accent-border)' : 'var(--bg-hover)',
              color: selectedCategory === cat ? 'var(--accent-text)' : 'var(--text-muted)',
              fontSize: '0.85rem',
              fontWeight: 600,
              cursor: 'pointer',
              transition: 'all 0.2s ease',
              whiteSpace: 'nowrap'
            }}
          >
            {cat === 'All' ? 'All Categories' : cat}
          </button>
        ))}
      </div>

      {/* Advanced Filter & Search Controls */}
      <div className="glass-card" style={{ padding: '18px', marginBottom: '2rem' }}>
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
          gap: '12px',
          alignItems: 'center'
        }}>
          {/* Keyword Search */}
          <div style={{ position: 'relative' }}>
            <input
              type="text"
              placeholder="Search produce (e.g. Tomatoes)..."
              className="form-input"
              style={{ width: '100%', paddingLeft: '38px' }}
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && fetchListings()}
            />
            <Search size={16} style={{ position: 'absolute', left: '12px', top: '12px', color: 'var(--text-faint)' }} />
          </div>

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

          {/* Grade Filter */}
          <select
            className="form-select"
            value={selectedGrade}
            onChange={(e) => setSelectedGrade(e.target.value)}
          >
            <option value="" style={{ background: 'var(--bg-raised)' }}>All Quality Grades</option>
            <option value="A" style={{ background: 'var(--bg-raised)' }}>Grade A (Premium / Export)</option>
            <option value="B" style={{ background: 'var(--bg-raised)' }}>Grade B (Standard Market)</option>
            <option value="C" style={{ background: 'var(--bg-raised)' }}>Grade C (Processing)</option>
          </select>

          {/* Sort Control */}
          <select
            className="form-select"
            value={`${sortBy}:${sortDir}`}
            onChange={(e) => {
              const [sb, sd] = e.target.value.split(':');
              setSortBy(sb);
              setSortDir(sd);
            }}
          >
            <option value="date:desc" style={{ background: 'var(--bg-raised)' }}>Newest Harvests First</option>
            <option value="price:asc" style={{ background: 'var(--bg-raised)' }}>Price: Low to High</option>
            <option value="price:desc" style={{ background: 'var(--bg-raised)' }}>Price: High to Low</option>
            <option value="quantity:desc" style={{ background: 'var(--bg-raised)' }}>Largest Quantity First</option>
          </select>

          {/* Search / Refresh Button */}
          <div style={{ display: 'flex', gap: '8px' }}>
            <button
              className="btn btn-primary"
              style={{ flex: 1, padding: '10px 14px' }}
              onClick={fetchListings}
            >
              <Search size={15} /> Find Produce
            </button>
            <button
              className="btn btn-secondary"
              style={{ padding: '10px 14px' }}
              onClick={fetchListings}
              title="Refresh"
            >
              <RefreshCw size={15} />
            </button>
          </div>
        </div>
      </div>

      {/* Results Header */}
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: '1.25rem',
        padding: '0 4px'
      }}>
        <div style={{ fontSize: '0.9rem', color: 'var(--text-muted)' }}>
          Showing <strong style={{ color: 'var(--text)' }}>{filteredListings.length}</strong> available wholesale batches
        </div>
        <div style={{ fontSize: '0.8rem', color: 'var(--text-faint)' }}>
          Sorted by most recent listing
        </div>
      </div>

      {/* 3D Products Grid */}
      {loading ? (
        <div style={{ textAlign: 'center', padding: '5rem 1rem', color: 'var(--text-muted)' }}>
          <RefreshCw size={36} className="animate-spin" style={{ color: 'var(--accent)', marginBottom: '14px' }} />
          <div style={{ fontSize: '1rem', fontWeight: 600 }}>Loading published produce from database...</div>
        </div>
      ) : filteredListings.length === 0 ? (
        <div className="glass-card" style={{ textAlign: 'center', padding: '5rem 2rem' }}>
          <ShoppingBag size={48} style={{ color: 'var(--text-faint)', marginBottom: '1rem' }} />
          <h3 style={{ fontSize: '1.3rem', fontWeight: 700, color: 'var(--text)' }}>
            No produce matches your filters
          </h3>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginTop: '6px', maxWidth: '420px', margin: '6px auto 18px' }}>
            Try expanding your search query, selecting "All Categories", or clearing the grade filter.
          </p>
          <button
            className="btn btn-secondary"
            onClick={() => {
              setSearchQuery('');
              setSelectedCategory('All');
              setSelectedRegion('');
              setSelectedGrade('');
            }}
          >
            Reset All Filters
          </button>
        </div>
      ) : (
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(310px, 1fr))',
          gap: '24px'
        }}>
          {filteredListings.map(listing => (
            <ProductCard3D
              key={listing.id}
              listing={listing}
              onSelect={setSelectedListing}
            />
          ))}
        </div>
      )}

      {/* Interactive Single Product Modal */}
      {selectedListing && (
        <ProductDetailModal
          listing={selectedListing}
          onClose={() => setSelectedListing(null)}
        />
      )}
    </div>
  );
};
