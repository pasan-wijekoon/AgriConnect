import type { HTMLAttributes, ReactNode } from 'react'
import './Card.css'

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode
}

/** Design.md §14 — the primary content container. */
export function Card({ children, className = '', ...rest }: CardProps) {
  return (
    <div className={`card ${className}`.trim()} {...rest}>
      {children}
    </div>
  )
}
