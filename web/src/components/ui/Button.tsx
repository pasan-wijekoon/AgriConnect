import React from 'react';
import { Loader2 } from 'lucide-react';

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'destructive' | 'outline' | 'ghost';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
  icon?: React.ReactNode;
}

export const Button: React.FC<ButtonProps> = ({
  children,
  variant = 'primary',
  size = 'md',
  isLoading = false,
  icon,
  className = '',
  disabled,
  style,
  ...props
}) => {
  const getStyles = (): React.CSSProperties => {
    let bg = 'var(--primary-green)';
    let color = '#FFFFFF';
    let border = '1px solid transparent';

    if (variant === 'secondary' || variant === 'outline') {
      bg = '#FFFFFF';
      color = 'var(--text-primary)';
      border = '1px solid var(--border-color)';
    } else if (variant === 'destructive') {
      bg = '#DC2626';
      color = '#FFFFFF';
      border = '1px solid transparent';
    } else if (variant === 'ghost') {
      bg = 'transparent';
      color = 'var(--text-primary)';
      border = '1px solid transparent';
    }

    let padding = '8px 16px';
    let fontSize = '14px';
    let height = '38px';

    if (size === 'sm') {
      padding = '6px 12px';
      fontSize = '13px';
      height = '32px';
    } else if (size === 'lg') {
      padding = '10px 20px';
      fontSize = '16px';
      height = '44px';
    }

    return {
      display: 'inline-flex',
      alignItems: 'center',
      justifyContent: 'center',
      gap: '8px',
      backgroundColor: bg,
      color: color,
      border: border,
      borderRadius: 'var(--radius-sm)',
      padding,
      fontSize,
      height,
      fontWeight: 500,
      cursor: disabled || isLoading ? 'not-allowed' : 'pointer',
      opacity: disabled || isLoading ? 0.65 : 1,
      transition: 'all 0.15s ease-in-out',
      whiteSpace: 'nowrap',
      userSelect: 'none',
      ...style
    };
  };

  return (
    <button
      disabled={disabled || isLoading}
      style={getStyles()}
      className={`btn ${className}`}
      {...props}
    >
      {isLoading ? <Loader2 size={16} className="animate-spin" /> : icon}
      {children}
    </button>
  );
};
