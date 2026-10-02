import { type ReactNode } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'

import { useTheme } from '../../context/ThemeContext'
import { NotificationBell } from '../NotificationBell'
import {
  Sprout,
  Layers,
  TrendingUp,
  ShoppingBag,
  Calendar,
  Layers as BarChart2,
  AlertCircle,
  ShieldCheck as ClipboardCheck,
  ShieldCheck,
  LogOut,
  Sparkles,
  Sprout as Sun,
  Sprout as Moon,
} from '../Icons'
import './AppShell.css'

// ── Sidebar nav configuration ──────────────────────────────────────────────

type UserRole = 'Farmer' | 'Buyer' | 'Officer' | 'Administrator'

interface NavItem {
  to: string
  label: string
  icon: React.FC<{ size?: number }>
  roles: UserRole[]
  end?: boolean
}

interface NavSection {
  section: string
  items: NavItem[]
  roles: UserRole[] // section is visible if user role is in this list
}

const SIDEBAR_NAV: NavSection[] = [
  {
    section: 'Marketplace',
    roles: ['Farmer', 'Buyer', 'Officer', 'Administrator'],
    items: [
      {
        to: '/dashboard',
        label: 'Dashboard',
        icon: Layers,
        roles: ['Farmer', 'Buyer', 'Officer', 'Administrator'],
        end: true,
      },
    ],
  },
  {
    section: 'My Orders',
    roles: ['Farmer', 'Buyer'],
    items: [
      {
        to: '/my-orders',
        label: 'Orders',
        icon: ShoppingBag,
        roles: ['Farmer', 'Buyer'],
      },
    ],
  },
  {
    section: 'Quality Control',
    roles: ['Officer', 'Administrator'],
    items: [
      {
        to: '/quality/inspections',
        label: 'Inspection Queue',
        icon: ClipboardCheck,
        roles: ['Officer', 'Administrator'],
      },
      {
        to: '/quality/discrepancies',
        label: 'Discrepancies',
        icon: AlertCircle,
        roles: ['Officer', 'Administrator'],
      },
      {
        to: '/quality/history',
        label: 'Inspection History',
        icon: Layers,
        roles: ['Officer', 'Administrator'],
      },
    ],
  },
  {
    section: 'Orders & Logistics',
    roles: ['Officer', 'Administrator'],
    items: [
      {
        to: '/orders',
        label: 'Orders',
        icon: ShoppingBag,
        roles: ['Officer', 'Administrator'],
      },
      {
        to: '/orders/schedule',
        label: 'Schedule',
        icon: Calendar,
        roles: ['Officer', 'Administrator'],
      },
    ],
  },
  {
    section: 'Analytics',
    roles: ['Officer', 'Administrator'],
    items: [
      {
        to: '/analytics/price-trends',
        label: 'Price Trends',
        icon: TrendingUp,
        roles: ['Officer', 'Administrator'],
      },
      {
        to: '/analytics/shortages',
        label: 'Shortages',
        icon: AlertCircle,
        roles: ['Officer', 'Administrator'],
      },
      {
        to: '/analytics/anomalies',
        label: 'Anomaly Queue',
        icon: Sparkles,
        roles: ['Officer', 'Administrator'],
      },
      {
        to: '/analytics/reports',
        label: 'Reports',
        icon: BarChart2,
        roles: ['Officer', 'Administrator'],
      },
    ],
  },
  {
    section: 'Administration',
    roles: ['Administrator'],
    items: [
      {
        to: '/dashboard',
        label: 'Listing Approvals',
        icon: ShieldCheck,
        roles: ['Administrator'],
        end: true,
      },
    ],
  },
]

// ── Role label & display helpers ───────────────────────────────────────────

const ROLE_LABEL: Record<UserRole, string> = {
  Farmer: 'Farmer',
  Buyer: 'Buyer',
  Officer: 'Officer',
  Administrator: 'Administrator',
}

const ROLE_ICON: Record<UserRole, React.FC<{ size?: number }>> = {
  Farmer: Sprout,
  Buyer: ShoppingBag,
  Officer: ShieldCheck,
  Administrator: ShieldCheck,
}

// ── AppShell ───────────────────────────────────────────────────────────────

/**
 * Unified application shell used by every route. Renders a sticky top header
 * and a role-aware sidebar. Replaces the old split between Navbar.tsx
 * (marketplace) and the previous AppShell (back-office) to give every
 * stakeholder a consistent navigation experience.
 *
 * Auth bridge: when a real Admin user is logged in the DevIdentityContext is
 * automatically seeded with their id and the Administrator role so that
 * back-office pages (Orders, Analytics, Quality) receive the correct
 * X-Dev-Role / X-Dev-UserId request headers without exposing the dev picker UI.
 */
export function AppShell({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth()
  
  const { theme, toggle } = useTheme()
  const navigate = useNavigate()

  const role: UserRole = (user?.role as UserRole | undefined) ?? 'Buyer'

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  const RoleIcon = user ? ROLE_ICON[role] : null

  return (
    <div className="app-shell">
      {/* ── Top header ── */}
      <header className="app-header">
        {/* Brand */}
        <NavLink to="/dashboard" className="app-header-brand" title="Go to Dashboard">
          <div className="app-header-brand-icon">
            <Sprout size={18} />
          </div>
          <span>AgriConnect</span>
        </NavLink>

        {/* Theme toggle */}
        <button
          onClick={toggle}
          className="app-header-theme-toggle"
          title={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
          aria-label={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
        >
          {theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}
        </button>

        {/* Notifications (FR22) */}
        {user && <NotificationBell theme={theme} />}

        {/* User badge + logout */}
        {user && (
          <div className="app-header-user">
            <div className="app-header-user-avatar">
              {user.fullName.charAt(0).toUpperCase()}
            </div>
            <div className="app-header-user-info">
              <span className="app-header-user-name">{user.fullName}</span>
              <span className="app-header-user-role">
                {RoleIcon && <RoleIcon size={11} />}
                {ROLE_LABEL[role]}
                {user.region && <>&nbsp;·&nbsp;{user.region}</>}
              </span>
            </div>
            <button
              onClick={handleLogout}
              title="Sign out"
              className="app-header-logout"
              aria-label="Sign out"
            >
              <LogOut size={15} />
            </button>
          </div>
        )}
      </header>

      {/* ── Body: sidebar + main ── */}
      <div className="app-body">
        <nav className="app-sidebar" aria-label="Primary navigation">
          {SIDEBAR_NAV.filter((section) => section.roles.includes(role)).map((section) => {
            const visibleItems = section.items.filter((item) => item.roles.includes(role))
            if (visibleItems.length === 0) return null
            return (
              <div key={section.section}>
                <div className="app-sidebar-section">{section.section}</div>
                {visibleItems.map((item) => {
                  const Icon = item.icon
                  return (
                    <NavLink
                      key={item.to + item.label}
                      to={item.to}
                      end={item.end}
                      className={({ isActive }) =>
                        `app-sidebar-link${isActive ? ' active' : ''}`
                      }
                    >
                      <Icon size={15} />
                      <span>{item.label}</span>
                    </NavLink>
                  )
                })}
              </div>
            )
          })}
        </nav>

        <main className="app-main">{children}</main>
      </div>
    </div>
  )
}
