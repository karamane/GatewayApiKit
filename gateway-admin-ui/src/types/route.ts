/**
 * Route tanımı (Gateway API yanıtına uygun)
 */
export interface Route {
  // Gateway camelCase döndürür
  key?: string;
  Key?: string;
  upstreamPathTemplate?: string;
  UpstreamPathTemplate?: string;
  // Gateway UpstreamHttpMethod kullanır
  httpMethods?: string[];
  UpstreamHttpMethod?: string[];
  legacyPath?: string;
  newPath?: string;
  overridePercentage?: number | null;
  disabledNodes?: {
    legacy?: string[];
    new?: string[];
    Legacy?: string[];
    New?: string[];
  };
  DisabledNodes?: {
    legacy?: string[];
    new?: string[];
    Legacy?: string[];
    New?: string[];
  };
}

/**
 * Routes yanıtı
 */
export interface RoutesResponse {
  routes?: Route[];
  Routes?: Route[];
}

/**
 * Modül tanımı (Gateway PascalCase döndürür)
 */
export interface Module {
  // camelCase (normalized)
  code?: string;
  name?: string;
  description?: string;
  endpointCount?: number;
  newPercentage?: number;
  routes?: ModuleRoute[];
  // PascalCase (raw API)
  Code?: string;
  Name?: string;
  Description?: string;
  EndpointCount?: number;
  NewPercentage?: number;
  Routes?: ModuleRoute[];
}

/**
 * Modül route tanımı (Gateway PascalCase döndürür)
 */
export interface ModuleRoute {
  // camelCase (normalized)
  key?: string;
  legacyPath?: string;
  newPath?: string;
  httpMethods?: string[];
  overridePercentage?: number | null;
  // PascalCase (raw API)
  Key?: string;
  LegacyPath?: string;
  NewPath?: string;
  HttpMethods?: string[];
  UpstreamHttpMethod?: string[];
  UpstreamPathTemplate?: string;
  OverridePercentage?: number | null;
  NewSystemPercentage?: number | null;
}

/**
 * Modüller yanıtı (Gateway PascalCase döndürür)
 */
export interface ModulesResponse {
  // camelCase (normalized)
  toplamModul?: number;
  toplamEndpoint?: number;
  moduller?: Module[];
  // PascalCase (raw API)
  ToplamModul?: number;
  ToplamEndpoint?: number;
  Moduller?: Module[];
}

/**
 * Modül yüzdesi güncelleme isteği
 */
export interface SetModulePercentageRequest {
  percentage: number;
}

/**
 * Route override güncelleme isteği
 * Gateway: PUT /routes/override { path, percentage }
 */
export interface SetRouteOverrideRequest {
  path: string;
  percentage: number | null;
}
