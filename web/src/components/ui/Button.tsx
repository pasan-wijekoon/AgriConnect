import type { ButtonHTMLAttributes } from 'react'
import './Button.css'

type Variant = 'primary' | 'secondary' | 'destructive'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  loading?: boolean
}

/** Design.md §15/§16 — three button levels, loading/disabled states. */
export function Button({ variant = 'primary', loading = false, disabled, children, ...rest }: ButtonProps) {
  return (
    <button
      type="button"
      className={`btn btn-${variant}`}
      disabled={disabled || loading}
      aria-busy={loading}
      {...rest}
    >
      {loading ? 'Working…' : children}
    </button>
  )
}
