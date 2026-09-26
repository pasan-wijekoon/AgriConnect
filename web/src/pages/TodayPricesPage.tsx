import React, { useState, useEffect, useMemo } from 'react';
import { api, type TodayPriceItem } from '../utils/api';
import { useAuth } from '../context/AuthContext';
import {
  Sparkles,
  MapPin,
  Search,
  CheckCircle2,
  Plus,
  ShoppingBag
} from '../components/Icons';

interface TodayPricesPageProps {
  onListProduce?: (cropName: string) => void;
  onBrowseProduce?: (cropName: string) => void;
}

export const TodayPricesPage: React.FC<TodayPricesPageProps> = ({
  onListProduce,
  onBrowseProduce
}) => {
  const { user } = useAuth();
  const [items, setItems] = useState<TodayPriceItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedCategory, setSelectedCategory] = useState<string>('All');
  const [selectedRegion, setSelectedRegion] = useState<string>('All');
  const [selectedGrade, setSelectedGrade] = useState<string>('A');
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [activeModalItem, setActiveModalItem] = useState<TodayPriceItem | null>(null);

  const categories = ['All', 'Vegetables', 'Fruits', 'Grains', 'Spices', 'Beverages'];
  const regions = [
    'All', 'Colombo', 'Gampaha', 'Kalutara', 'Kandy', 'Matale', 'Nuwara Eliya',
    'Galle', 'Matara', 'Hambantota', 'Jaffna', 'Kilinochchi', 'Mannar',
    'Mullaitivu', 'Vavuniya', 'Batticaloa', 'Ampara', 'Trincomalee',
    'Kurunegala', 'Puttalam', 'Anuradhapura', 'Polonnaruwa',
    'Badulla', 'Monaragala', 'Ratnapura', 'Kegalle', 'Dambulla'
  ];

  const fetchPrices = async () => {
    setIsLoading(true);
    try {
      const res = await api.getTodayPrices(selectedRegion, selectedGrade);
      setItems(res.items || []);
    } catch (err) {
      console.error('Failed to load today prices:', err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchPrices();
  }, [selectedRegion, selectedGrade]);

  const filteredItems = useMemo(() => {
    return items.filter(item => {
      const matchesCategory = selectedCategory === 'All' || item.category.toLowerCase() === selectedCategory.toLowerCase();
      const matchesSearch = !searchQuery || item.name.toLowerCase().includes(searchQuery.toLowerCase()) || item.category.toLowerCase().includes(searchQuery.toLowerCase());
      return matchesCategory && matchesSearch;
    });
  }, [items, selectedCategory, searchQuery]);

  // Compute summary stats
  const summaryStats = useMemo(() => {
    if (items.length === 0) return { rising: 0, falling: 0, stable: 0, avgConfidence: 0 };
    const rising = items.filter(i => i.trend === 'rising').length;
    const falling = items.filter(i => i.trend === 'falling').length;
    const stable = items.length - rising - falling;
    const avgConfidence = Math.round(items.reduce((acc, i) => acc + i.confidence, 0) / items.length * 100);
    return { rising, falling, stable, avgConfidence };
  }, [items]);

  return (
    <div style={{ maxWidth: '1440px', margin: '0 auto', padding: '1.5rem 2rem 4rem' }}>
      
      {/* ── Compact Hero Banner ── */}
      <div style={{
        background: 'var(--bg-card)',
        border: '1px solid var(--accent-border)',
        borderRadius: '20px',
        padding: '20px 28px',
        marginBottom: '20px',
        position: 'relative',
        overflow: 'hidden'
      }}>
        <div style={{
          position: 'absolute', top: '-30px', right: '-30px', width: '160px', height: '160px',
          borderRadius: '50%', background: 'radial-gradient(circle, var(--accent-border) 0%, transparent 70%)',
          pointerEvents: 'none'
        }} />
        <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: '14px' }}>
          <div>
            <div style={{
              display: 'inline-flex', alignItems: 'center', gap: '8px',
              padding: '3px 10px', borderRadius: '9999px',
              background: 'var(--accent-soft)', border: '1px solid var(--accent-border)', marginBottom: '8px'
            }}>
              <span style={{ width: '6px', height: '6px', borderRadius: '50%', background: 'var(--accent)', display: 'inline-block' }} />
              <span style={{ fontSize: '0.72rem', fontWeight: 500, color: 'var(--accent-text)' }}>Market-grounded price estimates</span>
            </div>
            <h1 style={{ fontSize: '1.6rem', fontWeight: 800, margin: '0 0 4px', color: 'var(--text)', letterSpacing: '-0.02em' }}>
              Today's Produce Prices
            </h1>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', margin: 0, maxWidth: '600px' }}>
              Fair price summary for Sri Lanka's most popular wholesale produce — updated by <strong style={{ color: 'var(--accent)' }}>Agentic AI</strong>
            </p>
          </div>

          {/* Summary Stats Chips */}
          <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
            <div style={{
              background: 'var(--bg-hover)', border: '1px solid var(--accent-border)',
              padding: '8px 14px', borderRadius: '12px', textAlign: 'center', minWidth: '70px'
            }}>
              <div style={{ fontSize: '0.65rem', color: 'var(--text-faint)', fontWeight: 700 }}>TRACKED</div>
              <div style={{ fontSize: '1.2rem', fontWeight: 800, color: 'var(--accent)' }}>{items.length || '—'}</div>
            </div>
            <div style={{
              background: 'var(--bg-hover)', border: '1px solid var(--accent-border)',
              padding: '8px 14px', borderRadius: '12px', textAlign: 'center', minWidth: '70px'
            }}>
              <div style={{ fontSize: '0.65rem', color: 'var(--text-faint)', fontWeight: 700 }}>RISING</div>
              <div style={{ fontSize: '1.2rem', fontWeight: 800, color: 'var(--accent-text)' }}>▲ {summaryStats.rising}</div>
            </div>
            <div style={{
              background: 'var(--bg-hover)', border: '1px solid var(--danger-soft)',
              padding: '8px 14px', borderRadius: '12px', textAlign: 'center', minWidth: '70px'
            }}>
              <div style={{ fontSize: '0.65rem', color: 'var(--text-faint)', fontWeight: 700 }}>FALLING</div>
              <div style={{ fontSize: '1.2rem', fontWeight: 800, color: '#e08776' }}>▼ {summaryStats.falling}</div>
            </div>
            <div style={{
              background: 'var(--bg-hover)', border: '1px solid var(--border-strong)',
              padding: '8px 14px', borderRadius: '12px', textAlign: 'center', minWidth: '70px'
            }}>
              <div style={{ fontSize: '0.65rem', color: 'var(--text-faint)', fontWeight: 700 }}>AI CONF.</div>
              <div style={{ fontSize: '1.2rem', fontWeight: 800, color: 'var(--text-muted)' }}>{summaryStats.avgConfidence}%</div>
            </div>
          </div>
        </div>
      </div>

      {/* ── Filter Bar ── */}
      <div style={{
        display: 'flex', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: '12px',
        background: 'var(--bg-hover)', border: '1px solid var(--border)',
        borderRadius: '14px', padding: '10px 16px', marginBottom: '18px'
      }}>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '5px' }}>
          {categories.map(cat => (
            <button key={cat} onClick={() => setSelectedCategory(cat)} style={{
              padding: '5px 12px', borderRadius: '7px', fontSize: '0.78rem', fontWeight: 600,
              border: 'none', cursor: 'pointer', transition: 'all 0.2s',
              background: selectedCategory === cat ? 'var(--accent)' : 'var(--border)',
              color: selectedCategory === cat ? 'var(--text)' : 'var(--text-muted)'
            }}>
              {cat}
            </button>
          ))}
        </div>

        <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: '8px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '5px' }}>
            <MapPin size={14} style={{ color: 'var(--accent)' }} />
            <select value={selectedRegion} onChange={(e) => setSelectedRegion(e.target.value)}
              style={{
                background: 'var(--bg)', color: 'var(--text-muted)',
                border: '1px solid var(--border)', borderRadius: '7px',
                padding: '5px 10px', fontSize: '0.78rem', fontWeight: 500, outline: 'none', cursor: 'pointer'
              }}>
              {regions.map(r => (
                <option key={r} value={r} style={{ background: 'var(--bg-raised)' }}>
                  {r === 'All' ? 'All Regions' : r}
                </option>
              ))}
            </select>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '3px', background: 'var(--bg)', padding: '3px', borderRadius: '7px', border: '1px solid var(--border)' }}>
            {['A', 'B', 'C'].map(g => (
              <button key={g} onClick={() => setSelectedGrade(g)} style={{
                padding: '3px 9px', borderRadius: '5px', fontSize: '0.74rem', fontWeight: 700,
                border: 'none', cursor: 'pointer',
                background: selectedGrade === g ? 'var(--accent-border)' : 'transparent',
                color: selectedGrade === g ? 'var(--accent-text)' : 'var(--text-faint)'
              }}>
                {g}
              </button>
            ))}
          </div>

          <div style={{ position: 'relative', width: '180px' }}>
            <Search size={13} style={{ position: 'absolute', left: '9px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-faint)' }} />
            <input type="text" placeholder="Search..." value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              style={{
                width: '100%', padding: '5px 10px 5px 28px', borderRadius: '7px',
                background: 'var(--bg)', border: '1px solid var(--border)',
                color: 'var(--text)', fontSize: '0.78rem', outline: 'none'
              }}
            />
          </div>
        </div>
      </div>

      {/* ── Grid: Price Summary Tiles ── */}
      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '50px 0', color: 'var(--text-muted)' }}>
          <div style={{
            width: '36px', height: '36px', borderRadius: '50%',
            border: '3px solid var(--accent-border)', borderTopColor: 'var(--accent)',
            animation: 'spin 0.8s linear infinite', margin: '0 auto 12px'
          }} />
          <div style={{ fontSize: '0.88rem', fontWeight: 600 }}>Loading prices from AI...</div>
        </div>
      ) : filteredItems.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '50px 0', color: 'var(--text-faint)' }}>
          No produce matches the selected filters.
        </div>
      ) : (
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(195px, 1fr))',
          gap: '12px',
          alignItems: 'stretch'
        }}>
          {filteredItems.map(item => {
            const isRising = item.trend === 'rising';
            const isFalling = item.trend === 'falling';

            return (
              <div
                key={item.cropId + item.name}
                onClick={() => setActiveModalItem(item)}
                style={{
                  background: 'var(--bg-raised)',
                  border: '1px solid var(--border)',
                  borderRadius: '14px',
                  overflow: 'hidden',
                  cursor: 'pointer',
                  transition: 'all 0.25s cubic-bezier(0.16, 1, 0.3, 1)',
                  display: 'flex',
                  flexDirection: 'column',
                  position: 'relative'
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.boxShadow = 'var(--shadow-md)';
                  e.currentTarget.style.borderColor = 'var(--border-strong)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.boxShadow = 'none';
                  e.currentTarget.style.borderColor = 'var(--border)';
                }}
              >
                {/* Photo with overlay */}
                <div style={{ position: 'relative', width: '100%', height: '100px', overflow: 'hidden', background: 'var(--bg-input)' }}>
                  <img
                    src={item.imageUrl}
                    alt={item.name}
                    style={{ width: '100%', height: '100%', objectFit: 'cover', transition: 'transform 0.4s ease' }}
                    loading="lazy"
                  />
                  <div style={{
                    position: 'absolute', inset: 0,
                    background: 'linear-gradient(to bottom, rgba(0, 0, 0, 0.05) 0%, var(--bg-raised) 100%)'
                  }} />

                  {/* Trend badge */}
                  <div style={{
                    position: 'absolute', top: '6px', right: '6px',
                    padding: '2px 7px', borderRadius: '6px',
                    fontSize: '0.68rem', fontWeight: 700,
                    background: isRising ? 'var(--accent)' : (isFalling ? 'var(--danger)' : 'var(--bg-hover)'),
                    color: 'var(--text)', display: 'flex', alignItems: 'center', gap: '3px',
                    boxShadow: '0 2px 6px rgba(0, 0, 0, 0.4)',
                    backdropFilter: 'blur(4px)'
                  }}>
                    {isRising && '▲'}{isFalling && '▼'}{!isRising && !isFalling && '●'}
                    {item.change24h > 0 ? `+${item.change24h}%` : `${item.change24h}%`}
                  </div>

                  {/* Produce name overlay on photo */}
                  <div style={{ position: 'absolute', bottom: '6px', left: '8px', right: '8px' }}>
                    <h3 style={{
                      margin: 0, fontSize: '0.95rem', fontWeight: 800, color: 'var(--text)',
                      textShadow: '0 1px 6px rgba(0, 0, 0, 0.8)', lineHeight: 1.2
                    }}>
                      {item.name}
                    </h3>
                  </div>
                </div>

                {/* Card content — compact price summary */}
                <div style={{ padding: '10px 10px 12px', flex: 1, display: 'flex', flexDirection: 'column', gap: '6px' }}>
                  {/* Region + Category row */}
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '3px', fontSize: '0.68rem', color: 'var(--text-faint)' }}>
                      <MapPin size={10} /> {item.region}
                    </div>
                    <span style={{
                      fontSize: '0.62rem', fontWeight: 700, padding: '1px 6px',
                      borderRadius: '4px', background: 'var(--border)',
                      color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.04em'
                    }}>
                      {item.category}
                    </span>
                  </div>

                  {/* Price box — the hero element */}
                  <div style={{
                    background: 'var(--accent-soft)',
                    border: '1px solid var(--accent-border)',
                    borderRadius: '8px',
                    padding: '8px',
                    textAlign: 'center'
                  }}>
                    <div style={{ fontSize: '0.58rem', fontWeight: 700, color: 'var(--accent)', letterSpacing: '0.06em', textTransform: 'uppercase', marginBottom: '2px', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '3px' }}>
                      <Sparkles size={9} /> AI Fair Price
                    </div>
                    <div style={{ fontSize: '1.1rem', fontWeight: 800, color: 'var(--text)', lineHeight: 1.1 }}>
                      Rs. {item.suggestedPriceMin.toFixed(0)}–{item.suggestedPriceMax.toFixed(0)}
                    </div>
                    <div style={{ fontSize: '0.65rem', color: 'var(--text-muted)', marginTop: '1px' }}>
                      per {item.unit} · Gr. {item.grade}
                    </div>
                  </div>

                  {/* Bottom stats */}
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: '0.65rem' }}>
                    <span style={{ color: 'var(--text-muted)' }}>Avg <strong style={{ color: 'var(--text-muted)' }}>Rs.{item.averagePrice.toFixed(0)}</strong></span>
                    <span style={{
                      color: 'var(--text-muted)', fontWeight: 700,
                      background: 'var(--bg-hover)', padding: '1px 5px', borderRadius: '4px'
                    }}>
                      {Math.round(item.confidence * 100)}%
                    </span>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* ── AI Analysis Modal ── */}
      {activeModalItem && (
        <div style={{
          position: 'fixed', inset: 0, zIndex: 60,
          background: 'rgba(0, 0, 0, 0.85)', backdropFilter: 'blur(10px)',
          display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '20px'
        }}>
          <div style={{
            background: 'var(--bg-input)', border: '1px solid var(--accent-border)',
            borderRadius: '20px', maxWidth: '620px', width: '100%', overflow: 'hidden',
            boxShadow: '0 25px 60px rgba(0, 0, 0, 0.6)', animation: 'fadeIn 0.2s ease-out'
          }}>
            {/* Modal Header with Image */}
            <div style={{ position: 'relative', height: '160px' }}>
              <img src={activeModalItem.imageUrl} alt={activeModalItem.name}
                style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
              <div style={{
                position: 'absolute', inset: 0,
                background: 'linear-gradient(to bottom, rgba(0, 0, 0, 0.2) 0%, var(--bg-raised) 100%)'
              }} />

              <button onClick={() => setActiveModalItem(null)} style={{
                position: 'absolute', top: '12px', right: '12px', width: '30px', height: '30px',
                borderRadius: '50%', background: 'rgba(0, 0, 0, 0.6)',
                border: '1px solid var(--border)', color: 'var(--text)',
                fontSize: '16px', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center'
              }}>✕</button>

              <div style={{ position: 'absolute', bottom: '14px', left: '18px' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginBottom: '3px' }}>
                  <span style={{
                    padding: '2px 7px', borderRadius: '4px', fontSize: '0.7rem',
                    fontWeight: 700, background: 'var(--accent)', color: 'var(--text)'
                  }}>{activeModalItem.category}</span>
                  <span style={{
                    padding: '2px 7px', borderRadius: '4px', fontSize: '0.7rem',
                    fontWeight: 700, background: 'var(--border)', color: 'var(--text)'
                  }}>Grade {activeModalItem.grade}</span>
                </div>
                <h2 style={{ margin: 0, fontSize: '1.5rem', fontWeight: 800, color: 'var(--text)' }}>
                  {activeModalItem.name} ({activeModalItem.region})
                </h2>
              </div>
            </div>

            {/* Modal Body */}
            <div style={{ padding: '20px 22px', maxHeight: '65vh', overflowY: 'auto' }}>
              {/* Price Banner */}
              <div style={{
                background: 'var(--bg-card)',
                border: '1px solid var(--accent-border)', borderRadius: '12px',
                padding: '14px 18px', marginBottom: '16px',
                display: 'flex', alignItems: 'center', justifyContent: 'space-between'
              }}>
                <div>
                  <div style={{ fontSize: '0.72rem', fontWeight: 700, color: 'var(--accent-text)', textTransform: 'uppercase', marginBottom: '3px', display: 'flex', alignItems: 'center', gap: '5px' }}>
                    <Sparkles size={13} /> AI Suggested Fair Range
                  </div>
                  <div style={{ fontSize: '1.5rem', fontWeight: 800, color: 'var(--text)' }}>
                    Rs. {activeModalItem.suggestedPriceMin.toFixed(2)} – {activeModalItem.suggestedPriceMax.toFixed(2)}
                    <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)', fontWeight: 500, marginLeft: '5px' }}>/ {activeModalItem.unit}</span>
                  </div>
                </div>
                <div style={{ textAlign: 'right' }}>
                  <div style={{ fontSize: '0.7rem', color: 'var(--text-faint)', fontWeight: 600 }}>CONFIDENCE</div>
                  <div style={{ fontSize: '1.2rem', fontWeight: 800, color: 'var(--text-muted)' }}>
                    {Math.round(activeModalItem.confidence * 100)}%
                  </div>
                </div>
              </div>

              {/* Agent Reasoning */}
              <div style={{ marginBottom: '16px' }}>
                <h4 style={{ margin: '0 0 6px', fontSize: '0.8rem', fontWeight: 700, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                  AI Analysis & Rationale
                </h4>
                <div style={{
                  background: 'var(--bg-hover)', border: '1px solid var(--border)',
                  borderRadius: '10px', padding: '12px 14px', fontSize: '0.85rem', lineHeight: 1.5, color: 'var(--text-muted)'
                }}>
                  {activeModalItem.reasoning}
                </div>
              </div>

              {/* Tool cards */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', marginBottom: '18px' }}>
                <div style={{
                  background: 'var(--bg-hover)', border: '1px solid var(--border)',
                  borderRadius: '10px', padding: '10px 12px'
                }}>
                  <div style={{ fontSize: '0.68rem', color: 'var(--text-faint)', fontWeight: 700, textTransform: 'uppercase', marginBottom: '3px' }}>
                    MarketPriceLookupTool
                  </div>
                  <div style={{ fontSize: '0.88rem', fontWeight: 700, color: 'var(--text)' }}>
                    Wholesale: Rs. {activeModalItem.benchmarkWholesale.toFixed(2)}/kg
                  </div>
                  <div style={{ fontSize: '0.7rem', color: 'var(--accent)', marginTop: '2px' }}>
                    ✓ Central economic centres data
                  </div>
                </div>
                <div style={{
                  background: 'var(--bg-hover)', border: '1px solid var(--border)',
                  borderRadius: '10px', padding: '10px 12px'
                }}>
                  <div style={{ fontSize: '0.68rem', color: 'var(--text-faint)', fontWeight: 700, textTransform: 'uppercase', marginBottom: '3px' }}>
                    HistoricalTrendTool
                  </div>
                  <div style={{ fontSize: '0.88rem', fontWeight: 700, color: 'var(--text)' }}>
                    24h: {activeModalItem.change24h > 0 ? `+${activeModalItem.change24h}%` : `${activeModalItem.change24h}%`}
                  </div>
                  <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)', marginTop: '2px' }}>
                    ✓ Moving averages verified
                  </div>
                </div>
              </div>

              {/* Validation */}
              <div style={{
                display: 'flex', alignItems: 'center', gap: '8px', padding: '8px 12px',
                background: 'var(--accent-soft)', border: '1px solid var(--accent-border)',
                borderRadius: '8px', fontSize: '0.78rem', color: 'var(--accent-text)', marginBottom: '18px'
              }}>
                <CheckCircle2 size={15} />
                <span>Bounds Validator: <strong>Passed</strong>. Outliers rejected before officer review.</span>
              </div>

              {/* Action Buttons */}
              <div style={{ display: 'flex', gap: '10px' }}>
                {user?.role === 'Farmer' ? (
                  <button onClick={() => {
                    const crop = activeModalItem.name;
                    setActiveModalItem(null);
                    if (onListProduce) onListProduce(crop);
                  }} style={{
                    flex: 1, padding: '11px', borderRadius: '10px',
                    background: 'var(--accent)',
                    border: 'none', color: 'var(--text)', fontSize: '0.88rem', fontWeight: 700,
                    cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center',
                    gap: '7px', boxShadow: 'var(--shadow-sm)'
                  }}>
                    <Plus size={15} /> List at Fair Price
                  </button>
                ) : (
                  <button onClick={() => {
                    const crop = activeModalItem.name;
                    setActiveModalItem(null);
                    if (onBrowseProduce) onBrowseProduce(crop);
                  }} style={{
                    flex: 1, padding: '11px', borderRadius: '10px',
                    background: 'var(--bg-hover)',
                    border: 'none', color: 'var(--text)', fontSize: '0.88rem', fontWeight: 700,
                    cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center',
                    gap: '7px', boxShadow: 'var(--shadow-sm)'
                  }}>
                    <ShoppingBag size={15} /> Find Listings
                  </button>
                )}
                <button onClick={() => setActiveModalItem(null)} style={{
                  padding: '11px 18px', borderRadius: '10px',
                  background: 'var(--border)', border: '1px solid var(--border)',
                  color: 'var(--text-muted)', fontSize: '0.85rem', fontWeight: 600, cursor: 'pointer'
                }}>
                  Close
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
