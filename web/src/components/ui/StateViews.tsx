import React from 'react';
import { Inbox, AlertTriangle, Loader2 } from 'lucide-react';
import { Button } from './Button';

export const LoadingState: React.FC<{ message?: string }> = ({
  message = 'Loading verification records...'
}) => {
  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '48px 24px',
        color: 'var(--text-secondary)'
      }}
    >
      <Loader2 size={36} className="animate-spin" color="var(--primary-green)" style={{ marginBottom: '16px' }} />
      <p style={{ fontSize: '14px', fontWeight: 500 }}>{message}</p>
    </div>
  );
};

export const EmptyState: React.FC<{
  title: string;
  description: string;
  actionLabel?: string;
  onAction?: () => void;
  icon?: React.ReactNode;
}> = ({
  title,
  description,
  actionLabel,
  onAction,
  icon = <Inbox size={48} color="var(--text-muted)" />
}) => {
  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '48px 24px',
        textAlign: 'center',
        backgroundColor: '#FFFFFF',
        borderRadius: 'var(--radius-md)',
        border: '1px dashed var(--border-color)'
      }}
    >
      <div style={{ marginBottom: '16px' }}>{icon}</div>
      <h4 style={{ fontSize: '16px', fontWeight: 600, color: 'var(--text-primary)', marginBottom: '6px' }}>
        {title}
      </h4>
      <p style={{ fontSize: '14px', color: 'var(--text-secondary)', maxWidth: '400px', marginBottom: actionLabel ? '20px' : 0 }}>
        {description}
      </p>
      {actionLabel && onAction && (
        <Button variant="outline" size="sm" onClick={onAction}>
          {actionLabel}
        </Button>
      )}
    </div>
  );
};

export const ErrorState: React.FC<{
  title?: string;
  message?: string;
  onRetry?: () => void;
}> = ({
  title = 'Unable to load data',
  message = 'We encountered an issue retrieving the inspection records. Please try again.',
  onRetry
}) => {
  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '40px 24px',
        textAlign: 'center',
        backgroundColor: 'var(--status-error-bg)',
        borderRadius: 'var(--radius-md)',
        border: '1px solid var(--status-error-border)'
      }}
    >
      <AlertTriangle size={36} color="var(--status-error-text)" style={{ marginBottom: '12px' }} />
      <h4 style={{ fontSize: '16px', fontWeight: 600, color: 'var(--status-error-text)', marginBottom: '4px' }}>
        {title}
      </h4>
      <p style={{ fontSize: '14px', color: 'var(--text-primary)', maxWidth: '420px', marginBottom: onRetry ? '16px' : 0 }}>
        {message}
      </p>
      {onRetry && (
        <Button variant="secondary" size="sm" onClick={onRetry}>
          Try Again
        </Button>
      )}
    </div>
  );
};
