import React from 'react';
import { Card, Row, Col, Statistic, Typography, Space } from 'antd';
import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  WarningOutlined,
  ClockCircleOutlined,
} from '@ant-design/icons';
import type { SystemHealthSummary as SummaryType } from '../../../types';

const { Text } = Typography;

interface HealthSummaryProps {
  summary: SummaryType;
}

/**
 * Sistem sağlığı özet kartları
 */
export const HealthSummary: React.FC<HealthSummaryProps> = ({ summary }) => {
  const formatDate = (dateString: string) => {
    try {
      const date = new Date(dateString);
      return date.toLocaleString('tr-TR');
    } catch {
      return dateString;
    }
  };

  return (
    <div style={{ marginBottom: 24 }}>
      <Row gutter={16}>
        {/* Toplam Servis */}
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="Toplam Servis"
              value={summary.toplamServis}
              prefix={<ClockCircleOutlined style={{ color: '#3b82f6' }} />}
              valueStyle={{ color: '#3b82f6' }}
            />
          </Card>
        </Col>

        {/* Çevrimiçi */}
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="Çevrimiçi"
              value={summary.cevrimiciServis}
              prefix={<CheckCircleOutlined style={{ color: '#059669' }} />}
              valueStyle={{ color: '#059669' }}
            />
          </Card>
        </Col>

        {/* Çevrimdışı */}
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="Çevrimdışı"
              value={summary.cevrimdisiServis}
              prefix={<CloseCircleOutlined style={{ color: summary.cevrimdisiServis > 0 ? '#dc2626' : '#6b7280' }} />}
              valueStyle={{ color: summary.cevrimdisiServis > 0 ? '#dc2626' : '#6b7280' }}
            />
          </Card>
        </Col>

        {/* Kritik */}
        <Col xs={24} sm={12} md={6}>
          <Card style={{ borderColor: summary.kritikAlarmVar ? '#dc2626' : undefined }}>
            <Statistic
              title="Kritik Alarm"
              value={summary.kritikAlarmVar ? 'EVET' : 'Yok'}
              prefix={
                <WarningOutlined
                  style={{
                    color: summary.kritikAlarmVar ? '#dc2626' : '#059669',
                  }}
                />
              }
              valueStyle={{
                color: summary.kritikAlarmVar ? '#dc2626' : '#059669',
              }}
            />
          </Card>
        </Col>
      </Row>

      {/* Son güncelleme */}
      <div style={{ marginTop: 12, textAlign: 'right' }}>
        <Space>
          <ClockCircleOutlined />
          <Text type="secondary" style={{ fontSize: 12 }}>
            Son güncelleme: {formatDate(summary.sonGuncelleme)}
          </Text>
        </Space>
      </div>
    </div>
  );
};
