import React, { useMemo } from 'react';
import { Collapse, Space, Tag, Typography, Switch, Tooltip, Card } from 'antd';
import { ApiOutlined, CloudServerOutlined, AppstoreOutlined } from '@ant-design/icons';
import { StatusDot } from '../../../components/ui';
import type { GatewayNode, NodeHealthStatus, Route } from '../../../types';

const { Text, Title } = Typography;

/**
 * Modül veri yapısı (Backend API'den gelen)
 */
interface ModuleRoute {
  key?: string;
  Key?: string;
  legacyPath?: string;
  LegacyPath?: string;
  newPath?: string;
  NewPath?: string;
  httpMethods?: string[];
  HttpMethods?: string[];
  UpstreamHttpMethod?: string[];
  upstreamPathTemplate?: string;
  UpstreamPathTemplate?: string;
  overridePercentage?: number | null;
  OverridePercentage?: number | null;
  NewSystemPercentage?: number | null;
}

interface Module {
  code?: string;
  Code?: string;
  name?: string;
  Name?: string;
  description?: string;
  Description?: string;
  endpointCount?: number;
  EndpointCount?: number;
  routes?: ModuleRoute[];
  Routes?: ModuleRoute[];
}

interface RouteNodeOverridesProps {
  modules: Module[];
  routes: Route[];
  legacyNodes: GatewayNode[];
  newNodes: GatewayNode[];
  healthStatus: {
    legacy: NodeHealthStatus[];
    new: NodeHealthStatus[];
  };
  onRouteNodeChange: (
    routeKey: string,
    target: 'Legacy' | 'New',
    nodeId: string,
    enabled: boolean
  ) => void;
}

/**
 * Modül bazlı sunucu özelleştirmeleri - Backend'den gelen modül verilerini kullanır
 */
