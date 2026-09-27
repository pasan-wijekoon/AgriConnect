import { Navigate, Outlet, Route, Routes } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import { OrderQueuePage } from './pages/orders/OrderQueuePage'
import { OrderDetailPage } from './pages/orders/OrderDetailPage'
import { ScheduleCalendarPage } from './pages/orders/ScheduleCalendarPage'
import { PriceTrendsPage } from './pages/PriceTrendsPage'
import { ShortagesPage } from './pages/ShortagesPage'
import { AnomalyQueuePage } from './pages/AnomalyQueuePage'
import { ReportsPage } from './pages/ReportsPage'
import './styles/analytics.css'

// Component D's pages (Market Price Analytics & Reporting) were built as their own
// generically-classed design (.card, .badge, etc. — web/src/styles/analytics.css).
// This wrapper scopes that CSS to just this route subtree so it can't collide with
// the rest of the app's own design-system CSS (web/src/components/ui/*.css).
function AnalyticsScope() {
  return (
    <div className="analytics-scope">
      <Outlet />
    </div>
  )
}

function App() {
  return (
    <AppShell>
      <Routes>
        <Route path="/" element={<Navigate to="/orders" replace />} />
        <Route path="/orders" element={<OrderQueuePage />} />
        <Route path="/orders/schedule" element={<ScheduleCalendarPage />} />
        <Route path="/orders/:orderId" element={<OrderDetailPage />} />
        <Route element={<AnalyticsScope />}>
          <Route path="/analytics/price-trends" element={<PriceTrendsPage />} />
          <Route path="/analytics/shortages" element={<ShortagesPage />} />
          <Route path="/analytics/anomalies" element={<AnomalyQueuePage />} />
          <Route path="/analytics/reports" element={<ReportsPage />} />
        </Route>
      </Routes>
    </AppShell>
  )
}

export default App
