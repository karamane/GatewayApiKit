import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ConfigProvider } from 'antd';
import trTR from 'antd/locale/tr_TR';
import dayjs from 'dayjs';
import 'dayjs/locale/tr';
import App from './App';
import './index.css';

// Türkçe locale ayarla
dayjs.locale('tr');

// React Query client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
      staleTime: 30000, // 30 saniye
    },
  },
});

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <ConfigProvider
        locale={trTR}
        theme={{
          token: {
            colorPrimary: '#3b82f6',
            colorSuccess: '#059669',
            colorError: '#dc2626',
            colorWarning: '#f59e0b',
            borderRadius: 8,
            fontFamily: "'Segoe UI', system-ui, -apple-system, sans-serif",
          },
        }}
      >
        <BrowserRouter basename="/admin">
          <App />
        </BrowserRouter>
      </ConfigProvider>
    </QueryClientProvider>
  </React.StrictMode>
);
