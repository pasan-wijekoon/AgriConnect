import React from 'react';

export interface FormControlProps {
  label?: string;
  error?: string;
  helperText?: string;
  required?: boolean;
}

export const FormGroup: React.FC<FormControlProps & { children: React.ReactNode; id?: string }> = ({
  label,
  error,
  helperText,
  required,
  children,
  id
}) => {
  return (
    <div style={{ marginBottom: '16px', display: 'flex', flexDirection: 'column', gap: '6px' }}>
      {label && (
        <label
          htmlFor={id}
          style={{
            fontSize: '13px',
            fontWeight: 600,
            color: 'var(--text-primary)',
            display: 'flex',
            alignItems: 'center',
            gap: '4px'
          }}
        >
          {label}
          {required && <span style={{ color: '#DC2626' }}>*</span>}
        </label>
      )}
      {children}
      {error ? (
        <span style={{ fontSize: '12px', color: 'var(--status-error-text)', fontWeight: 500 }}>
          {error}
        </span>
      ) : helperText ? (
        <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>{helperText}</span>
      ) : null}
    </div>
  );
};

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement>, FormControlProps {}

export const Input: React.FC<InputProps> = ({
  label,
  error,
  helperText,
  required,
  id,
  style,
  ...props
}) => {
  const inputId = id || (label ? label.toLowerCase().replace(/\s+/g, '-') : undefined);

  return (
    <FormGroup label={label} error={error} helperText={helperText} required={required} id={inputId}>
      <input
        id={inputId}
        style={{
          width: '100%',
          padding: '8px 12px',
          fontSize: '14px',
          borderRadius: 'var(--radius-sm)',
          border: `1px solid ${error ? 'var(--status-error-border)' : 'var(--border-color)'}`,
          backgroundColor: '#FFFFFF',
          color: 'var(--text-primary)',
          outline: 'none',
          boxSizing: 'border-box',
          transition: 'border-color 0.15s ease',
          ...style
        }}
        {...props}
      />
    </FormGroup>
  );
};

export interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement>, FormControlProps {
  options?: { value: string; label: string; disabled?: boolean }[];
}

export const Select: React.FC<SelectProps> = ({
  label,
  error,
  helperText,
  required,
  id,
  options,
  children,
  style,
  ...props
}) => {
  const selectId = id || (label ? label.toLowerCase().replace(/\s+/g, '-') : undefined);

  return (
    <FormGroup label={label} error={error} helperText={helperText} required={required} id={selectId}>
      <select
        id={selectId}
        style={{
          width: '100%',
          padding: '8px 12px',
          fontSize: '14px',
          borderRadius: 'var(--radius-sm)',
          border: `1px solid ${error ? 'var(--status-error-border)' : 'var(--border-color)'}`,
          backgroundColor: '#FFFFFF',
          color: 'var(--text-primary)',
          outline: 'none',
          boxSizing: 'border-box',
          cursor: 'pointer',
          ...style
        }}
        {...props}
      >
        {options
          ? options.map((opt) => (
              <option key={opt.value} value={opt.value} disabled={opt.disabled}>
                {opt.label}
              </option>
            ))
          : children}
      </select>
    </FormGroup>
  );
};

export interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement>, FormControlProps {}

export const Textarea: React.FC<TextareaProps> = ({
  label,
  error,
  helperText,
  required,
  id,
  style,
  ...props
}) => {
  const textareaId = id || (label ? label.toLowerCase().replace(/\s+/g, '-') : undefined);

  return (
    <FormGroup label={label} error={error} helperText={helperText} required={required} id={textareaId}>
      <textarea
        id={textareaId}
        rows={props.rows || 3}
        style={{
          width: '100%',
          padding: '10px 12px',
          fontSize: '14px',
          borderRadius: 'var(--radius-sm)',
          border: `1px solid ${error ? 'var(--status-error-border)' : 'var(--border-color)'}`,
          backgroundColor: '#FFFFFF',
          color: 'var(--text-primary)',
          outline: 'none',
          boxSizing: 'border-box',
          resize: 'vertical',
          ...style
        }}
        {...props}
      />
    </FormGroup>
  );
};
