import React from 'react';
import { Card, Slider, Space, Tag, Typography, Button, Popconfirm } from 'antd';
import { SettingOutlined } from '@ant-design/icons';
import type { Module } from '../../../types';

const { Text, Title } = Typography;

interface ModuleCardProps {
  module: Module;
  onPercentageChange: (percentage: number) => void;
  loading?: boolean;
  children?: React.ReactNode;
}

/**
 * Modül kartı - yüzde slider ile
 */
export const ModuleCard: React.FC<ModuleCardProps> = ({
  module,
  onPercentageChange,
  loading = false,
  children,
}) => {
  const [pendingValue, setPendingValue] = React.useState<number | null>(null);

  // PascalCase / camelCase uyumluluğu
  const moduleName = module.name || module.Name || '';
  const moduleCode = module.code || module.Code || '';
  const moduleDescription = module.description || module.Description || '';
  const moduleEndpointCount = module.endpointCount ?? module.EndpointCount ?? 0;
  const moduleNewPercentage = module.newPercentage ?? module.NewPercentage ?? 0;

  const handleSliderChange = (value: number) => {
    setPendingValue(value);
  };

  const handleSliderAfterChange = (value: number) => {
    if (value !== moduleNewPercentage) {
      onPercentageChange(value);
    }
    setPendingValue(null);
  };

  const displayValue = pendingValue ?? moduleNewPercentage;

  const getTargetLabel = (percentage: number) => {
    if (percentage === 0) return { text: 'Eski Sistem', color: 'orange' };
    if (percentage === 100) return { text: 'Yeni Sistem', color: 'green' };
    return { text: 'Bölünmüş', color: 'blue' };
  };

  const targetLabel = getTargetLabel(displayValue);

  return (
    <Card
      loading={loading}
      style={{ marginBottom: 16 }}
      title={
        <Space>
          <SettingOutlined />
          <Title level={5} style={{ margin: 0 }}>
            {moduleName}
          </Title>
          <Tag>{moduleCode}</Tag>
        </Space>
      }
      extra={
        <Space>
          <Tag color={targetLabel.color}>{targetLabel.text}</Tag>
          <Text strong>%{displayValue}</Text>
        </Space>
      }
    >
      <div style={{ marginBottom: 16 }}>
        <Text type="secondary">{moduleDescription}</Text>
        <br />
        <Text type="secondary" style={{ fontSize: 12 }}>
          {moduleEndpointCount} endpoint
        </Text>
      </div>

      <div style={{ marginBottom: 16 }}>
        <Text strong style={{ display: 'block', marginBottom: 8 }}>
          Yeni Sisteme Yönlendirme Oranı
        </Text>
        <Slider
          value={displayValue}
          min={0}
          max={100}
          step={10}
          marks={{
            0: 'Eski',
            50: '%50',
            100: 'Yeni',
          }}
          onChange={handleSliderChange}
          onChangeComplete={handleSliderAfterChange}
          tooltip={{
            formatter: (value) => `%${value} Yeni Sistem`,
          }}
        />
      </div>

      <Space>
        <Popconfirm
          title="Tüm trafiği eski sisteme yönlendir?"
          onConfirm={() => onPercentageChange(0)}
          okText="Evet"
          cancelText="İptal"
        >
          <Button size="small" danger>
            Tümü Eski
          </Button>
        </Popconfirm>
        <Popconfirm
          title="Tüm trafiği Yeni Sisteme yönlendir?"
          onConfirm={() => onPercentageChange(100)}
          okText="Evet"
          cancelText="İptal"
        >
          <Button size="small" type="primary">
            Tümü Yeni
          </Button>
        </Popconfirm>
      </Space>

      {/* Endpoint listesi (accordion) */}
      {children}
    </Card>
  );
};
