import React from 'react';
import { Card, Space, Typography } from 'antd';
import { StatusDot } from './StatusDot';

const { Title } = Typography;

export interface ServiceCardProps {
  /** Kart başlığı */
  title: string;
  /** Durum */
  status?: boolean;
  /** Durum tooltip'i */
  statusHint?: string;
  /** İçerik */
  children: React.ReactNode;
  /** Ekstra başlık elementleri */
  extra?: React.ReactNode;
  /** Yükleniyor durumu */
  loading?: boolean;
}

/**
 * Servis/modül kartı - başlıkta durum noktası ile
 */
export const ServiceCard: React.FC<ServiceCardProps> = ({
  title,
  status,
  statusHint,
  children,
  extra,
  loading = false,
}) => {
  return (
    <Card
      loading={loading}
      title={
        <Space>
          <Title level={5} style={{ margin: 0 }}>
            {title}
          </Title>
          {status !== undefined && (
            <StatusDot status={status} hint={statusHint} />
          )}
        </Space>
      }
      extra={extra}
      style={{ marginBottom: 16 }}
    >
      {children}
    </Card>
  );
};
