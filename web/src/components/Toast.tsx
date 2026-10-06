import { useEffect } from 'react';

export type ToastKind = 'success' | 'error' | 'warning' | 'info';

export interface ToastData {
  kind: ToastKind;
  /** Short headline. Optional for simple confirmations. */
  title?: string;
  message: string;
  action?: { label: string; onClick: () => void };
}

const ICON: Record<ToastKind, string> = {
  success: 'M20 6 9 17l-5-5',
  error: 'M12 8v5M12 16.5v.01M10.3 3.9 2.4 17.5A2 2 0 0 0 4.1 20.5h15.8a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z',
  warning: 'M12 8v5M12 16.5v.01M10.3 3.9 2.4 17.5A2 2 0 0 0 4.1 20.5h15.8a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z',
  info: 'M12 11v5M12 7.5v.01M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18Z',
};

/**
 * A dismissible notification card: coloured by kind, with an icon, an optional title,
 * the message and an optional action button. Renders nothing when `toast` is null and
 * closes itself after a while (longer when there is something to read or act on).
 */
export function Toast({ toast, onClose }: { toast: ToastData | null; onClose: () => void }) {
  useEffect(() => {
    if (!toast) return;
    const long = toast.kind === 'error' || toast.kind === 'warning' || Boolean(toast.action);
    const timer = setTimeout(onClose, long ? 14000 : 4000);
    return () => clearTimeout(timer);
  }, [toast, onClose]);

  if (!toast) return null;

  return (
    <div className={`app-toast app-toast-${toast.kind}`} role={toast.kind === 'error' ? 'alert' : 'status'}>
      <span className="app-toast-icon" aria-hidden="true">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d={ICON[toast.kind]} />
        </svg>
      </span>
      <div className="app-toast-body">
        {toast.title && <div className="app-toast-title">{toast.title}</div>}
        <div className="app-toast-message">{toast.message}</div>
        {toast.action && (
          <button
            type="button"
            className="btn btn-primary app-toast-action"
            onClick={() => {
              toast.action?.onClick();
              onClose();
            }}
          >
            {toast.action.label}
          </button>
        )}
      </div>
      <button type="button" className="app-toast-close" aria-label="Dismiss" onClick={onClose}>
        ×
      </button>
    </div>
  );
}
