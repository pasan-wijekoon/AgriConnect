import type { ReactNode } from 'react'
import './PageHeader.css'

interface PageHeaderProps {
  title: string
  description?: string
  action?: ReactNode
}

/** Design.md §13 — every major page starts with Title + description + primary action. */
export function PageHeader({ title, description, action }: PageHeaderProps) {
  return (
    <div className="page-header">
      <div>
        <h1 className="page-header-title">{title}</h1>
        {description && <p className="page-header-description">{description}</p>}
      </div>
      {action && <div className="page-header-action">{action}</div>}
    </div>
  )
}
