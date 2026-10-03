import type { ReactNode } from 'react'

import type { AnomalyStatus } from '../utils/anomalies.ts'

export function PageHeader({ title, description, actions }: { title: string; description: string; actions?: ReactNode }) {
  return (
    <header className="page-header">
      <div>
        <h1>{title}</h1>
        <p className="muted">{description}</p>
      </div>
      {actions && <div className="page-actions">{actions}</div>}
    </header>
  )
}

export function Card({ title, actions, children, className = '', variant = 'default' }: { title?: string; actions?: ReactNode; children: ReactNode; className?: string; variant?: 'default' | 'glass' }) {
  return (
    <section className={`card ${variant === 'glass' ? 'card-glass' : ''} ${className}`}>
      {(title || actions) && (
        <div className="card-head">
          {title && <h2>{title}</h2>}
          {actions}
        </div>
      )}
      {children}
    </section>
  )
}

export function StatTile({ label, value, detail }: { label: string; value: ReactNode; detail?: ReactNode }) {
  return (
    <div className="stat">
      <div className="stat-label">{label}</div>
      <div className="stat-value">{value}</div>
      {detail && <div className="stat-detail">{detail}</div>}
    </div>
  )
}

export function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="field">
      <span>{label}</span>
      {children}
    </label>
  )
}

export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="empty" role="status">
      <strong>{title}</strong>
      {children && <div className="muted">{children}</div>}
    </div>
  )
}

export function ErrorNotice({ error, onRetry }: { error: Error; onRetry?: () => void }) {
  return (
    <div className="notice notice-error" role="alert">
      <Icon name="alert" />
      <span>{error.message}</span>
      {onRetry && (
        <button type="button" className="btn btn-quiet" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  )
}

export function SuccessNotice({ children, onDismiss }: { children: ReactNode; onDismiss?: () => void }) {
  return (
    <div className="notice notice-success" role="status">
      <Icon name="check" />
      <span>{children}</span>
      {onDismiss && (
        <button type="button" className="btn btn-quiet" onClick={onDismiss} aria-label="Dismiss message">
          ×
        </button>
      )}
    </div>
  )
}

const statusIcon: Record<AnomalyStatus, IconName> = { Open: 'alert', Reviewed: 'check', Dismissed: 'minus' }

/** Status is always icon + word, never colour alone. */
export function StatusBadge({ status }: { status: AnomalyStatus }) {
  return (
    <span className={`badge badge-${status.toLowerCase()}`}>
      <Icon name={statusIcon[status]} />
      {status}
    </span>
  )
}

export function Pill({ children }: { children: ReactNode }) {
  return <span className="pill">{children}</span>
}

type IconName = 'chart' | 'grid' | 'flag' | 'file' | 'alert' | 'check' | 'minus' | 'up' | 'down' | 'close' | 'refresh' | 'download' | 'spark'

const paths: Record<IconName, string> = {
  chart: 'M3 17l5-6 4 4 7-9M3 21h18',
  grid: 'M4 4h7v7H4zM13 4h7v7h-7zM4 13h7v7H4zM13 13h7v7h-7z',
  flag: 'M5 21V4h11l-1.5 4L16 12H5',
  file: 'M7 3h7l5 5v13H7zM14 3v5h5M10 13h6M10 17h6',
  alert: 'M12 3l9.5 17h-19zM12 10v4M12 17h.01',
  check: 'M5 12l4.5 4.5L19 7',
  minus: 'M6 12h12',
  up: 'M12 19V5M6 11l6-6 6 6',
  down: 'M12 5v14M6 13l6 6 6-6',
  close: 'M6 6l12 12M18 6L6 18',
  refresh: 'M20 11a8 8 0 10-2.3 5.7M20 5v6h-6',
  download: 'M12 4v11M7 10l5 5 5-5M5 20h14',
  spark: 'M12 3l1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8zM19 17l.7 2 2 .7-2 .7-.7 2-.7-2-2-.7 2-.7z',
}

export function Icon({ name, label }: { name: IconName; label?: string }) {
  return (
    <svg
      className="icon"
      viewBox="0 0 24 24"
      width="16"
      height="16"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      role={label ? 'img' : undefined}
      aria-label={label}
      aria-hidden={label ? undefined : true}
    >
      <path d={paths[name]} />
    </svg>
  )
}
