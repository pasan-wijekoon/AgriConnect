import React from 'react';
import {
  ClipboardCheck,
  AlertTriangle,
  History,
  FileCheck2,
  ShieldCheck
} from 'lucide-react';

export type NavTab = 'queue' | 'discrepancies' | 'history' | 'publish-gate' | 'new-inspection';

export interface SidebarProps {
  activeTab: NavTab;
  onTabChange: (tab: NavTab) => void;
  pendingCount?: number;
  discrepancyCount?: number;
}

export const Sidebar: React.FC<SidebarProps> = ({
  activeTab,
  onTabChange,
  pendingCount = 0,
  discrepancyCount = 0
}) => {
  const navItems = [
    {
      id: 'queue' as NavTab,
      label: 'Inspection Queue',
      icon: <ClipboardCheck size={18} />,
      badge: pendingCount > 0 ? pendingCount : undefined,
      badgeColor: 'var(--status-pending-bg)',
      badgeTextColor: 'var(--status-pending-text)'
    },
    {
      id: 'discrepancies' as NavTab,
      label: 'Grade Discrepancies',
      icon: <AlertTriangle size={18} />,
      badge: discrepancyCount > 0 ? discrepancyCount : undefined,
      badgeColor: 'var(--status-warning-bg)',
      badgeTextColor: 'var(--status-warning-text)'
    },
    {
      id: 'publish-gate' as NavTab,
      label: 'Publication Gate (FR5)',
      icon: <FileCheck2 size={18} />
    },
    {
      id: 'history' as NavTab,
      label: 'Inspection History & Audit',
      icon: <History size={18} />
    }
  ];

  return (
    <aside
      style={{
        width: '260px',
        backgroundColor: '#FFFFFF',
        borderRight: '1px solid var(--border-color)',
        display: 'flex',
        flexDirection: 'column',
        minHeight: 'calc(100vh - 64px)'
      }}
    >
      <div style={{ padding: '20px 16px 8px 16px' }}>
        <p
          style={{
            fontSize: '11px',
            fontWeight: 700,
            textTransform: 'uppercase',
            letterSpacing: '0.06em',
            color: 'var(--text-muted)',
            marginBottom: '8px',
            paddingLeft: '8px'
          }}
        >
          
        </p>
      </div>

      <nav style={{ padding: '0 8px', display: 'flex', flexDirection: 'column', gap: '4px', flex: 1 }}>
        {navItems.map((item) => {
          const isActive = activeTab === item.id;
          return (
            <button
              key={item.id}
              onClick={() => onTabChange(item.id)}
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '10px 12px',
                borderRadius: 'var(--radius-sm)',
                border: 'none',
                backgroundColor: isActive ? 'var(--primary-green-light)' : 'transparent',
                color: isActive ? 'var(--primary-green)' : 'var(--text-primary)',
                fontWeight: isActive ? 600 : 500,
                fontSize: '14px',
                cursor: 'pointer',
                textAlign: 'left',
                transition: 'all 0.15s ease'
              }}
              onMouseEnter={(e) => {
                if (!isActive) e.currentTarget.style.backgroundColor = '#F9FAFB';
              }}
              onMouseLeave={(e) => {
                if (!isActive) e.currentTarget.style.backgroundColor = 'transparent';
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                <span style={{ color: isActive ? 'var(--primary-green)' : 'var(--text-secondary)' }}>
                  {item.icon}
                </span>
                <span>{item.label}</span>
              </div>

              {item.badge !== undefined && (
                <span
                  style={{
                    backgroundColor: item.badgeColor,
                    color: item.badgeTextColor,
                    fontSize: '11px',
                    fontWeight: 700,
                    padding: '2px 8px',
                    borderRadius: 'var(--radius-pill)'
                  }}
                >
                  {item.badge}
                </span>
              )}
            </button>
          );
        })}
      </nav>

      <div
        style={{
          padding: '16px',
          borderTop: '1px solid var(--border-color)',
          backgroundColor: '#F9FAFB'
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '4px' }}>
          <ShieldCheck size={16} color="var(--primary-green)" />
          <span style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-primary)' }}>
            Compliance Gate Active
          </span>
        </div>
        <p style={{ fontSize: '11px', color: 'var(--text-secondary)', lineHeight: 1.4, margin: 0 }}>
          All inspections must pass the compliance gate before publication.
        </p>
      </div>
    </aside>
  );
};
