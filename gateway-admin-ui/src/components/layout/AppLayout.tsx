import React from 'react';
import { Outlet } from 'react-router-dom';
import { Layout } from 'antd';
import { Sidebar } from './Sidebar';

const { Content } = Layout;

/**
 * Ana uygulama layout'u - sidebar + content
 */
export const AppLayout: React.FC = () => {
  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sidebar />
      <Layout>
        <Content
          style={{
            padding: 24,
            background: '#f3f4f6',
            minHeight: '100vh',
          }}
        >
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
};
