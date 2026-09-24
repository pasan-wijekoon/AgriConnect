import './Badge.css'

export type BadgeTone = 'success' | 'pending' | 'warning' | 'error' | 'info' | 'proposed' | 'neutral'

interface BadgeProps {
  tone: BadgeTone
  children: string
}

/** Design.md §20 — one shared badge component; never a per-page reimplementation. */
export function Badge({ tone, children }: BadgeProps) {
  return <span className={`badge badge-${tone}`}>{children}</span>
}
