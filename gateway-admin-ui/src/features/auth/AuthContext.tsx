import React, { createContext, useContext, useState, useEffect } from 'react';
import { getApiKey, setApiKey as storeApiKey, clearApiKey } from '../../services/api';

interface AuthContextType {
  isAuthenticated: boolean;
  apiKey: string | null;
  login: (apiKey: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [apiKey, setApiKey] = useState<string | null>(() => getApiKey());

  useEffect(() => {
    // sessionStorage değişikliklerini dinle
    const handleStorageChange = () => {
      setApiKey(getApiKey());
    };

    window.addEventListener('storage', handleStorageChange);
    return () => window.removeEventListener('storage', handleStorageChange);
  }, []);

  const login = (key: string) => {
    storeApiKey(key);
    setApiKey(key);
  };

  const logout = () => {
    clearApiKey();
    setApiKey(null);
  };

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated: !!apiKey,
        apiKey,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
