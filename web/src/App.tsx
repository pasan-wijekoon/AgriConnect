import { Navigate, Route, Routes, useNavigate } from 'react-router-dom'
import { ProtectedRoute } from './components/ProtectedRoute'
import { useAuth } from './context/AuthContext'
import { LoginPage } from './pages/LoginPage'
import { FarmerDashboard } from './pages/FarmerDashboard'
import { BuyerDashboard } from './pages/BuyerDashboard'
import { AdminDashboard } from './pages/AdminDashboard'
import { InspectionQueuePage } from './pages/InspectionQueuePage'
import { RecordInspectionPage } from './pages/RecordInspectionPage'
import { DiscrepancyQueuePage } from './pages/DiscrepancyQueuePage'
import { InspectionHistoryPage } from './pages/InspectionHistoryPage'
import { PublishGatePage } from './pages/PublishGatePage'
import { OrderQueuePage } from './pages/orders/OrderQueuePage'
import { OrderDetailPage } from './pages/orders/OrderDetailPage'
import { ScheduleCalendarPage } from './pages/orders/ScheduleCalendarPage'
import { MyOrdersPage } from './pages/MyOrdersPage'
import { PriceTrendsPage } from './pages/PriceTrendsPage'
import { ShortagesPage } from './pages/ShortagesPage'
import { AnomalyQueuePage } from './pages/AnomalyQueuePage'
import { ReportsPage } from './pages/ReportsPage'

function Forbidden() { return <div className="glass-card"><h1>Access denied</h1><p>Your account cannot access this page.</p></div> }
function RoleRoute({ roles, children }: { roles: string[]; children: React.ReactNode }) {
  const { user } = useAuth()
  return user && roles.includes(user.role) ? <>{children}</> : <Forbidden />
}
function Home() {
  const { user } = useAuth()
  const navigate = useNavigate()
  if (user?.role === 'Farmer') return <FarmerDashboard />
  if (user?.role === 'Buyer') return <BuyerDashboard onOpenOrders={() => navigate('/my-orders')} />
  if (user?.role === 'Administrator') return <AdminDashboard />
  return <PriceTrendsPage />
}
export default function App() {
  return <Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route element={<ProtectedRoute />}>
      <Route path="/dashboard" element={<Home />} />
      <Route path="/quality/inspections" element={<RoleRoute roles={['Officer','Administrator']}><InspectionQueuePage /></RoleRoute>} />
      <Route path="/quality/inspections/:listingId/record" element={<RoleRoute roles={['Officer','Administrator']}><RecordInspectionPage /></RoleRoute>} />
      <Route path="/quality/discrepancies" element={<RoleRoute roles={['Officer','Administrator']}><DiscrepancyQueuePage /></RoleRoute>} />
      <Route path="/quality/history" element={<RoleRoute roles={['Officer','Administrator']}><InspectionHistoryPage /></RoleRoute>} />
      <Route path="/quality/publish/:listingId" element={<RoleRoute roles={['Officer','Administrator']}><PublishGatePage /></RoleRoute>} />
      <Route path="/my-orders" element={<RoleRoute roles={['Buyer','Farmer']}><MyOrdersPage /></RoleRoute>} />
      <Route path="/orders" element={<RoleRoute roles={['Buyer','Officer','Administrator']}><OrderQueuePage /></RoleRoute>} />
      <Route path="/orders/:orderId" element={<RoleRoute roles={['Buyer','Officer','Administrator']}><OrderDetailPage /></RoleRoute>} />
      <Route path="/orders/schedule" element={<RoleRoute roles={['Officer','Administrator']}><ScheduleCalendarPage /></RoleRoute>} />
      <Route path="/analytics/price-trends" element={<RoleRoute roles={['Officer','Administrator']}><PriceTrendsPage /></RoleRoute>} />
      <Route path="/analytics/shortages" element={<RoleRoute roles={['Officer','Administrator']}><ShortagesPage /></RoleRoute>} />
      <Route path="/analytics/anomalies" element={<RoleRoute roles={['Officer','Administrator']}><AnomalyQueuePage /></RoleRoute>} />
      <Route path="/analytics/reports" element={<RoleRoute roles={['Officer','Administrator']}><ReportsPage /></RoleRoute>} />
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Route>
  </Routes>
}
