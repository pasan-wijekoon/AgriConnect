import React from 'react';
import { useAuth } from '../context/AuthContext';
import { Sprout, ShoppingBag, ShieldCheck, LogOut, TrendingUp, Layers } from './Icons';

interface NavbarProps {
  currentTab?: string;
  onTabChange?: (tab: string) => void;
  currentView?: 'dashboard' | 'today-prices';
  onNavigate?: (view: 'dashboard' | 'today-prices') => void;
}

const roleLabel: Record<string, { icon: React.FC<{ size?: number }>; label: string }> = {
  Farmer: { icon: Sprout, label: 'Farmer' },
  Buyer: { icon: ShoppingBag, label: 'Buyer' },
  Admin: { icon: ShieldCheck, label: 'Officer' },
};

export const Navbar: React.FC<NavbarProps> = ({
  currentView = 'dashboard',
  onNavigate
}) => {
  const { user, logout } = useAuth();
  const role = user ? roleLabel[user.role] : undefined;

  return (
    <header style={{
      position: 'sticky',
      top: 0,
      zIndex: 40,
      background: 'var(--bg-raised)',
      borderBottom: '1px solid var(--border)',
      padding: '0.75rem 2rem',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      gap: '1rem',
    }}>
      {/* Brand */}
      <div
        onClick={() => onNavigate?.('dashboard')}
        style={{ display: 'flex', alignItems: 'center', gap: '12px', cursor: 'pointer' }}
      >
        <div style={{
          width: '36px',
          height: '36px',
          borderRadius: 'var(--radius-md)',
          background: 'var(--accent)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}>
          <Sprout size={20} style={{ color: '#06120c' }} />
        </div>
        <div>
          <span style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--text)' }}>
            AgriConnect
          </span>
          <div style={{ fontSize: '0.72rem', color: 'var(--text-faint)' }}>
            Produce listings & fair-price discovery
          </div>
        </div>
      </div>

      {/* Navigation */}
      {user && onNavigate && (
        <nav style={{
          display: 'flex',
          alignItems: 'center',
          gap: '4px',
          background: 'var(--bg-input)',
          padding: '4px',
          borderRadius: 'var(--radius-md)',
          border: '1px solid var(--border)'
        }}>
          <button
            onClick={() => onNavigate('dashboard')}
            style={{
              padding: '6px 14px',
              borderRadius: 'var(--radius-sm)',
              fontSize: '0.82rem',
              fontWeight: 500,
              border: 'none',
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              gap: '6px',
              background: currentView === 'dashboard' ? 'var(--bg-hover)' : 'transparent',
              color: currentView === 'dashboard' ? 'var(--text)' : 'var(--text-muted)'
            }}
          >
            <Layers size={14} />
            <span>{user.role === 'Admin' ? 'Officer Queue' : 'Marketplace'}</span>
          </button>

          <button
            onClick={() => onNavigate('today-prices')}
            style={{
              padding: '6px 14px',
              borderRadius: 'var(--radius-sm)',
              fontSize: '0.82rem',
              fontWeight: 500,
              border: 'none',
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              gap: '6px',
              background: currentView === 'today-prices' ? 'var(--bg-hover)' : 'transparent',
              color: currentView === 'today-prices' ? 'var(--text)' : 'var(--text-muted)',
            }}
          >
            <TrendingUp size={14} />
            <span>Today's Prices</span>
          </button>
        </nav>
      )}

      {/* User */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
        {user && (
          <div style={{
            display: 'flex',
            alignItems: 'center',
            gap: '10px',
            padding: '6px 12px',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--border)'
          }}>
            <div style={{
              width: '28px',
              height: '28px',
              borderRadius: '50%',
              background: 'var(--bg-hover)',
              border: '1px solid var(--border-strong)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '0.8rem',
              fontWeight: 600,
              color: 'var(--text)'
            }}>
              {user.fullName.charAt(0)}
            </div>
            <div>
              <div style={{ fontSize: '0.85rem', fontWeight: 500, color: 'var(--text)' }}>
                {user.fullName}
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '5px', fontSize: '0.72rem', color: 'var(--text-faint)' }}>
                {role && <role.icon size={11} />}
                <span>{role?.label}</span>
                {user.region && <span>• {user.region}</span>}
              </div>
            </div>

            <button
              onClick={logout}
              title="Sign out"
              className="btn btn-ghost"
              style={{ padding: '5px', marginLeft: '4px' }}
            >
              <LogOut size={15} />
            </button>
          </div>
        )}
      </div>
    </header>
  );
};
