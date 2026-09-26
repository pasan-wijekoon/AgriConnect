import React, { useState } from 'react';
import './index.css';
import { useAuth } from './context/AuthContext';
import { Navbar } from './components/Navbar';
import { LoginPage } from './pages/LoginPage';
import { FarmerDashboard } from './pages/FarmerDashboard';
import { BuyerDashboard } from './pages/BuyerDashboard';
import { AdminDashboard } from './pages/AdminDashboard';
import { TodayPricesPage } from './pages/TodayPricesPage';

export const App: React.FC = () => {
  const { user, isLoading } = useAuth();
  const [currentView, setCurrentView] = useState<'dashboard' | 'today-prices'>('dashboard');

  // Loading spinner while restoring session
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
    );
  }

  // Not logged in → show login page
  if (!user) {
    return <LoginPage />;
  }

  // Logged in → role-based dashboard or today prices with navbar
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
              setCurrentView('dashboard');
            }}
            onBrowseProduce={(_crop) => {
              setCurrentView('dashboard');
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
  );
};

export default App;
