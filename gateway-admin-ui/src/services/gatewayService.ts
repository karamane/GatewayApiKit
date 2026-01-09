import { api } from './api';
import type {
  NodesResponse,
  NodeStatusResponse,
  SetNodeEnabledRequest,
  SetRouteNodeEnabledRequest,
  RoutesResponse,
  ModulesResponse,
  SetModulePercentageRequest,
  SystemHealthResponse,
  GatewayStatus,
  GatewayNode,
  NodeHealthStatus,
} from '../types';

// Re-export for hooks
export type { ModulesResponse };

// =================== NODES ===================

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function normalizeNode(node: any): GatewayNode {
  return {
    id: node.Id ?? node.id ?? '',
    baseUrl: node.BaseUrl ?? node.baseUrl ?? '',
    enabled: node.Enabled ?? node.enabled ?? false,
    weight: node.Weight ?? node.weight ?? 100,
  };
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function normalizeNodeHealth(node: any): NodeHealthStatus {
  return {
    id: node.Id ?? node.id ?? '',
    ready: node.Ready ?? node.ready ?? false,
    hint: node.Hint ?? node.hint ?? 'Durum bilinmiyor',
  };
}

/**
 * Tüm node'ları getir
 */
export async function getNodes(): Promise<NodesResponse> {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const response = await api.get<any>('/nodes');
  const data = response.data;
  
  return {
    legacy: (data.Legacy ?? data.legacy ?? []).map(normalizeNode),
    new: (data.New ?? data.new ?? []).map(normalizeNode),
  };
}

/**
 * Node sağlık durumlarını getir
 */
export async function getNodeStatus(): Promise<NodeStatusResponse> {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const response = await api.get<any>('/nodes/status');
  const data = response.data;
  
  return {
    legacy: (data.Legacy ?? data.legacy ?? []).map(normalizeNodeHealth),
    new: (data.New ?? data.new ?? []).map(normalizeNodeHealth),
  };
}

/**
 * Global node etkinleştir/devre dışı bırak
 * Gateway nodeId'den target'ı otomatik belirler
 */
export async function setNodeEnabled(
  _target: 'Legacy' | 'New',
  nodeId: string,
  enabled: boolean
): Promise<void> {
  await api.put<void, SetNodeEnabledRequest>(`/nodes/${nodeId}/enabled`, {
    enabled,
  });
}

// =================== ROUTES ===================

/**
 * Tüm route'ları getir
 * Gateway farklı case kullanabilir, normalize ederiz
 */
export async function getRoutes(): Promise<RoutesResponse> {
  const response = await api.get<RoutesResponse>('/routes');
  const data = response.data;
  
  // Normalize: Routes veya routes olabilir
  const rawRoutes = data.Routes ?? data.routes ?? [];
  
  // Her route'u normalize et
  const routes = rawRoutes.map(route => ({
    key: route.Key ?? route.key ?? '',
    upstreamPathTemplate: route.UpstreamPathTemplate ?? route.upstreamPathTemplate ?? '',
    httpMethods: route.UpstreamHttpMethod ?? route.httpMethods ?? [],
    legacyPath: route.legacyPath ?? '',
    newPath: route.newPath ?? '',
    overridePercentage: route.overridePercentage ?? null,
    disabledNodes: {
      legacy: route.DisabledNodes?.Legacy ?? route.DisabledNodes?.legacy ?? route.disabledNodes?.Legacy ?? route.disabledNodes?.legacy ?? [],
      new: route.DisabledNodes?.New ?? route.DisabledNodes?.new ?? route.disabledNodes?.New ?? route.disabledNodes?.new ?? [],
    },
  }));
  
  return { routes };
}

/**
 * Route bazlı node etkinleştir/devre dışı bırak
 */
export async function setRouteNodeEnabled(
  routeKey: string,
  target: 'Legacy' | 'New',
  nodeId: string,
  enabled: boolean
): Promise<void> {
  await api.put<void, SetRouteNodeEnabledRequest>('/routes/nodes', {
    routeKey,
    target,
    nodeId,
    enabled,
  });
}

// =================== MODULES ===================

/**
 * Tüm modülleri getir
 */
export async function getModules(): Promise<ModulesResponse> {
  const response = await api.get<ModulesResponse>('/modules');
  return response.data;
}

/**
 * Modül yüzdesini güncelle
 */
export async function setModulePercentage(
  moduleCode: string,
  percentage: number
): Promise<void> {
  await api.put<void, SetModulePercentageRequest>(`/modules/${moduleCode}/percentage`, {
    percentage,
  });
}

/**
 * Route override'ını güncelle
 * Gateway: PUT /routes/override { path, percentage }
 */
export async function setRouteOverride(
  _moduleCode: string,
  path: string,
  percentage: number | null
): Promise<void> {
  await api.put('/routes/override', { path, percentage });
}

/**
 * Tüm trafiği Legacy'ye çek (Emergency Rollback)
 */
export async function emergencyRollback(): Promise<void> {
  await api.post('/rollback');
}

// =================== HEALTH ===================

/**
 * Sistem sağlığını getir
 */
export async function getSystemHealth(): Promise<SystemHealthResponse> {
  const response = await api.get<SystemHealthResponse>('/system-health');
  return response.data;
}

/**
 * Gateway durumunu getir
 */
export async function getGatewayStatus(): Promise<GatewayStatus> {
  const response = await api.get<GatewayStatus>('/health');
  return response.data;
}

// =================== AUTH ===================

/**
 * API Key'i doğrula
 */
export async function validateApiKey(apiKey: string): Promise<{ valid: boolean; message?: string }> {
  try {
    const response = await api.get('/health', {
      headers: { 'X-Admin-Api-Key': apiKey },
    });
    return { valid: response.status === 200 };
  } catch {
    return { valid: false, message: 'Geçersiz API Key' };
  }
}
