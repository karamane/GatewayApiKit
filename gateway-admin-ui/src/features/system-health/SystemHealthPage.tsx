import React from 'react';
import { Alert, Spin, Space } from 'antd';
import { PageHeader } from '../../components/ui';
import { HealthSummary } from './components/HealthSummary';
import { ServiceTable } from './components/ServiceTable';
import { CriticalAlerts } from './components/CriticalAlerts';
import { useSystemHealth } from '../../hooks/useApi';

/**
 * Sistem Sağlığı sayfası
 */
export const SystemHealthPage: React.FC = () => {
  const { data, isLoading, refetch, isFetching } = useSystemHealth();

  if (isLoading) {
    return (
      <div style={{ textAlign: 'center', padding: 100 }}>
        <Spin size="large" />
      </div>
    );
  }

  if (!data) {
    return (
      <Alert
        type="error"
        message="Veri alınamadı"
        description="Sistem sağlığı verileri yüklenemedi. Lütfen sayfayı yenileyin."
      />
    );
  }

  return (
    <div>
      <PageHeader
        title="Sistem Sağlığı"
        subtitle="Downstream servislerin anlık durumunu izleyin"
        onRefresh={() => refetch()}
        refreshing={isFetching}
      />

      {/* Kritik alarmlar */}
      <CriticalAlerts services={data.kritikServisler} />

      {/* Bilgi mesajı */}
      <Alert
        type="info"
        showIcon
        message="Sağlık Kontrolü"
        description={
          <Space direction="vertical" size="small">
            <span>
              Gateway, tüm downstream servisleri düzenli aralıklarla (1 dakika) kontrol eder.
            </span>
            <span>
              Ardışık 3 veya daha fazla başarısız kontrol sonucunda servis "kritik" olarak işaretlenir.
            </span>
          </Space>
        }
        style={{ marginBottom: 24 }}
      />

      {/* Özet */}
      <HealthSummary summary={data.ozet} />

      {/* Legacy servisleri */}
      <ServiceTable
        title="Legacy Sistemi"
        services={data.legacy}
      />

      {/* Yeni sistem servisleri */}
      <ServiceTable
        title="Yeni Sistem"
        services={data.yeni}
      />
    </div>
  );
};
