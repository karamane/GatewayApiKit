import React from 'react';
import { Typography, Space, InputNumber, Tag, Tooltip } from 'antd';
import { WarningOutlined } from '@ant-design/icons';
import { DataTable, StatusDot, NodeToggle } from '../../../components/ui';
import type { GatewayNode, NodeHealthStatus } from '../../../types';
import type { ColumnsType } from 'antd/es/table';

const { Text } = Typography;

interface NodeTableProps {
  title: string;
  nodes: GatewayNode[];
  healthStatus: NodeHealthStatus[];
  onNodeEnabledChange: (nodeId: string, enabled: boolean) => void;
  loading?: boolean;
}

/**
 * Node tablosu - global enable/disable ile
 */
export const NodeTable: React.FC<NodeTableProps> = ({
  title,
  nodes,
  healthStatus,
  onNodeEnabledChange,
  loading = false,
}) => {
  const getHealthForNode = (nodeId: string): NodeHealthStatus | undefined => {
    return healthStatus.find((h) => h.id === nodeId);
  };

  const columns: ColumnsType<GatewayNode> = [
    {
      title: 'Durum',
      key: 'health',
      width: 60,
      align: 'center',
      render: (_, record) => {
        const health = getHealthForNode(record.id);
        return (
          <StatusDot
            status={health?.ready ?? false}
            hint={health?.hint ?? 'Durum bilinmiyor'}
          />
        );
      },
    },
    {
      title: 'Sunucu Kimliği',
      dataIndex: 'id',
      key: 'id',
      render: (id: string) => (
        <Text strong style={{ fontFamily: 'monospace' }}>
          {id}
        </Text>
      ),
    },
    {
      title: 'Sunucu Adresi',
      dataIndex: 'baseUrl',
      key: 'baseUrl',
      render: (url: string) => (
        <Text code style={{ fontSize: 12 }}>
          {url}
        </Text>
      ),
    },
    {
      title: 'Ağırlık',
      dataIndex: 'weight',
      key: 'weight',
      width: 100,
      align: 'center',
      render: (weight: number) => (
        <InputNumber
          value={weight}
          min={1}
          max={1000}
          size="small"
          disabled
          style={{ width: 60 }}
        />
      ),
    },
    {
      title: 'Aktif',
      key: 'enabled',
      width: 180,
      align: 'center',
      render: (_, record) => {
        const health = getHealthForNode(record.id);
        const isActiveButUnhealthy = record.enabled && !(health?.ready ?? false);

        return (
          <Space>
            <NodeToggle
              checked={record.enabled}
              onChange={(enabled) => onNodeEnabledChange(record.id, enabled)}
            />
            {isActiveButUnhealthy && (
              <Tooltip title="Bu sunucu aktif ancak sağlıksız! Trafiğe dahil edilmeyecek.">
                <Tag color="warning" icon={<WarningOutlined />} style={{ marginLeft: 4 }}>
                  Sağlıksız
                </Tag>
              </Tooltip>
            )}
          </Space>
        );
      },
    },
  ];

  return (
    <div style={{ marginBottom: 24 }}>
      <Space style={{ marginBottom: 12 }}>
        <Typography.Title level={5} style={{ margin: 0 }}>
          {title}
        </Typography.Title>
        <Tag>{nodes.length} node</Tag>
      </Space>
      <DataTable<GatewayNode>
        dataSource={nodes}
        columns={columns}
        rowKey="id"
        loading={loading}
        pagination={false}
        size="middle"
        emptyText="Bu sistem için tanımlı sunucu yok"
      />
    </div>
  );
};