export const RouteNodeOverrides: React.FC<RouteNodeOverridesProps> = ({
  modules = [],
  routes = [],
  legacyNodes = [],
  newNodes = [],
  healthStatus = { legacy: [], new: [] },
  onRouteNodeChange,
}) => {
  // Null/undefined kontrolü
  if (!modules || modules.length === 0) {
    return null;
  }

  // Route bazlı disabled nodes lookup map oluştur
  // Key: route path (UpstreamPathTemplate), Value: { legacy: string[], new: string[] }
  const disabledNodesMap = useMemo(() => {
    const map: Record<string, { legacy: string[]; new: string[] }> = {};
    
    for (const route of routes) {
      const routePath = route.upstreamPathTemplate || route.UpstreamPathTemplate || '';
      const routeKey = route.key || route.Key || routePath;
      
      if (routePath || routeKey) {
        const disabledNodes = route.disabledNodes || route.DisabledNodes || { legacy: [], new: [] };
        const legacy = disabledNodes.legacy || disabledNodes.Legacy || [];
        const newNodes = disabledNodes.new || disabledNodes.New || [];
        
        // Hem path hem de key ile eşleştir
        if (routePath) {
          map[routePath.toLowerCase()] = { legacy, new: newNodes };
        }
        if (routeKey && routeKey !== routePath) {
          map[routeKey.toLowerCase()] = { legacy, new: newNodes };
        }
      }
    }
    
    return map;
  }, [routes]);

  const getHealthForNode = (
    target: 'Legacy' | 'New',
    nodeId: string
  ): NodeHealthStatus | undefined => {
    const statusList = target === 'Legacy' ? (healthStatus?.legacy ?? []) : (healthStatus?.new ?? []);
    return statusList.find((h) => h.id === nodeId);
  };

  const isNodeGloballyEnabled = (target: 'Legacy' | 'New', nodeId: string): boolean => {
    const nodes = target === 'Legacy' ? (legacyNodes ?? []) : (newNodes ?? []);
    return nodes.find((n) => n.id === nodeId)?.enabled ?? false;
  };

  // Route bazlı disabled nodes kontrolü
  const isNodeDisabledForRoute = (routeKey: string, target: 'Legacy' | 'New', nodeId: string): boolean => {
    const key = routeKey.toLowerCase();
    const disabledNodes = disabledNodesMap[key];
    
    if (!disabledNodes) {
      return false;
    }
    
    const disabledList = target === 'Legacy' ? disabledNodes.legacy : disabledNodes.new;
    return disabledList.some((id) => id.toLowerCase() === nodeId.toLowerCase());
  };

  const getMethodColor = (method: string) => {
    const colors: Record<string, string> = {
      GET: 'green',
      POST: 'blue',
      PUT: 'orange',
      DELETE: 'red',
      PATCH: 'purple',
    };
    return colors[method] || 'default';
  };

  const renderNodeList = (
    routeKey: string,
    target: 'Legacy' | 'New',
    nodes: GatewayNode[]
  ) => {
    if (nodes.length === 0) {
      return <Text type="secondary">Sunucu tanımlı değil</Text>;
    }

    return (
      <Space direction="vertical" size="small" style={{ width: '100%' }}>
        {nodes.map((node) => {
          const health = getHealthForNode(target, node.id);
          const globalEnabled = isNodeGloballyEnabled(target, node.id);
          const isDisabled = isNodeDisabledForRoute(routeKey, target, node.id);
          const isEnabled = globalEnabled && !isDisabled;

          return (
            <div
              key={node.id}
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '4px 8px',
                background: '#fafafa',
                borderRadius: 4,
              }}
            >
              <Space>
                <StatusDot
                  status={health?.ready ?? false}
                  hint={health?.hint ?? 'Durum bilinmiyor'}
                  size={8}
                />
                <Text style={{ fontFamily: 'monospace', fontSize: 12 }}>
                  {node.id}
                </Text>
                {!globalEnabled && (
                  <Tag color="orange" style={{ fontSize: 10 }}>
                    Genel Pasif
                  </Tag>
                )}
                {globalEnabled && isDisabled && (
                  <Tag color="red" style={{ fontSize: 10 }}>
                    Bu Endpoint İçin Pasif
                  </Tag>
                )}
              </Space>
              <Tooltip
                title={
                  !globalEnabled
                    ? 'Sunucu genel olarak pasif'
                    : isEnabled
                    ? 'Bu endpoint için sunucu aktif - kapatmak için tıklayın'
                    : 'Bu endpoint için sunucu pasif - açmak için tıklayın'
                }
              >
                <Switch
                  size="small"
                  checked={isEnabled}
                  disabled={!globalEnabled}
                  onChange={(checked) => {
                    onRouteNodeChange(routeKey, target, node.id, checked);
                  }}
                />
              </Tooltip>
            </div>
          );
        })}
      </Space>
    );
  };

  // Her route için accordion item oluştur
  const createRouteItems = (moduleRoutes: ModuleRoute[]) => {
    return moduleRoutes.map((route) => {
      const routeKey = route.key || route.Key || route.legacyPath || route.upstreamPathTemplate || '';
      const routePath = route.legacyPath || route.LegacyPath || route.upstreamPathTemplate || route.UpstreamPathTemplate || '';
      const httpMethods = route.httpMethods || route.HttpMethods || route.UpstreamHttpMethod || [];

      return {
        key: routeKey,
        label: (
          <Space>
            <ApiOutlined />
            <Space size={4}>
              {httpMethods.map((method) => (
                <Tag key={method} color={getMethodColor(method)} style={{ margin: 0 }}>
                  {method}
                </Tag>
              ))}
            </Space>
            <Text code style={{ fontSize: 11 }}>
              {routePath}
            </Text>
          </Space>
        ),
        children: (
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <div>
              <Space style={{ marginBottom: 8 }}>
                <CloudServerOutlined />
                <Text strong>Eski Sistem Sunucuları</Text>
              </Space>
              {renderNodeList(routePath || routeKey, 'Legacy', legacyNodes)}
            </div>
            <div>
              <Space style={{ marginBottom: 8 }}>
                <CloudServerOutlined />
                <Text strong>Yeni Sistem Sunucuları</Text>
              </Space>
              {renderNodeList(routePath || routeKey, 'New', newNodes)}
            </div>
          </div>
        ),
      };
    });
  };

  // Toplam endpoint sayısı
  const totalEndpoints = modules.reduce((sum, m) => {
    const routes = m.routes || m.Routes || [];
    return sum + routes.length;
  }, 0);

  return (
    <div style={{ marginTop: 24 }}>
      <Title level={5} style={{ marginBottom: 16 }}>
        <Space>
          <ApiOutlined />
          Endpoint Bazlı Sunucu Özelleştirmeleri
          <Tag>{totalEndpoints} endpoint</Tag>
        </Space>
      </Title>

      {modules.map((module) => {
        const moduleCode = module.code || module.Code || '';
        const moduleName = module.name || module.Name || moduleCode;
        const moduleDesc = module.description || module.Description || '';
        const moduleRoutes = module.routes || module.Routes || [];

        if (moduleRoutes.length === 0) {
          return null;
        }

        return (
          <Card
            key={moduleCode}
            size="small"
            style={{ marginBottom: 16 }}
            title={
              <Space>
                <AppstoreOutlined />
                <Text strong>{moduleName}</Text>
                <Tag>{moduleCode}</Tag>
                <Tag color="blue">{moduleRoutes.length} endpoint</Tag>
              </Space>
            }
            extra={
              moduleDesc && (
                <Text type="secondary" style={{ fontSize: 12 }}>
                  {moduleDesc}
                </Text>
              )
            }
          >
            <Collapse
              items={createRouteItems(moduleRoutes)}
              size="small"
              ghost
            />
          </Card>
        );
      })}
    </div>
  );
};
