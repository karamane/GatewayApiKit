import { Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './features/auth/AuthContext';
import { ProtectedRoute } from './features/auth/ProtectedRoute';
import { LoginPage } from './features/auth/LoginPage';
import { AppLayout } from './components/layout/AppLayout';
import { RoutingPage } from './features/routing/RoutingPage';
import { LoadBalancingPage } from './features/load-balancing/LoadBalancingPage';
import { SystemHealthPage } from './features/system-health/SystemHealthPage';

function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/"
          element={
            <ProtectedRoute>
              <AppLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<Navigate to="/routing" replace />} />
          <Route path="routing" element={<RoutingPage />} />
          <Route path="load-balancing" element={<LoadBalancingPage />} />
          <Route path="system-health" element={<SystemHealthPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AuthProvider>
  );
}

export default App;
