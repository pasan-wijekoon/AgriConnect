import React from 'react';
import { CheckCircle2, Clock, AlertTriangle, XCircle, Check } from 'lucide-react';

export interface BadgeProps {
  children: React.ReactNode;
  variant?: 'success' | 'pending' | 'warning' | 'error' | 'info' | 'neutral';
  icon?: React.ReactNode;
  size?: 'sm' | 'md';
}

export const Badge: React.FC<BadgeProps> = ({
  children,
  variant = 'neutral',
  icon,
  size = 'md'
}) => {
  const getStyles = (): React.CSSProperties => {
    let bg = '#F3F4F6';
    let color = 'var(--text-secondary)';
    let border = '1px solid #E5E7EB';

    if (variant === 'success') {
      bg = 'var(--status-success-bg)';
      color = 'var(--status-success-text)';
      border = '1px solid var(--status-success-border)';
    } else if (variant === 'pending') {
      bg = 'var(--status-pending-bg)';
      color = 'var(--status-pending-text)';
      border = '1px solid var(--status-pending-border)';
    } else if (variant === 'warning') {
      bg = 'var(--status-warning-bg)';
      color = 'var(--status-warning-text)';
      border = '1px solid var(--status-warning-border)';
    } else if (variant === 'error') {
      bg = 'var(--status-error-bg)';
      color = 'var(--status-error-text)';
      border = '1px solid var(--status-error-border)';
    } else if (variant === 'info') {
      bg = 'var(--status-info-bg)';
      color = 'var(--status-info-text)';
      border = '1px solid var(--status-info-border)';
    }

    return {
      display: 'inline-flex',
      alignItems: 'center',
      gap: '4px',
      backgroundColor: bg,
      color: color,
      border: border,
      borderRadius: 'var(--radius-pill)',
      padding: size === 'sm' ? '2px 8px' : '4px 10px',
      fontSize: size === 'sm' ? '11px' : '12px',
      fontWeight: 600,
      letterSpacing: '0.01em',
      lineHeight: 1.2
    };
  };

  return (
    <span style={getStyles()}>
      {icon}
      {children}
    </span>
  );
};

export const GradeBadge: React.FC<{ grade: string; size?: 'sm' | 'md' }> = ({ grade, size = 'md' }) => {
  const g = (grade || '').toLowerCase().trim();

  if (g.includes('grade a') || g === 'a') {
    return (
      <Badge variant="success" size={size} icon={<CheckCircle2 size={12} />}>
        Grade A (Export)
      </Badge>
    );
  }
  if (g.includes('grade b') || g === 'b') {
    return (
      <Badge variant="pending" size={size} icon={<Check size={12} />}>
        Grade B (Standard)
      </Badge>
    );
  }
  if (g.includes('grade c') || g === 'c') {
    return (
      <Badge variant="warning" size={size} icon={<AlertTriangle size={12} />}>
        Grade C (Processing)
      </Badge>
    );
  }
  if (g.includes('reject')) {
    return (
      <Badge variant="error" size={size} icon={<XCircle size={12} />}>
        Rejected (Unfit)
      </Badge>
    );
  }

  return <Badge variant="neutral" size={size}>{grade || 'Not Graded'}</Badge>;
};

export const StatusBadge: React.FC<{ status: string }> = ({ status }) => {
  const s = (status || '').toLowerCase().trim();

  if (s === 'published' || s === 'completed' || s === 'approved') {
    return <Badge variant="success" icon={<CheckCircle2 size={12} />}>{status}</Badge>;
  }
  if (s === 'pendingapproval' || s === 'pending') {
    return <Badge variant="pending" icon={<Clock size={12} />}>Pending Inspection</Badge>;
  }
  if (s === 'withdrawn' || s === 'cancelled' || s === 'rejected') {
    return <Badge variant="error" icon={<XCircle size={12} />}>{status}</Badge>;
  }
  if (s === 'draft') {
    return <Badge variant="neutral">{status}</Badge>;
  }

  return <Badge variant="info">{status}</Badge>;
};
