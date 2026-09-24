import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'
import { useAuth, type DevRole } from '../../context/AuthContext'
import './AppShell.css'

const NAV_ITEMS = [
  { to: '/orders', label: 'Orders' },
  { to: '/orders/schedule', label: 'Schedule' },
]

const DEV_ROLES: DevRole[] = ['Buyer', 'Farmer', 'Officer', 'Admin']

/**
 * Design.md §11/§12 — the consistent header + sidebar shell every
 * team-developed page shares. The role/user-id picker on the right is a
 * dev-only stand-in for real sign-in (no shared User/Auth/JWT exists yet,
 * plan §6) — it sets the X-Dev-Role/X-Dev-UserId headers every API call uses.
 */
export function AppShell({ children }: { children: ReactNode }) {
  const { identity, setIdentity } = useAuth()

  return (
    <div className="app-shell">
      <header className="app-header">
        <span className="app-header-brand">AgriConnect</span>
        <div className="dev-identity" title="Dev-mode identity — stands in for real sign-in until shared auth lands">
          <select
            aria-label="Dev role"
            value={identity.role}
            onChange={(e) => setIdentity({ ...identity, role: e.target.value as DevRole })}
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
            onChange={(e) => setIdentity({ ...identity, userId: e.target.value })}
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
        </nav>
        <main className="app-main">{children}</main>
      </div>
    </div>
  )
}
