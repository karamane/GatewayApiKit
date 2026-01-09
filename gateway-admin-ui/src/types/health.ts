/**
 * Sistem sağlık özeti
 */
export interface SystemHealthSummary {
  toplamServis: number;
  cevrimiciServis: number;
  cevrimdisiServis: number;
  legacySaglikli: boolean;
  yeniSistemSaglikli: boolean;
  sonGuncelleme: string;
  kritikAlarmVar: boolean;
}

/**
 * Servis sağlık durumu
 */
export interface ServiceHealth {
  servisId: string;
  hedefSistem: string;
  baseUrl: string;
  cevrimici: boolean;
  durum: string | null;
  uygulamaKimligi: string | null;
  versiyon: string | null;
  sonKontrol: string;
  sonCevrimici: string | null;
  yanitSuresiMs: number | null;
  ardisikHataSayisi: number;
  hataMesaji: string | null;
  kritik: boolean;
}

/**
 * Sistem sağlığı API yanıtı
 */
export interface SystemHealthResponse {
  ozet: SystemHealthSummary;
  legacy: ServiceHealth[];
  yeni: ServiceHealth[];
  kritikServisler: ServiceHealth[];
}

/**
 * Gateway durumu
 */
export interface GatewayStatus {
  durum: string;
  varsayilanHedef: string;
}
