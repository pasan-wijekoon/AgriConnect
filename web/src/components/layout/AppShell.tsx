import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'
import { useAuth, type DevRole } from '../../context/AuthContext'
import { setRole as setAnalyticsRole } from '../../context/session'
import './AppShell.css'

const NAV_ITEMS = [
  { to: '/orders', label: 'Orders' },
  { to: '/orders/schedule', label: 'Schedule' },
]

// Component D's analytics pages (Market Price Analytics & Reporting) are
// Officer/Administrator-only, matching their backend [Authorize(Roles=...)] gates.
const ANALYTICS_NAV_ITEMS = [
  { to: '/analytics/price-trends', label: 'Price Trends' },
  { to: '/analytics/shortages', label: 'Shortages' },
  { to: '/analytics/anomalies', label: 'Anomaly Queue' },
  { to: '/analytics/reports', label: 'Reports', adminOnly: true },
]

const DEV_ROLES: DevRole[] = ['Buyer', 'Farmer', 'Officer', 'Administrator']

/**
 * Design.md §11/§12 — the consistent header + sidebar shell every
 * team-developed page shares. The role/user-id picker on the right is a
 * dev-only stand-in for real sign-in (no shared User/Auth/JWT exists yet,
 * plan §6) — it sets the X-Dev-Role/X-Dev-UserId headers every API call uses.
 */
export function AppShell({ children }: { children: ReactNode }) {
  const { identity, setIdentity } = useAuth()

  // Component D's analytics pages read their role from their own session.ts store
  // (web/src/context/session.ts, predates this integration) rather than this
  // AuthContext. Rather than rewire their 4 already-tested pages onto a second
  // context, this picker is the single source of truth and just mirrors every
  // change into session.ts too, so both stay in sync from one control.
  const updateIdentity = (next: typeof identity) => {
    setIdentity(next)
    setAnalyticsRole(next.role === 'Administrator' ? 'Administrator' : 'Officer')
  }

  const showAnalyticsNav = identity.role === 'Officer' || identity.role === 'Administrator'

  return (
    <div className="app-shell">
      <header className="app-header">
        <span className="app-header-brand">AgriConnect</span>
        <div className="dev-identity" title="Dev-mode identity — stands in for real sign-in until shared auth lands">
          <select
            aria-label="Dev role"
            value={identity.role}
            onChange={(e) => updateIdentity({ ...identity, role: e.target.value as DevRole })}
          >
            {DEV_ROLES.map((role) => (
              <option key={role} value={role}>
                {role}
              </option>
            ))}
          </select>
          <input
            aria-label="Dev user id"
            value={identity.userId}
            onChange={(e) => updateIdentity({ ...identity, userId: e.target.value })}
          />
        </div>
      </header>
      <div className="app-body">
        <nav className="app-sidebar" aria-label="Primary">
          {NAV_ITEMS.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => `app-sidebar-link${isActive ? ' active' : ''}`}
            >
              {item.label}
            </NavLink>
          ))}
          {showAnalyticsNav && (
            <>
              <div className="app-sidebar-section">Analytics</div>
              {ANALYTICS_NAV_ITEMS.filter((item) => !item.adminOnly || identity.role === 'Administrator').map(
                (item) => (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    className={({ isActive }) => `app-sidebar-link${isActive ? ' active' : ''}`}
                  >
                    {item.label}
                  </NavLink>
                ),
              )}
            </>
          )}
        </nav>
        <main className="app-main">{children}</main>
      </div>
    </div>
  )
}
