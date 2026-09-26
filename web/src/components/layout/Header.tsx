import React from 'react';
import { Bell, UserCircle2, CheckCircle2, Sprout } from 'lucide-react';

export const Header: React.FC = () => {
  return (
    <header
      style={{
        height: '64px',
        backgroundColor: '#FFFFFF',
        borderBottom: '1px solid var(--border-color)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '0 24px',
        position: 'sticky',
        top: 0,
        zIndex: 100
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
        <div
          style={{
            width: '36px',
            height: '36px',
            borderRadius: '8px',
            backgroundColor: 'var(--primary-green)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: '#FFFFFF'
          }}
        >
          <Sprout size={22} />
        </div>
        <div>
          <span style={{ fontSize: '18px', fontWeight: 700, color: 'var(--primary-green)', letterSpacing: '-0.02em' }}>
            AgriConnect
          </span>
          <span
            style={{
              marginLeft: '8px',
              fontSize: '11px',
              fontWeight: 600,
              backgroundColor: 'var(--primary-green-light)',
              color: 'var(--primary-green)',
              padding: '2px 8px',
              borderRadius: 'var(--radius-pill)',
              textTransform: 'uppercase'
            }}
          >
            Officer Portal
          </span>
        </div>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: '20px' }}>
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            backgroundColor: '#F3F4F6',
            padding: '6px 12px',
            borderRadius: 'var(--radius-pill)',
            fontSize: '12px',
            color: 'var(--text-secondary)'
          }}
        >
          <CheckCircle2 size={14} color="var(--primary-green)" />
          <span>Colombo Central Collection Centre</span>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <div
            style={{
              width: '36px',
              height: '36px',
              borderRadius: '50%',
              backgroundColor: '#F3F4F6',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: 'var(--text-secondary)',
              cursor: 'pointer'
            }}
          >
            <Bell size={18} />
          </div>

          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '10px',
              padding: '4px 8px',
              borderRadius: 'var(--radius-sm)',
              cursor: 'pointer'
            }}
          >
            <UserCircle2 size={28} color="var(--primary-green)" />
            <div style={{ display: 'flex', flexDirection: 'column' }}>
              <span style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-primary)' }}>
                Kamal Gunawardena
              </span>
              <span style={{ fontSize: '11px', color: 'var(--text-secondary)' }}>
                Quality & Inspection Officer
              </span>
            </div>
          </div>
        </div>
      </div>
    </header>
  );
};
