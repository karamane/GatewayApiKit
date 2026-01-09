import React, { useState, useMemo } from 'react';
import { Alert, Spin, Card, Space, Typography, Input, Row, Col, Button } from 'antd';
import { CloudServerOutlined, SearchOutlined, ClearOutlined } from '@ant-design/icons';
import { PageHeader } from '../../components/ui';
import { NodeTable } from './components/NodeTable';
import { RouteNodeOverrides } from './components/RouteNodeOverrides';
import {
  useNodes,
  useNodeStatus,
  useModules,
  useRoutes,
  useSetNodeEnabled,
  useSetRouteNodeEnabled,
} from '../../hooks/useApi';

const { Text } = Typography;

/**
 * Yük Dengeleme Yönetimi sayfası
 */
export const LoadBalancingPage: React.FC = () => {
  const { data: nodesData, isLoading: nodesLoading, refetch: refetchNodes, isFetching: nodesFetching } = useNodes();
  const { data: healthData, isLoading: healthLoading } = useNodeStatus();
  const { data: modulesData, isLoading: modulesLoading, refetch: refetchModules, isFetching: modulesFetching } = useModules();
  const { data: routesData, isLoading: routesLoading, refetch: refetchRoutes } = useRoutes();
  
  const setNodeEnabled = useSetNodeEnabled();
  const setRouteNodeEnabled = useSetRouteNodeEnabled();

  // Filtre state'leri
  const [moduleFilter, setModuleFilter] = useState('');
  const [endpointFilter, setEndpointFilter] = useState('');

  const handleRefresh = () => {
    refetchNodes();
    refetchModules();
    refetchRoutes();
  };

  const handleNodeEnabledChange = (target: 'Legacy' | 'New', nodeId: string, enabled: boolean) => {
    setNodeEnabled.mutate({ target, nodeId, enabled });
  };

  const handleRouteNodeChange = (
    routeKey: string,
    target: 'Legacy' | 'New',
    nodeId: string,
    enabled: boolean
  ) => {
    setRouteNodeEnabled.mutate({ routeKey, target, nodeId, enabled });
  };

  const isLoading = nodesLoading || healthLoading || modulesLoading || routesLoading;
  const isFetching = nodesFetching || modulesFetching;

  // Filtrelenmiş modüller (Routing sayfasındaki modül listesini kullan)
  const filteredModules = useMemo(() => {
    const modules = modulesData?.moduller ?? [];
    const moduleSearch = moduleFilter.toLowerCase().trim();
    const endpointSearch = endpointFilter.toLowerCase().trim();

    return modules
      .filter((module) => {
        const moduleName = (module.name || module.Name || '').toLowerCase();
        const moduleCode = (module.code || module.Code || '').toLowerCase();
        
        // Modül filtresi
        if (moduleSearch && !moduleName.includes(moduleSearch) && !moduleCode.includes(moduleSearch)) {
          return false;
        }
        
        return true;
      })
      .map((module) => {
        // Endpoint filtresi
        if (!endpointSearch) {
          return module;
        }
        
        const routes = module.routes || module.Routes || [];
        const filteredRoutes = routes.filter((route: { legacyPath?: string; LegacyPath?: string; upstreamPathTemplate?: string }) => {
          const path = (route.legacyPath || route.LegacyPath || route.upstreamPathTemplate || '').toLowerCase();
          return path.includes(endpointSearch);
        });
        
        if (filteredRoutes.length === 0) {
          return null;
        }
        
        return { ...module, routes: filteredRoutes };
      })
      .filter((m) => m !== null);
  }, [modulesData?.moduller, moduleFilter, endpointFilter]);

  // Toplam endpoint sayısı
  const totalEndpoints = useMemo(() => {
    return filteredModules.reduce((sum, m) => sum + (m?.routes?.length || 0), 0);
  }, [filteredModules]);

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

  const legacyNodes = nodesData?.legacy ?? [];
  const newNodes = nodesData?.new ?? [];
  const legacyHealth = healthData?.legacy ?? [];
  const newHealth = healthData?.new ?? [];

  const totalNodes = legacyNodes.length + newNodes.length;
  const activeNodes = [...legacyNodes, ...newNodes].filter((n) => n.enabled).length;
  const healthyNodes = [...legacyHealth, ...newHealth].filter((h) => h.ready).length;

  return (
    <div>
      <PageHeader
        title="Yük Dengeleme Yönetimi"
        subtitle="Sunucuları ve endpoint bazlı trafik dağılımını yönetin"
        onRefresh={handleRefresh}
        refreshing={isFetching}
      />

      {/* Özet */}
      <Card style={{ marginBottom: 24 }}>
        <Space size="large">
          <div style={{ textAlign: 'center' }}>
            <CloudServerOutlined style={{ fontSize: 32, color: '#3b82f6' }} />
            <div>
              <Text strong style={{ fontSize: 24 }}>{totalNodes}</Text>
              <br />
              <Text type="secondary">Toplam Sunucu</Text>
            </div>
          </div>
          <div style={{ textAlign: 'center' }}>
            <Text strong style={{ fontSize: 24, color: '#059669' }}>{activeNodes}</Text>
            <br />
            <Text type="secondary">Aktif Sunucu</Text>
          </div>
          <div style={{ textAlign: 'center' }}>
            <Text strong style={{ fontSize: 24, color: healthyNodes === totalNodes ? '#059669' : '#dc2626' }}>
              {healthyNodes}
            </Text>
            <br />
            <Text type="secondary">Sağlıklı Sunucu</Text>
          </div>
        </Space>
      </Card>

      {/* Bilgi mesajı */}
      <Alert
        type="info"
        showIcon
        message="Sunucu Yönetimi"
        description={
          <Space direction="vertical" size="small">
            <span>
              Global olarak pasif edilen sunuculara hiçbir trafik yönlendirilmez.
            </span>
            <span>
              Endpoint bazlı özelleştirmeler, sadece global olarak aktif sunucular için geçerlidir.
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
        {hasActiveFilters && (
          <div style={{ marginTop: 8 }}>
            <Text type="secondary">
              {filteredModules.length} modül, {totalEndpoints} endpoint gösteriliyor
            </Text>
          </div>
        )}
      </Card>

      {/* Eski Sistem Sunucuları */}
      <NodeTable
        title="Eski Sistem Sunucuları"
        nodes={legacyNodes}
        healthStatus={legacyHealth}
        onNodeEnabledChange={(nodeId, enabled) =>
          handleNodeEnabledChange('Legacy', nodeId, enabled)
        }
        loading={setNodeEnabled.isPending}
      />

      {/* Yeni Sistem Sunucuları */}
      <NodeTable
        title="Yeni Sistem Sunucuları"
        nodes={newNodes}
        healthStatus={newHealth}
        onNodeEnabledChange={(nodeId, enabled) =>
          handleNodeEnabledChange('New', nodeId, enabled)
        }
        loading={setNodeEnabled.isPending}
      />

      {/* Modül bazlı sunucu özelleştirmeleri */}
      {filteredModules.length > 0 && (
        <RouteNodeOverrides
          modules={filteredModules}
          routes={routesData?.routes ?? []}
          legacyNodes={legacyNodes}
          newNodes={newNodes}
          healthStatus={{ legacy: legacyHealth, new: newHealth }}
          onRouteNodeChange={handleRouteNodeChange}
        />
      )}

      {filteredModules.length === 0 && (modulesData?.moduller?.length ?? 0) > 0 && (
        <Alert
          type="info"
          message="Sonuç bulunamadı"
          description="Filtre kriterlerine uygun endpoint bulunamadı."
          style={{ marginTop: 24 }}
        />
      )}

      {(modulesData?.moduller?.length ?? 0) === 0 && (
        <Alert
          type="warning"
          message="Modül bulunamadı"
          description="Yapılandırmada tanımlı modül yok."
        />
      )}
    </div>
  );
};
