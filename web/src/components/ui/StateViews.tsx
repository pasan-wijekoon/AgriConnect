import type { ReactNode } from 'react'
import { Button } from './Button'
import './StateViews.css'

/** Design.md §25 — never a blank screen while loading. */
export function LoadingState({ label = 'Loading…' }: { label?: string }) {
  return (
    <div className="state-view" role="status" aria-live="polite">
      <div className="spinner" aria-hidden="true" />
      <p>{label}</p>
    </div>
  )
}

/** Design.md §26 — every list page needs an intentional empty state. */
export function EmptyState({
  title,
  description,
  action,
}: {
  title: string
  description?: string
  action?: ReactNode
}) {
  return (
    <div className="state-view">
      <h3>{title}</h3>
      {description && <p className="state-view-description">{description}</p>}
      {action}
    </div>
  )
}

/** Design.md §27 — understandable errors, never raw exception text. */
export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="state-view state-view-error">
      <h3>Something went wrong</h3>
      <p className="state-view-description">{message}</p>
      {onRetry && (
        <Button variant="secondary" onClick={onRetry}>
          Try Again
        </Button>
      )}
    </div>
  )
}
