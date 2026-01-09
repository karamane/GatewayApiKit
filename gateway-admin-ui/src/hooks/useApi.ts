import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { message } from 'antd';
import * as gatewayService from '../services/gatewayService';
import { getErrorMessage } from '../services/api';

// Query keys
export const queryKeys = {
  nodes: ['nodes'] as const,
  nodeStatus: ['nodeStatus'] as const,
  routes: ['routes'] as const,
  modules: ['modules'] as const,
  systemHealth: ['systemHealth'] as const,
  gatewayStatus: ['gatewayStatus'] as const,
};

// =================== NODES HOOKS ===================

export function useNodes() {
  return useQuery({
    queryKey: queryKeys.nodes,
    queryFn: gatewayService.getNodes,
  });
}

export function useNodeStatus() {
  return useQuery({
    queryKey: queryKeys.nodeStatus,
    queryFn: gatewayService.getNodeStatus,
    refetchInterval: 30000, // Her 30 saniyede yenile
  });
}

export function useSetNodeEnabled() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      target,
      nodeId,
      enabled,
    }: {
      target: 'Legacy' | 'New';
      nodeId: string;
      enabled: boolean;
    }) => gatewayService.setNodeEnabled(target, nodeId, enabled),
    onSuccess: (_, variables) => {
      message.success(`${variables.nodeId} ${variables.enabled ? 'aktif' : 'pasif'} edildi`);
      queryClient.invalidateQueries({ queryKey: queryKeys.nodes });
      queryClient.invalidateQueries({ queryKey: queryKeys.nodeStatus });
    },
    onError: (error) => {
      message.error(getErrorMessage(error));
    },
  });
}

// =================== ROUTES HOOKS ===================

export function useRoutes() {
  return useQuery({
    queryKey: queryKeys.routes,
    queryFn: gatewayService.getRoutes,
  });
}

export function useSetRouteNodeEnabled() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      routeKey,
      target,
      nodeId,
      enabled,
    }: {
      routeKey: string;
      target: 'Legacy' | 'New';
      nodeId: string;
      enabled: boolean;
    }) => gatewayService.setRouteNodeEnabled(routeKey, target, nodeId, enabled),
    onSuccess: (_, variables) => {
      message.success(`${variables.routeKey}: ${variables.nodeId} ${variables.enabled ? 'açıldı' : 'kapandı'}`);
      queryClient.invalidateQueries({ queryKey: queryKeys.routes });
    },
    onError: (error) => {
      message.error(getErrorMessage(error));
    },
  });
}

// =================== MODULES HOOKS ===================

/**
 * Gateway API'den gelen PascalCase yanıtı camelCase'e çevirir
 */
function normalizeModulesResponse(data: gatewayService.ModulesResponse): gatewayService.ModulesResponse {
  const moduller = data.Moduller || data.moduller || [];
  
  return {
    toplamModul: data.ToplamModul ?? data.toplamModul ?? 0,
    toplamEndpoint: data.ToplamEndpoint ?? data.toplamEndpoint ?? 0,
    moduller: moduller.map((m) => ({
      code: m.Code ?? m.code ?? '',
      name: m.Name ?? m.name ?? '',
      description: m.Description ?? m.description ?? '',
      endpointCount: m.EndpointCount ?? m.endpointCount ?? 0,
      newPercentage: m.NewPercentage ?? m.newPercentage ?? 0,
      routes: (m.Routes ?? m.routes ?? []).map((r) => ({
        key: r.Key ?? r.key ?? '',
        legacyPath: r.LegacyPath ?? r.legacyPath ?? '',
        newPath: r.NewPath ?? r.newPath ?? '',
        httpMethods: r.HttpMethods ?? r.httpMethods ?? r.UpstreamHttpMethod ?? [],
        overridePercentage: r.OverridePercentage ?? r.overridePercentage ?? r.NewSystemPercentage ?? null,
        upstreamPathTemplate: r.UpstreamPathTemplate ?? r.legacyPath ?? '',
      })),
    })),
  };
}

export function useModules() {
  return useQuery({
    queryKey: queryKeys.modules,
    queryFn: async () => {
      const data = await gatewayService.getModules();
      console.log('[useModules] Raw API data:', data);
      const normalized = normalizeModulesResponse(data);
      console.log('[useModules] Normalized data:', normalized);
      return normalized;
    },
  });
}

export function useSetModulePercentage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      moduleCode,
      percentage,
    }: {
      moduleCode: string;
      percentage: number;
    }) => gatewayService.setModulePercentage(moduleCode, percentage),
    onSuccess: (_, variables) => {
      const targetText =
        variables.percentage === 100
          ? 'Yeni sisteme'
          : variables.percentage === 0
          ? 'Eski sisteme'
          : `%${variables.percentage} yeni sisteme`;
      message.success(`${variables.moduleCode} modülü ${targetText} yönlendirildi`);
      queryClient.invalidateQueries({ queryKey: queryKeys.modules });
    },
    onError: (error) => {
      message.error(getErrorMessage(error));
    },
  });
}

export function useSetRouteOverride() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      moduleCode,
      path,
      percentage,
    }: {
      moduleCode: string;
      path: string;
      percentage: number | null;
    }) => {
      console.log('[useSetRouteOverride] Calling API:', { moduleCode, path, percentage });
      await gatewayService.setRouteOverride(moduleCode, path, percentage);
      console.log('[useSetRouteOverride] API call successful');
    },
    onSuccess: (_, variables) => {
      const msg =
        variables.percentage === null
          ? 'Modül oranına döndürüldü'
          : variables.percentage === 100
          ? 'Yeni sisteme yönlendirildi'
          : variables.percentage === 0
          ? 'Eski sisteme yönlendirildi'
          : `%${variables.percentage} yeni sisteme yönlendirildi`;
      message.success(msg);
      console.log('[useSetRouteOverride] Invalidating queries...');
      queryClient.invalidateQueries({ queryKey: queryKeys.modules });
    },
    onError: (error) => {
      console.error('[useSetRouteOverride] Error:', error);
      message.error(getErrorMessage(error));
    },
  });
}

export function useEmergencyRollback() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: gatewayService.emergencyRollback,
    onSuccess: () => {
      message.success('Tüm trafik eski sisteme yönlendirildi');
      queryClient.invalidateQueries({ queryKey: queryKeys.modules });
    },
    onError: (error) => {
      message.error(getErrorMessage(error));
    },
  });
}

// =================== HEALTH HOOKS ===================

export function useSystemHealth() {
  return useQuery({
    queryKey: queryKeys.systemHealth,
    queryFn: gatewayService.getSystemHealth,
    refetchInterval: 60000, // Her 1 dakikada yenile
  });
}

export function useGatewayStatus() {
  return useQuery({
    queryKey: queryKeys.gatewayStatus,
    queryFn: gatewayService.getGatewayStatus,
  });
}
