import React from 'react';
import { Alert, Space, Typography, Tag } from 'antd';
import { WarningOutlined, ExclamationCircleOutlined } from '@ant-design/icons';
import type { ServiceHealth } from '../../../types';

const { Text } = Typography;

interface CriticalAlertsProps {
  services: ServiceHealth[];
}

/**
 * Kritik alarm banner'ı
 */
export const CriticalAlerts: React.FC<CriticalAlertsProps> = ({ services }) => {
  if (services.length === 0) {
    return null;
  }

  return (
    <Alert
      type="error"
      icon={<WarningOutlined />}
      showIcon
      message={
        <Space>
          <ExclamationCircleOutlined />
          <Text strong>Kritik Alarm</Text>
          <Tag color="error">{services.length} servis</Tag>
        </Space>
      }
      description={
        <Space direction="vertical" size="small" style={{ marginTop: 8 }}>
          <Text>
            Aşağıdaki servisler 3 veya daha fazla ardışık kontrolde çevrimdışı olarak tespit edildi.
            Acil müdahale gerekiyor.
          </Text>
          <Space wrap>
            {services.map((service) => (
              <Tag key={service.servisId} color="error">
                {service.servisId} ({service.hedefSistem})
                {service.hataMesaji && (
                  <span style={{ opacity: 0.8 }}>: {service.hataMesaji}</span>
                )}
              </Tag>
            ))}
          </Space>
        </Space>
      }
      style={{ marginBottom: 24 }}
    />
  );
};
