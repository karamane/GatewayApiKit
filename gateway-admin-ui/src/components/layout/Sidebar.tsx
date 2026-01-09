import React from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { Layout, Menu } from 'antd';
import {
  ApiOutlined,
  CloudServerOutlined,
  HeartOutlined,
  LogoutOutlined,
} from '@ant-design/icons';
import { clearApiKey } from '../../services/api';

const { Sider } = Layout;

/**
 * Sol menü navigasyonu
 */
export const Sidebar: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    clearApiKey();
    navigate('/login');
  };

  const menuItems = [
    {
      key: '/routing',
      icon: <ApiOutlined />,
      label: 'Routing Yönetimi',
    },
    {
      key: '/load-balancing',
      icon: <CloudServerOutlined />,
      label: 'Yük Dengeleme',
    },
    {
      key: '/system-health',
      icon: <HeartOutlined />,
      label: 'Sistem Sağlığı',
    },
    {
      type: 'divider' as const,
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: 'Çıkış',
      danger: true,
    },
  ];

  const handleMenuClick = ({ key }: { key: string }) => {
    if (key === 'logout') {
      handleLogout();
    } else {
      navigate(key);
    }
  };

  // Aktif menü öğesini belirle
  const getSelectedKey = () => {
    const path = location.pathname;
    if (path.startsWith('/routing')) return '/routing';
    if (path.startsWith('/load-balancing')) return '/load-balancing';
    if (path.startsWith('/system-health')) return '/system-health';
    return '/routing';
  };

  return (
    <Sider
      width={240}
      style={{
        background: 'linear-gradient(135deg, #1e3a5f 0%, #0f172a 100%)',
        minHeight: '100vh',
      }}
    >
      {/* Logo */}
      <div
        style={{
          padding: '20px 16px',
          textAlign: 'center',
          borderBottom: '1px solid rgba(255,255,255,0.1)',
        }}
      >
        <div
          style={{
            color: '#fff',
            fontSize: 20,
            fontWeight: 600,
          }}
        >
          🌐 Gateway
        </div>
        <div
          style={{
            color: 'rgba(255,255,255,0.6)',
            fontSize: 12,
            marginTop: 4,
          }}
        >
          Yönetim Paneli
        </div>
      </div>

      {/* Menu */}
      <Menu
        theme="dark"
        mode="inline"
        selectedKeys={[getSelectedKey()]}
        items={menuItems}
        onClick={handleMenuClick}
        style={{
          background: 'transparent',
          borderRight: 0,
          marginTop: 16,
        }}
      />
    </Sider>
  );
};
