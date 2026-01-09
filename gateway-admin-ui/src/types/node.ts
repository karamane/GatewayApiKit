/**
 * Gateway hedef node tanımı
 */
export interface GatewayNode {
  id: string;
  baseUrl: string;
  enabled: boolean;
  weight: number;
}

/**
 * Node listesi yanıtı
 */
export interface NodesResponse {
  legacy: GatewayNode[];
  new: GatewayNode[];
}

/**
 * Node sağlık durumu
 */
export interface NodeHealthStatus {
  id: string;
  ready: boolean;
  hint: string;
}

/**
 * Node sağlık durumu yanıtı
 */
export interface NodeStatusResponse {
  legacy: NodeHealthStatus[];
  new: NodeHealthStatus[];
}

/**
 * Node etkinleştirme/devre dışı bırakma isteği
 */
export interface SetNodeEnabledRequest {
  enabled: boolean;
}

/**
 * Route bazlı node override isteği
 */
export interface SetRouteNodeEnabledRequest {
  routeKey: string;
  target: 'Legacy' | 'New';
  nodeId: string;
  enabled: boolean;
}
