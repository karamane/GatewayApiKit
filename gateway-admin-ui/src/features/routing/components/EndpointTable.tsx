import React, { useState } from 'react';
import { Collapse, Tag, Button, Space, Tooltip, Typography, Slider, InputNumber, Modal } from 'antd';
import { ApiOutlined, CloseOutlined, EditOutlined } from '@ant-design/icons';
import type { ModuleRoute } from '../../../types';

const { Text } = Typography;

interface EndpointTableProps {
  routes: ModuleRoute[];
  modulePercentage: number;
  onOverride: (path: string, percentage: number | null) => void;
  loading?: boolean;
}

/**
 * Endpoint listesi - accordion içinde
 */
export const EndpointTable: React.FC<EndpointTableProps> = ({
  routes = [],
  modulePercentage,
  onOverride,
  loading = false,
}) => {
  const [editingRoute, setEditingRoute] = useState<string | null>(null);
  const [editValue, setEditValue] = useState<number>(0);

  if (!routes || routes.length === 0) {
    return null;
  }

  const getEffectivePercentage = (route: ModuleRoute): number => {
    // Gateway'den gelen override değerini veya modül yüzdesini kullan
    const override = route.overridePercentage ?? route.OverridePercentage ?? route.NewSystemPercentage;
    const raw = override ?? modulePercentage;
    // Değeri number'a çevir ve 0-100 arasında sınırla
    const numValue = typeof raw === 'string' ? parseInt(raw, 10) : (raw ?? 0);
    return Math.max(0, Math.min(100, isNaN(numValue) ? 0 : numValue));
  };

  // Route path'i al (PascalCase veya camelCase)
  const getRoutePath = (route: ModuleRoute) => {
    return route.legacyPath || route.LegacyPath || route.UpstreamPathTemplate || '';
  };

  // HTTP metotlarını al
  const getHttpMethods = (route: ModuleRoute) => {
    return route.httpMethods || route.HttpMethods || route.UpstreamHttpMethod || [];
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

  const handleEditClick = (routePath: string, currentValue: number) => {
    setEditingRoute(routePath);
    setEditValue(currentValue);
  };

  const handleEditConfirm = () => {
    if (editingRoute) {
      onOverride(editingRoute, editValue);
      setEditingRoute(null);
    }
  };

  const handleEditCancel = () => {
    setEditingRoute(null);
  };

  const items = [
    {
      key: 'endpoints',
      label: (
        <Space>
          <ApiOutlined />
          <span>Endpoint Özelleştirmeleri</span>
          <Tag>{routes.length} endpoint</Tag>
        </Space>
      ),
      children: (
        <div>
          {routes.map((route) => {
            const routePath = getRoutePath(route);
            const httpMethods = getHttpMethods(route);
            const routeKey = route.key || route.Key || routePath;
            const effective = getEffectivePercentage(route);
            const overrideVal = route.overridePercentage ?? route.OverridePercentage ?? route.NewSystemPercentage;
            const hasOverride = overrideVal !== null && overrideVal !== undefined;

            return (
              <div
                key={routeKey}
                style={{
                  padding: '12px 0',
                  borderBottom: '1px solid #f0f0f0',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                }}
              >
                <div style={{ flex: 1 }}>
                  <Space wrap>
                    {httpMethods.map((method) => (
                      <Tag key={method} color={getMethodColor(method)}>
                        {method}
                      </Tag>
                    ))}
                    <Text code style={{ fontSize: 12 }}>
                      {routePath}
                    </Text>
                  </Space>
                  {hasOverride && (
                    <div style={{ marginTop: 4 }}>
                      <Tag color="purple">
                        Override: %{overrideVal}
                      </Tag>
                    </div>
                  )}
                </div>

                <Space>
                  {/* Mevcut durum göstergesi */}
                  <Tag 
                    color={effective <= 0 ? 'orange' : effective >= 100 ? 'green' : 'blue'}
                    style={{ minWidth: 100, textAlign: 'center' }}
                  >
                    {effective <= 0 ? '🔸 Eski Sistem' : effective >= 100 ? '🔹 Yeni Sistem' : `⚡ Bölünmüş %${effective}`}
                  </Tag>

                  {/* Yüzde düzenleme butonu */}
                  <Tooltip title="Özel oran belirlemek için tıklayın">
                    <Button
                      type="text"
                      size="small"
                      onClick={() => handleEditClick(routePath, effective)}
                      disabled={loading}
                      icon={<EditOutlined />}
                    >
                      %{effective}
                    </Button>
                  </Tooltip>

                  {/* Eski sisteme geç butonu - sadece yeni sistemde veya bölünmüşken aktif */}
                  <Tooltip title="Tümünü Eski Sisteme yönlendir">
                    <Button
                      size="small"
                      disabled={effective <= 0 || loading}
                      onClick={() => onOverride(routePath, 0)}
                      style={{
                        backgroundColor: effective <= 0 ? '#fff7e6' : undefined,
                        borderColor: effective <= 0 ? '#ffa940' : undefined,
                        color: effective <= 0 ? '#d46b08' : undefined,
                      }}
                    >
                      Eski
                    </Button>
                  </Tooltip>

                  {/* Yeni sisteme geç butonu - sadece eski sistemde veya bölünmüşken aktif */}
                  <Tooltip title="Tümünü Yeni Sisteme yönlendir">
                    <Button
                      size="small"
                      type={effective >= 100 ? 'primary' : 'default'}
                      disabled={effective >= 100 || loading}
                      onClick={() => onOverride(routePath, 100)}
                    >
                      Yeni
                    </Button>
                  </Tooltip>

                  {hasOverride && (
                    <Tooltip title="Override'ı kaldır, modül oranına döndür">
                      <Button 
                        size="small" 
                        icon={<CloseOutlined />}
                        disabled={loading}
                        onClick={() => onOverride(routePath, null)}
                      >
                        Sıfırla
                      </Button>
                    </Tooltip>
                  )}
                </Space>
              </div>
            );
          })}
        </div>
      ),
    },
  ];

  return (
    <>
      <Collapse
        items={items}
        ghost
        style={{ marginTop: 16 }}
      />

      {/* Özel yüzde girişi için modal */}
      <Modal
        title="Endpoint Override Oranı"
        open={editingRoute !== null}
        onOk={handleEditConfirm}
        onCancel={handleEditCancel}
        okText="Kaydet"
        cancelText="İptal"
        confirmLoading={loading}
      >
        <div style={{ padding: '20px 0' }}>
          <Text strong style={{ display: 'block', marginBottom: 16 }}>
            Yeni sisteme yönlendirilecek trafik yüzdesi:
          </Text>
          
          <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
            <Slider
              value={editValue}
              min={0}
              max={100}
              step={5}
              onChange={(value) => {
                // Değeri 0-100 arasında sınırla
                const clamped = Math.max(0, Math.min(100, value));
                setEditValue(clamped);
              }}
              style={{ flex: 1 }}
              marks={{
                0: 'Eski',
                50: '%50',
                100: 'Yeni',
              }}
            />
            <InputNumber
              value={editValue}
              min={0}
              max={100}
              onChange={(value) => {
                // Değeri 0-100 arasında sınırla
                const clamped = Math.max(0, Math.min(100, value ?? 0));
                setEditValue(clamped);
              }}
              formatter={(value) => `${value}%`}
              parser={(value) => {
                const parsed = parseInt(value?.replace('%', '') || '0', 10);
                // Değeri 0-100 arasında sınırla
                return Math.max(0, Math.min(100, parsed));
              }}
              style={{ width: 80 }}
            />
          </div>

          <div style={{ marginTop: 16 }}>
            <Space>
              <Button size="small" onClick={() => setEditValue(0)}>%0 (Eski)</Button>
              <Button size="small" onClick={() => setEditValue(25)}>%25</Button>
              <Button size="small" onClick={() => setEditValue(50)}>%50</Button>
              <Button size="small" onClick={() => setEditValue(75)}>%75</Button>
              <Button size="small" onClick={() => setEditValue(100)}>%100 (Yeni)</Button>
            </Space>
          </div>

          <Text type="secondary" style={{ display: 'block', marginTop: 16, fontSize: 12 }}>
            Endpoint: {editingRoute}
          </Text>
        </div>
      </Modal>
    </>
  );
};
