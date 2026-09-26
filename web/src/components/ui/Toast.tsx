import React from 'react';
import { CheckCircle2, AlertTriangle, AlertCircle, Info, X } from 'lucide-react';

export interface ToastProps {
  type?: 'success' | 'warning' | 'error' | 'info';
  title?: string;
  message: string;
  onClose?: () => void;
}

export const Toast: React.FC<ToastProps> = ({
  type = 'info',
  title,
  message,
  onClose
}) => {
  let bg = 'var(--status-info-bg)';
  let color = 'var(--status-info-text)';
  let border = 'var(--status-info-border)';
  let icon = <Info size={18} />;

  if (type === 'success') {
    bg = 'var(--status-success-bg)';
    color = 'var(--status-success-text)';
    border = 'var(--status-success-border)';
    icon = <CheckCircle2 size={18} />;
  } else if (type === 'warning') {
    bg = 'var(--status-warning-bg)';
    color = 'var(--status-warning-text)';
    border = 'var(--status-warning-border)';
    icon = <AlertTriangle size={18} />;
  } else if (type === 'error') {
    bg = 'var(--status-error-bg)';
    color = 'var(--status-error-text)';
    border = 'var(--status-error-border)';
    icon = <AlertCircle size={18} />;
  }

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'flex-start',
        gap: '12px',
        backgroundColor: bg,
        color: color,
        border: `1px solid ${border}`,
        borderRadius: 'var(--radius-sm)',
        padding: '12px 16px',
        boxShadow: 'var(--shadow-subtle)',
        fontSize: '14px',
        lineHeight: 1.4
      }}
    >
      <div style={{ flexShrink: 0, marginTop: '2px' }}>{icon}</div>
      <div style={{ flex: 1 }}>
        {title && <div style={{ fontWeight: 600, marginBottom: '2px' }}>{title}</div>}
        <div>{message}</div>
      </div>
      {onClose && (
        <button
          onClick={onClose}
          style={{
            background: 'none',
            border: 'none',
            color: 'inherit',
            cursor: 'pointer',
            padding: 0,
            opacity: 0.8
          }}
        >
          <X size={16} />
        </button>
      )}
    </div>
  );
};
