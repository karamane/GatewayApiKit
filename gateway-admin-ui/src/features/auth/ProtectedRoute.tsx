import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext';

interface ProtectedRouteProps {
  children: React.ReactNode;
}

/**
 * Kimlik doğrulama gerektiren route wrapper'ı
 */
export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children }) => {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    // Kullanıcıyı login sayfasına yönlendir, geldiği yeri sakla
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <>{children}</>;
};
