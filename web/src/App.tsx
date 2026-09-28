import { useState } from 'react'
import { Outlet, Route, Routes } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import { OrderQueuePage } from './pages/orders/OrderQueuePage'
import { OrderDetailPage } from './pages/orders/OrderDetailPage'
import { ScheduleCalendarPage } from './pages/orders/ScheduleCalendarPage'
import { PriceTrendsPage } from './pages/PriceTrendsPage'
import { ShortagesPage } from './pages/ShortagesPage'
import { AnomalyQueuePage } from './pages/AnomalyQueuePage'
import { ReportsPage } from './pages/ReportsPage'
import { AiSchedulingPage } from './pages/AiSchedulingPage'
import { InspectionQueuePage } from './pages/InspectionQueuePage'
import { RecordInspectionPage } from './pages/RecordInspectionPage'
import { DiscrepancyQueuePage } from './pages/DiscrepancyQueuePage'
import { PublishGatePage } from './pages/PublishGatePage'
import { InspectionHistoryPage } from './pages/InspectionHistoryPage'
import './styles/analytics.css'
import './styles/marketplace.css'
import { useAuth } from './context/AuthContext'
import { Navbar } from './components/Navbar'
import { LoginPage } from './pages/LoginPage'
import { FarmerDashboard } from './pages/FarmerDashboard'
import { BuyerDashboard } from './pages/BuyerDashboard'
import { AdminDashboard } from './pages/AdminDashboard'
import { TodayPricesPage } from './pages/TodayPricesPage'

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

// Component B's Order/Scheduling console, Component D's Analytics dashboard, and
// Component C's Quality Grading & Inspection pages — internal Officer/
// Administrator tooling, still driven by the dev-role picker (DevIdentityContext)
// rather than the real login below; see AppShell's own doc comment for why. A
// layout route (renders AppShell + <Outlet/>), not its own nested <Routes> — a
// nested <Routes> under a "/orders/*" mount would need every child path written
// relative to that mount point (e.g. "" / "schedule"), which is easy to get
// wrong; one flat top-level <Routes> with absolute paths avoids that whole
// class of mistake.
function OrdersLayout() {
  return (
    <AppShell>
      <Outlet />
    </AppShell>
  )
}

// Component A's real-auth-gated produce marketplace (Farmer/Buyer/Admin
// dashboards, Today's Prices) — the public-facing storefront, as distinct
// from the OrdersConsole above (internal back-office tooling). Kept exactly
// as their own branch built it, including its own state-based view switching
// rather than nested router routes, to avoid rewriting already-tested code
// during integration.
function MarketplaceApp() {
  return (
    <div className="marketplace-scope">
      <MarketplaceAppInner />
    </div>
  )
}

function MarketplaceAppInner() {
  const { user, isLoading } = useAuth()
  const [currentView, setCurrentView] = useState<'dashboard' | 'today-prices'>('dashboard')

  if (isLoading) {
    return (
      <div style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        flexDirection: 'column',
        gap: '16px',
        color: '#94a3b8'
      }}>
        <div style={{
          width: '48px',
          height: '48px',
          borderRadius: '50%',
          border: '3px solid rgba(16, 185, 129, 0.2)',
          borderTopColor: '#10b981',
          animation: 'spin 0.8s linear infinite'
        }} />
        <span style={{ fontSize: '0.9rem', fontWeight: 600 }}>Loading AgriConnect...</span>
        <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
      </div>
    )
  }

  if (!user) {
    return <LoginPage />
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <Navbar
        currentView={currentView}
        onNavigate={(view) => setCurrentView(view)}
      />
      <main style={{ flex: 1 }}>
        {currentView === 'today-prices' ? (
          <TodayPricesPage
            onListProduce={(_crop) => {
              setCurrentView('dashboard')
            }}
            onBrowseProduce={(_crop) => {
              setCurrentView('dashboard')
            }}
          />
        ) : (
          <>
            {user.role === 'Farmer' && (
              <FarmerDashboard onOpenTodayPrices={() => setCurrentView('today-prices')} />
            )}
            {user.role === 'Buyer' && (
              <BuyerDashboard onOpenTodayPrices={() => setCurrentView('today-prices')} />
            )}
            {user.role === 'Admin' && <AdminDashboard />}
          </>
        )}
      </main>
    </div>
  )
}

function App() {
  return (
    <Routes>
      <Route element={<OrdersLayout />}>
        <Route path="/orders" element={<OrderQueuePage />} />
        <Route path="/orders/schedule" element={<ScheduleCalendarPage />} />
        <Route path="/orders/:orderId" element={<OrderDetailPage />} />
        <Route element={<AnalyticsScope />}>
          <Route path="/analytics/price-trends" element={<PriceTrendsPage />} />
          <Route path="/analytics/shortages" element={<ShortagesPage />} />
          <Route path="/analytics/anomalies" element={<AnomalyQueuePage />} />
          <Route path="/analytics/reports" element={<ReportsPage />} />
          <Route path="/analytics/ai-scheduling" element={<AiSchedulingPage />} />
        </Route>
        <Route path="/quality/inspections" element={<InspectionQueuePage />} />
        <Route path="/quality/inspections/:listingId/record" element={<RecordInspectionPage />} />
        <Route path="/quality/discrepancies" element={<DiscrepancyQueuePage />} />
        <Route path="/quality/publish/:listingId" element={<PublishGatePage />} />
        <Route path="/quality/history" element={<InspectionHistoryPage />} />
      </Route>
      {/* Everything else, including bare "/", is the real-auth marketplace —
          the primary consumer-facing app; /orders, /analytics and /quality
          above are reached by direct URL for internal Officer/Administrator use. */}
      <Route path="/*" element={<MarketplaceApp />} />
    </Routes>
  )
}

export default App
