import type { ComponentType } from 'react'

import './App.css'

import { Icon } from './components/ui.tsx'
import { DEV_ROLE_SWITCH, setRole, useRole, type StaffRole } from './context/session.ts'
import { AnomalyQueuePage } from './pages/AnomalyQueuePage.tsx'
import { PriceTrendsPage } from './pages/PriceTrendsPage.tsx'
import { ReportsPage } from './pages/ReportsPage.tsx'
import { ShortagesPage } from './pages/ShortagesPage.tsx'
import { routeHref, useRoute, type Route } from './utils/router.ts'

const NAV: { route: Route; label: string; icon: 'chart' | 'grid' | 'flag' | 'file'; adminOnly?: boolean }[] = [
  { route: 'price-trends', label: 'Price trends', icon: 'chart' },
  { route: 'shortages', label: 'Shortages', icon: 'grid' },
  { route: 'anomalies', label: 'Anomaly queue', icon: 'flag' },
  { route: 'reports', label: 'Reports', icon: 'file', adminOnly: true },
]

const PAGES: Record<Route, ComponentType> = {
  'price-trends': PriceTrendsPage,
  shortages: ShortagesPage,
  anomalies: AnomalyQueuePage,
  reports: ReportsPage,
}

function App() {
  const route = useRoute()
  const role = useRole()
  const Page = PAGES[route]

  return (
    <div className="shell">
      <aside className="sidebar">
        <a className="brand" href={routeHref('price-trends')}>
          <img src="/logo.png" alt="" width="28" height="28" />
          <span>
            AgriConnect
            <small>Market analytics</small>
          </span>
        </a>
        <nav aria-label="Analytics">
          {NAV.map((item) => (
            <a
              key={item.route}
              href={routeHref(item.route)}
              className={`nav-link${route === item.route ? ' is-active' : ''}`}
              aria-current={route === item.route ? 'page' : undefined}
            >
              <Icon name={item.icon} />
              <span>{item.label}</span>
              {item.adminOnly && <span className="nav-tag">Admin</span>}
            </a>
          ))}
        </nav>
        {DEV_ROLE_SWITCH && (
          <label className="role-switch">
            <span>Signed in as (dev)</span>
            <select value={role} onChange={(e) => setRole(e.target.value as StaffRole)}>
              <option value="Officer">Officer</option>
              <option value="Administrator">Administrator</option>
            </select>
          </label>
        )}
      </aside>
      <main className="main">
        <Page key={role} />
      </main>
    </div>
  )
}

export default App
