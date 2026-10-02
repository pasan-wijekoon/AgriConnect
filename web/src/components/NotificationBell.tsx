import { useCallback, useEffect, useRef, useState } from 'react'
import { ordersApi, type NotificationResponse } from '../utils/ordersApi'
import './NotificationBell.css'

const POLL_MS = 60_000

function timeAgo(iso: string): string {
  const seconds = Math.max(0, Math.round((Date.now() - new Date(iso).getTime()) / 1000))
  if (seconds < 60) return 'just now'
  const minutes = Math.round(seconds / 60)
  if (minutes < 60) return `${minutes} min ago`
  const hours = Math.round(minutes / 60)
  if (hours < 24) return `${hours} h ago`
  return new Date(iso).toLocaleDateString()
}

/**
 * Bell + unread badge + dropdown inbox (FR22). Works in both visual systems
 * (the dark marketplace and the light officer console) via `theme`. Polls the
 * unread count every minute; the full list is loaded only when opened.
 */
export function NotificationBell({ theme }: { theme: 'light' | 'dark' }) {
  const [unread, setUnread] = useState(0)
  const [open, setOpen] = useState(false)
  const [items, setItems] = useState<NotificationResponse[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const rootRef = useRef<HTMLDivElement>(null)

  const refreshUnread = useCallback(() => {
    ordersApi
      .unreadNotificationCount()
      .then((r) => setUnread(r.count))
      .catch(() => {
        // The badge is best-effort; the inbox itself reports real errors.
      })
  }, [])

  useEffect(() => {
    refreshUnread()
    const timer = window.setInterval(refreshUnread, POLL_MS)
    return () => window.clearInterval(timer)
  }, [refreshUnread])

  useEffect(() => {
    if (!open) return
    const onDown = (e: MouseEvent) => {
      if (rootRef.current && !rootRef.current.contains(e.target as Node)) setOpen(false)
    }
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && setOpen(false)
    document.addEventListener('mousedown', onDown)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onDown)
      document.removeEventListener('keydown', onKey)
    }
  }, [open])

  const toggle = () => {
    const next = !open
    setOpen(next)
    if (next) {
      setError(null)
      ordersApi
        .listNotifications()
        .then((list) => {
          setItems(list)
          setUnread(list.filter((n) => !n.readAt).length)
        })
        .catch(() => setError('Could not load notifications.'))
    }
  }

  const markRead = (n: NotificationResponse) => {
    if (n.readAt) return
    ordersApi
      .markNotificationRead(n.id)
      .then(() => {
        setItems((prev) => prev?.map((x) => (x.id === n.id ? { ...x, readAt: new Date().toISOString() } : x)) ?? prev)
        setUnread((c) => Math.max(0, c - 1))
      })
      .catch(() => setError('Could not update that notification.'))
  }

  const markAll = () => {
    ordersApi
      .markAllNotificationsRead()
      .then(() => {
        setItems((prev) => prev?.map((x) => ({ ...x, readAt: x.readAt ?? new Date().toISOString() })) ?? prev)
        setUnread(0)
      })
      .catch(() => setError('Could not update notifications.'))
  }

  return (
    <div className="nb" data-theme={theme} ref={rootRef}>
      <button
        type="button"
        className="nb-button"
        aria-label={unread > 0 ? `Notifications, ${unread} unread` : 'Notifications'}
        aria-expanded={open}
        onClick={toggle}
      >
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
          <path d="M13.7 21a2 2 0 0 1-3.4 0" />
        </svg>
        {unread > 0 && <span className="nb-badge">{unread > 9 ? '9+' : unread}</span>}
      </button>

      {open && (
        <div className="nb-panel" role="dialog" aria-label="Notifications">
          <div className="nb-head">
            <strong>Notifications</strong>
            {unread > 0 && (
              <button type="button" className="nb-link" onClick={markAll}>
                Mark all read
              </button>
            )}
          </div>
          <div className="nb-list">
            {error && <p className="nb-empty">{error}</p>}
            {!error && items === null && <p className="nb-empty">Loading…</p>}
            {!error && items?.length === 0 && <p className="nb-empty">You’re all caught up.</p>}
            {items?.map((n) => (
              <button
                type="button"
                key={n.id}
                className={`nb-item${n.readAt ? '' : ' nb-unread'}`}
                onClick={() => markRead(n)}
              >
                <span className="nb-msg">{n.title ?? n.message}</span>
                {n.title && <span className="nb-sub">{n.message}</span>}
                <span className="nb-time">{timeAgo(n.createdAt)}</span>
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
