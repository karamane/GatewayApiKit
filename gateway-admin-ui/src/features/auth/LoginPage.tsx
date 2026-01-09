import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { Card, Form, Input, Button, Typography, message, Space } from 'antd';
import { LockOutlined, ApiOutlined } from '@ant-design/icons';
import { useAuth } from './AuthContext';
import { validateApiKey } from '../../services/gatewayService';

const { Title, Text } = Typography;

interface LocationState {
  from?: { pathname: string };
}

/**
 * Login sayfası
 */
export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, isAuthenticated } = useAuth();
  const [loading, setLoading] = useState(false);

  const from = (location.state as LocationState)?.from?.pathname || '/routing';

  // Zaten giriş yapmışsa yönlendir
  React.useEffect(() => {
    if (isAuthenticated) {
      navigate(from, { replace: true });
    }
  }, [isAuthenticated, navigate, from]);

  const handleSubmit = async (values: { apiKey: string }) => {
    setLoading(true);
    try {
      const result = await validateApiKey(values.apiKey);
      if (result.valid) {
        login(values.apiKey);
        message.success('Giriş başarılı');
        navigate(from, { replace: true });
      } else {
        message.error(result.message || 'Geçersiz API Key');
      }
    } catch {
      message.error('Bağlantı hatası');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #1e3a5f 0%, #0f172a 100%)',
        padding: 20,
      }}
    >
      <Card
        style={{
          width: '100%',
          maxWidth: 400,
          boxShadow: '0 10px 40px rgba(0,0,0,0.3)',
        }}
      >
        <Space
          direction="vertical"
          size="large"
          style={{ width: '100%', textAlign: 'center' }}
        >
          {/* Logo */}
          <div>
            <ApiOutlined style={{ fontSize: 48, color: '#3b82f6' }} />
            <Title level={3} style={{ marginTop: 16, marginBottom: 4 }}>
              Gateway Yönetim Paneli
            </Title>
            <Text type="secondary">
              Devam etmek için API Key girin
            </Text>
          </div>

          {/* Form */}
          <Form
            layout="vertical"
            onFinish={handleSubmit}
            style={{ textAlign: 'left' }}
          >
            <Form.Item
              name="apiKey"
              rules={[
                { required: true, message: 'API Key gereklidir' },
                { min: 16, message: 'API Key en az 16 karakter olmalıdır' },
              ]}
            >
              <Input.Password
                prefix={<LockOutlined />}
                placeholder="API Key'inizi girin"
                size="large"
                autoComplete="off"
              />
            </Form.Item>

            <Form.Item style={{ marginBottom: 0 }}>
              <Button
                type="primary"
                htmlType="submit"
                size="large"
                block
                loading={loading}
              >
                Giriş Yap
              </Button>
            </Form.Item>
          </Form>

          {/* Footer */}
          <Text type="secondary" style={{ fontSize: 12 }}>
            API Key için sistem yöneticinizle iletişime geçin
          </Text>
        </Space>
      </Card>
    </div>
  );
};
