import { Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import { OrderQueuePage } from './pages/orders/OrderQueuePage'
import { OrderDetailPage } from './pages/orders/OrderDetailPage'
import { ScheduleCalendarPage } from './pages/orders/ScheduleCalendarPage'

function App() {
  return (
    <AppShell>
      <Routes>
        <Route path="/" element={<Navigate to="/orders" replace />} />
        <Route path="/orders" element={<OrderQueuePage />} />
        <Route path="/orders/schedule" element={<ScheduleCalendarPage />} />
        <Route path="/orders/:orderId" element={<OrderDetailPage />} />
      </Routes>
    </AppShell>
  )
}

export default App
