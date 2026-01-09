import React from 'react';
import { Typography, Tag, Space, Tooltip } from 'antd';
import { ClockCircleOutlined, ExclamationCircleOutlined } from '@ant-design/icons';
import { DataTable, StatusDot } from '../../../components/ui';
import type { ServiceHealth } from '../../../types';
import type { ColumnsType } from 'antd/es/table';

const { Text } = Typography;

interface ServiceTableProps {
  title: string;
  services: ServiceHealth[];
  loading?: boolean;
}

/**
 * Servis sağlık durumu tablosu
 */
export const ServiceTable: React.FC<ServiceTableProps> = ({
  title,
  services,
  loading = false,
}) => {
  const formatDate = (dateString: string | null) => {
    if (!dateString) return '-';
    try {
      const date = new Date(dateString);
      return date.toLocaleString('tr-TR');
    } catch {
      return dateString;
    }
  };

  const getStatusColor = (status: string | null) => {
    if (!status) return 'default';
    const lowered = status.toLowerCase();
    if (lowered === 'online' || lowered === 'çalışıyor') return 'success';
    if (lowered === 'offline' || lowered === 'çevrimdışı') return 'error';
    if (lowered.includes('hata') || lowered.includes('error')) return 'error';
    if (lowered.includes('engel') || lowered.includes('bulunamadı')) return 'warning';
    return 'default';
  };

  const columns: ColumnsType<ServiceHealth> = [
    {
      title: 'Durum',
      key: 'online',
      width: 60,
      align: 'center',
      render: (_, record) => (
        <StatusDot
          status={record.cevrimici}
          hint={record.cevrimici ? 'Sunucu hazır - trafik yönlendirilebilir' : record.hataMesaji || 'Sunucu erişilemiyor'}
          pulse={record.kritik}
        />
      ),
    },
    {
      title: 'Servis ID',
      dataIndex: 'servisId',
      key: 'servisId',
      render: (id: string, record) => (
        <Space>
          <Text strong style={{ fontFamily: 'monospace' }}>
            {id}
          </Text>
          {record.kritik && (
            <Tooltip title="Kritik durum - müdahale gerekiyor">
              <ExclamationCircleOutlined style={{ color: '#dc2626' }} />
            </Tooltip>
          )}
        </Space>
      ),
    },
    {
      title: 'Adres',
      dataIndex: 'baseUrl',
      key: 'baseUrl',
      render: (url: string) => (
        <Text code style={{ fontSize: 11 }}>
          {url}
        </Text>
      ),
    },
    {
      title: 'Durum',
      dataIndex: 'durum',
      key: 'status',
      width: 150,
      render: (status: string | null) => (
        <Tag color={getStatusColor(status)}>{status || '-'}</Tag>
      ),
    },
    {
      title: 'Uygulama',
      key: 'app',
      width: 150,
      render: (_, record) => (
        <Space direction="vertical" size={0}>
          {record.uygulamaKimligi && (
            <Text style={{ fontSize: 12 }}>{record.uygulamaKimligi}</Text>
          )}
          {record.versiyon && (
            <Text type="secondary" style={{ fontSize: 11 }}>
              v{record.versiyon}
            </Text>
          )}
          {!record.uygulamaKimligi && !record.versiyon && (
            <Text type="secondary">-</Text>
          )}
        </Space>
      ),
    },
    {
      title: 'Yanıt Süresi',
      dataIndex: 'yanitSuresiMs',
      key: 'responseTime',
      width: 100,
      align: 'right',
      render: (ms: number | null) =>
        ms !== null ? (
          <Text type={ms > 1000 ? 'danger' : ms > 500 ? 'warning' : undefined}>
            {ms} ms
          </Text>
        ) : (
          '-'
        ),
    },
    {
      title: 'Son Kontrol',
      dataIndex: 'sonKontrol',
      key: 'lastCheck',
      width: 160,
      render: (date: string) => (
        <Space>
          <ClockCircleOutlined />
          <Text style={{ fontSize: 11 }}>{formatDate(date)}</Text>
        </Space>
      ),
    },
  ];

  return (
    <div style={{ marginBottom: 24 }}>
      <Space style={{ marginBottom: 12 }}>
        <Typography.Title level={5} style={{ margin: 0 }}>
          {title}
        </Typography.Title>
        <Tag>{services.length} servis</Tag>
      </Space>
      <DataTable<ServiceHealth>
        dataSource={services}
        columns={columns}
        rowKey="servisId"
        loading={loading}
        pagination={false}
        size="middle"
        emptyText="Bu sistem için servis bulunamadı"
        rowClassName={(record) =>
          record.kritik ? 'critical-row' : ''
        }
      />
      <style>
        {`
          .critical-row {
            background-color: rgba(220, 38, 38, 0.05) !important;
          }
          .critical-row:hover td {
            background-color: rgba(220, 38, 38, 0.1) !important;
          }
        `}
      </style>
    </div>
  );
};
