import React from 'react';
import { Space, Typography, Button, Tooltip } from 'antd';
import { ReloadOutlined } from '@ant-design/icons';

const { Title, Text } = Typography;

export interface PageHeaderProps {
  /** Sayfa başlığı */
  title: string;
  /** Alt başlık */
  subtitle?: string;
  /** Yenile callback'i */
  onRefresh?: () => void;
  /** Yenileniyor durumu */
  refreshing?: boolean;
  /** Ekstra aksiyonlar */
  extra?: React.ReactNode;
}

/**
 * Sayfa başlığı - yenile butonu ile
 */
export const PageHeader: React.FC<PageHeaderProps> = ({
  title,
  subtitle,
  onRefresh,
  refreshing = false,
  extra,
}) => {
  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'flex-start',
        marginBottom: 24,
      }}
    >
      <div>
        <Title level={3} style={{ margin: 0 }}>
          {title}
        </Title>
        {subtitle && (
          <Text type="secondary" style={{ marginTop: 4, display: 'block' }}>
            {subtitle}
          </Text>
        )}
      </div>
      <Space>
        {extra}
        {onRefresh && (
          <Tooltip title="Yenile">
            <Button
              icon={<ReloadOutlined spin={refreshing} />}
              onClick={onRefresh}
              loading={refreshing}
            >
              Yenile
            </Button>
          </Tooltip>
        )}
      </Space>
    </div>
  );
};
