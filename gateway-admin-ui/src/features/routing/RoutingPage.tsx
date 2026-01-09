import React, { useState, useMemo } from 'react';
import { Alert, Button, Popconfirm, Space, Spin, Input, Card, Row, Col } from 'antd';
import { WarningOutlined, SearchOutlined, ClearOutlined } from '@ant-design/icons';
import { PageHeader } from '../../components/ui';
import { ModuleCard } from './components/ModuleCard';
import { EndpointTable } from './components/EndpointTable';
import {
  useModules,
  useSetModulePercentage,
  useSetRouteOverride,
  useEmergencyRollback,
} from '../../hooks/useApi';
import type { Module, ModuleRoute } from '../../types';

/**
 * Routing Yönetimi sayfası
 */
export const RoutingPage: React.FC = () => {
  const { data, isLoading, refetch, isFetching } = useModules();
  const setModulePercentage = useSetModulePercentage();
  const setRouteOverride = useSetRouteOverride();
  const emergencyRollback = useEmergencyRollback();

  // Filtre state'leri
  const [moduleFilter, setModuleFilter] = useState('');
  const [endpointFilter, setEndpointFilter] = useState('');

  const handleModulePercentageChange = (moduleCode: string, percentage: number) => {
    setModulePercentage.mutate({ moduleCode, percentage });
  };

  const handleRouteOverride = (moduleCode: string, path: string, percentage: number | null) => {
    setRouteOverride.mutate({ moduleCode, path, percentage });
  };

  const handleEmergencyRollback = () => {
    emergencyRollback.mutate();
  };

  // Filtrelenmiş modüller
  const filteredModules = useMemo(() => {
    const modules = data?.moduller ?? [];
    
    return modules
      .filter((module) => {
        // Modül filtresi
        const moduleName = (module.name || module.Name || '').toLowerCase();
        const moduleCode = (module.code || module.Code || '').toLowerCase();
        const moduleSearch = moduleFilter.toLowerCase().trim();
        
        if (moduleSearch && !moduleName.includes(moduleSearch) && !moduleCode.includes(moduleSearch)) {
          return false;
        }
        
        return true;
      })
      .map((module) => {
        // Endpoint filtresi
        if (!endpointFilter.trim()) {
          return module;
        }
        
        const routes = module.routes || module.Routes || [];
        const endpointSearch = endpointFilter.toLowerCase().trim();
        
        const filteredRoutes = routes.filter((route: ModuleRoute) => {
          const path = (route.legacyPath || route.LegacyPath || route.UpstreamPathTemplate || '').toLowerCase();
          const key = (route.key || route.Key || '').toLowerCase();
          return path.includes(endpointSearch) || key.includes(endpointSearch);
        });
        
        // Eşleşen endpoint yoksa modülü gösterme
        if (filteredRoutes.length === 0) {
          return null;
        }
        
        return { ...module, routes: filteredRoutes, Routes: filteredRoutes };
      })
      .filter((m): m is Module => m !== null);
  }, [data?.moduller, moduleFilter, endpointFilter]);

  const clearFilters = () => {
    setModuleFilter('');
    setEndpointFilter('');
  };

  const hasActiveFilters = moduleFilter.trim() || endpointFilter.trim();

  if (isLoading) {
    return (
      <div style={{ textAlign: 'center', padding: 100 }}>
        <Spin size="large" />
      </div>
    );
  }

  const modules = data?.moduller ?? [];

  return (
    <div>
      <PageHeader
        title="Routing Yönetimi"
        subtitle="Modül ve endpoint bazlı trafik yönlendirmelerini yönetin"
        onRefresh={() => refetch()}
        refreshing={isFetching}
        extra={
          <Popconfirm
            title="Acil Durum Geri Alma"
            description="Tüm trafik eski sisteme yönlendirilecek. Emin misiniz?"
            onConfirm={handleEmergencyRollback}
            okText="Evet, Geri Al"
            cancelText="İptal"
            okButtonProps={{ danger: true }}
          >
            <Button danger icon={<WarningOutlined />}>
              Acil Geri Alma
            </Button>
          </Popconfirm>
        }
      />

      {/* Bilgi mesajı */}
      <Alert
        type="info"
        showIcon
        message="Trafik Yönlendirme"
        description={
          <Space direction="vertical" size="small">
            <span>
              Her modül için yeni sisteme gidecek trafik yüzdesini ayarlayabilirsiniz.
            </span>
            <span>
              Endpoint bazlı override'lar modül ayarından bağımsız çalışır.
            </span>
          </Space>
        }
        style={{ marginBottom: 24 }}
      />

      {/* Filtreler */}
      <Card size="small" style={{ marginBottom: 24 }}>
        <Row gutter={16} align="middle">
          <Col flex="1">
            <Input
              placeholder="Modül adı veya kodu ile filtrele..."
              prefix={<SearchOutlined />}
              value={moduleFilter}
              onChange={(e) => setModuleFilter(e.target.value)}
              allowClear
            />
          </Col>
          <Col flex="1">
            <Input
              placeholder="Endpoint path ile filtrele..."
              prefix={<SearchOutlined />}
              value={endpointFilter}
              onChange={(e) => setEndpointFilter(e.target.value)}
              allowClear
            />
          </Col>
          <Col>
            <Button
              icon={<ClearOutlined />}
              onClick={clearFilters}
              disabled={!hasActiveFilters}
            >
              Temizle
            </Button>
          </Col>
        </Row>
      </Card>

      {/* Özet */}
      {data && (
        <Alert
          type="success"
          message={
            hasActiveFilters
              ? `${filteredModules.length} / ${data.toplamModul} modül gösteriliyor (filtre aktif)`
              : `${data.toplamModul} modül, ${data.toplamEndpoint} endpoint`
          }
          style={{ marginBottom: 24 }}
        />
      )}

      {/* Modül kartları */}
      {filteredModules.map((module) => {
        const moduleCode = module.code || module.Code || '';
        const moduleRoutes = module.routes || module.Routes || [];
        const moduleNewPercentage = module.newPercentage ?? module.NewPercentage ?? 0;
        
        return (
          <ModuleCard
            key={moduleCode}
            module={module}
            onPercentageChange={(percentage) =>
              handleModulePercentageChange(moduleCode, percentage)
            }
            loading={setModulePercentage.isPending}
          >
            <EndpointTable
              routes={moduleRoutes}
              modulePercentage={moduleNewPercentage}
              onOverride={(path, percentage) =>
                handleRouteOverride(moduleCode, path, percentage)
              }
              loading={setRouteOverride.isPending}
            />
          </ModuleCard>
        );
      })}

      {filteredModules.length === 0 && modules.length > 0 && (
        <Alert
          type="info"
          message="Sonuç bulunamadı"
          description="Filtre kriterlerine uygun modül veya endpoint bulunamadı."
        />
      )}

      {modules.length === 0 && (
        <Alert
          type="warning"
          message="Modül bulunamadı"
          description="Yapılandırmada tanımlı modül yok."
        />
      )}
    </div>
  );
};
