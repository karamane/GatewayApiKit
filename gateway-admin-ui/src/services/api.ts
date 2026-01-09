import axios, { AxiosError, AxiosInstance } from 'axios';

const STORAGE_KEY = 'gateway_admin_api_key';

/**
 * API Key'i sessionStorage'dan al
 */
export function getApiKey(): string | null {
  return sessionStorage.getItem(STORAGE_KEY);
}

/**
 * API Key'i sessionStorage'a kaydet
 */
export function setApiKey(apiKey: string): void {
  sessionStorage.setItem(STORAGE_KEY, apiKey);
}

/**
 * API Key'i sessionStorage'dan sil
 */
export function clearApiKey(): void {
  sessionStorage.removeItem(STORAGE_KEY);
}

/**
 * Axios instance oluştur
 */
function createApiClient(): AxiosInstance {
  const client = axios.create({
    baseURL: '/api/gateway/admin',
    timeout: 30000,
    headers: {
      'Content-Type': 'application/json; charset=utf-8',
    },
  });

  // Request interceptor - API key ekleme
  client.interceptors.request.use(
    (config) => {
      const apiKey = getApiKey();
      if (apiKey) {
        config.headers['X-Admin-Api-Key'] = apiKey;
      }
      return config;
    },
    (error) => Promise.reject(error)
  );

  // Response interceptor - Hata yönetimi
  client.interceptors.response.use(
    (response) => response,
    (error: AxiosError) => {
      if (error.response?.status === 401) {
        clearApiKey();
        window.location.href = '/admin/login';
      }
      return Promise.reject(error);
    }
  );

  return client;
}

export const api = createApiClient();

/**
 * API hatalarını kullanıcı dostu mesaja çevir
 */
export function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    if (error.response) {
      const status = error.response.status;
      const data = error.response.data;
      
      // Status code based messages
      if (status === 400) {
        if (typeof data === 'string' && data.length > 0) return data;
        if (data?.message) return data.message;
        return 'Geçersiz istek parametreleri';
      }
      if (status === 401) return 'Yetkilendirme hatası - Lütfen tekrar giriş yapın';
      if (status === 403) return 'Bu işlem için yetkiniz yok';
      if (status === 404) return 'İstenen kaynak bulunamadı';
      if (status === 500) return 'Sunucu hatası - Lütfen daha sonra tekrar deneyin';
      
      // Try to extract message from response
      if (typeof data === 'string' && data.length > 0) return data;
      if (data?.message) return data.message;
      if (data?.error) return data.error;
      if (data?.Mesaj) return data.Mesaj; // Turkish API response
      return `Sunucu hatası: ${status}`;
    }
    if (error.request) {
      return 'Sunucuya bağlanılamıyor';
    }
    return error.message || 'Bilinmeyen hata';
  }
  if (error instanceof Error) {
    return error.message || 'Bilinmeyen hata';
  }
  return 'Beklenmeyen hata';
}
