# ApiGatewayKit Gateway - Geliştirici Rehberi

Bu doküman, Gateway ve Admin UI projelerinin kurulumu, konfigürasyonu, bakımı ve yeni sistem entegrasyonu için kapsamlı bir rehber sunar.

---

## İçindekiler

1. [Proje Yapısı](#1-proje-yapısı)
2. [Hızlı Başlangıç](#2-hızlı-başlangıç)
3. [Konfigürasyon Rehberi](#3-konfigürasyon-rehberi)
4. [Yeni Sistem Entegrasyonu](#4-yeni-sistem-entegrasyonu)
5. [Admin UI Bakım Rehberi](#5-admin-ui-bakım-rehberi)
6. [Ortam Değişikliği](#6-ortam-değişikliği)
7. [Güvenlik](#7-güvenlik)
8. [Sorun Giderme](#8-sorun-giderme)

---

## 1. Proje Yapısı

```
Gateway/
├── src/
│   └── Gateway/
│       └── ApiGatewayKit.Gateway/     # Ana Gateway uygulaması
│           ├── appsettings.json       # Ana konfigürasyon
│           ├── appsettings.Development.json
│           ├── ocelot.json            # Route tanımları
│           ├── gateway-targets.json   # Node/sunucu tanımları
│           ├── Controllers/           # Admin API
│           ├── Services/              # İş mantığı
│           └── Program.cs             # Uygulama başlangıcı
│
├── gateway-admin-ui/                  # React Admin Panel
│   ├── src/
│   │   ├── features/                  # Sayfa bileşenleri
│   │   ├── components/                # Ortak UI bileşenleri
│   │   ├── services/                  # API servisleri
│   │   └── hooks/                     # React Query hooks
│   └── vite.config.ts                 # Build konfigürasyonu
│
├── start-dev.cmd                      # Geliştirme başlatma scripti
└── DeveloperGuide.md                  # Bu doküman
```

---

## 2. Hızlı Başlangıç

### Gereksinimler

- .NET 10.0+ SDK
- Node.js 18+ (Admin UI için)
- Visual Studio 2026 / VS Code / Rider

### Geliştirme Ortamını Başlatma

```cmd
# Tek komutla her iki servisi başlat
start-dev.cmd
```

veya ayrı ayrı:

```powershell
# Terminal 1 - Gateway API (port 53000)
cd src\Gateway\ApiGatewayKit.Gateway
dotnet run

# Terminal 2 - Admin UI (port 3000)
cd gateway-admin-ui
npm install
npm run dev
```

### Erişim Adresleri

| Servis | URL | Açıklama |
|--------|-----|----------|
| Gateway API | http://localhost:53000 | Ana API Gateway |
| Swagger | http://localhost:53000/swagger | API dokümantasyonu |
| Admin UI | http://localhost:3000 | React yönetim paneli |

### Admin Panel Girişi

API Key: `dev-admin-key-2024` (Development ortamı için)

---

## 3. Konfigürasyon Rehberi

### 3.1 appsettings.json - Ana Konfigürasyon

```json
{
  "AdminUI": { ... },           // Admin Panel URL ayarları
  "ModuleParsing": { ... },     // Modül belirleme kuralları
  "ModuleDefinitions": { ... }, // Modül isimleri (Türkçe)
  "GatewayAdmin": { ... },      // Admin API güvenlik ayarları
  "DownstreamWatcher": { ... }, // Sağlık kontrolü ayarları
  "Gateway": { ... },           // Genel gateway ayarları
  "IpRateLimiting": { ... },    // Rate limiting kuralları
  "CacheProvider": { ... },     // Cache ayarları
  "Logging": { ... },           // Loglama ayarları
  "Kestrel": { ... }            // HTTP sunucu ayarları
}
```

#### AdminUI - Admin Panel URL Ayarları

```json
"AdminUI": {
  "DevServerUrls": [
    "http://localhost:3000",
    "http://127.0.0.1:3000"
  ],
  "ProductionUrl": "https://admin.yourdomain.com",
  "RedirectUrl": "http://localhost:3000/admin"
}
```

| Alan | Açıklama |
|------|----------|
| `DevServerUrls` | Geliştirme ortamında CORS izinli URL'ler |
| `ProductionUrl` | Üretim ortamı URL'i (CORS için) |
| `RedirectUrl` | `/admin` isteğinde yönlendirilecek adres |

#### ModuleParsing - Modül Belirleme Kuralları

```json
"ModuleParsing": {
  "PathPatterns": [
    { "Prefix": "moim/api/v1/internet", "ModuleSegmentIndex": 4 },
    { "Prefix": "api", "ModuleSegmentIndex": 1 }
  ],
  "ModuleAliases": {
    "billlimit": "Bill",
    "genericmessages": "GenericMessages"
  }
}
```

| Alan | Açıklama |
|------|----------|
| `PathPatterns` | URL path'inden modül çıkarma kuralları |
| `Prefix` | Path'in başlangıç kısmı (eşleşme için) |
| `ModuleSegmentIndex` | Modül adının bulunduğu segment indexi (0'dan başlar) |
| `ModuleAliases` | Path'teki isimlerin modül kodlarına çevirisi |

**Örnek:**
- URL: `/moim/api/v1/internet/auth/login`
- Segments: `[moim, api, v1, internet, auth, login]`
- Index 4 = `auth` → Modül: `Auth`

#### ModuleDefinitions - Modül İsimleri

```json
"ModuleDefinitions": {
  "Auth": { "Name": "Kimlik Doğrulama", "Description": "Giriş, oturum işlemleri" },
  "Bill": { "Name": "Fatura", "Description": "Fatura sorgulama ve ödeme" }
}
```

| Alan | Açıklama |
|------|----------|
| Key | Modül kodu (path'ten çıkarılan) |
| `Name` | Türkçe modül adı (UI'da gösterilir) |
| `Description` | Modül açıklaması |

#### GatewayAdmin - Admin API Güvenlik Ayarları

```json
"GatewayAdmin": {
  "Enabled": true,
  "IpWhitelist": ["127.0.0.1", "::1", "10.0.0.0/8"],
  "ApiKeys": [
    {
      "Name": "DevOps-Team",
      "KeyHash": "sha256:3b612c75...",
      "Permissions": ["read", "write", "emergency"],
      "IsActive": true
    }
  ],
  "RateLimit": {
    "Enabled": true,
    "RequestsPerMinute": 30,
    "BlockDurationMinutes": 5
  }
}
```

| Alan | Açıklama |
|------|----------|
| `Enabled` | Admin API aktif mi |
| `IpWhitelist` | İzin verilen IP adresleri/CIDR blokları |
| `ApiKeys` | API anahtarları listesi |
| `ApiKeys[].KeyHash` | SHA256 hash veya `plain:key` (sadece dev) |
| `ApiKeys[].Permissions` | `read`, `write`, `emergency` yetkileri |
| `RateLimit` | İstek sınırlama ayarları |

**API Key Oluşturma:**

```powershell
# PowerShell ile SHA256 hash oluşturma
$key = "my-secret-key"
$bytes = [System.Text.Encoding]::UTF8.GetBytes($key)
$hash = [System.Security.Cryptography.SHA256]::Create().ComputeHash($bytes)
$hashString = [BitConverter]::ToString($hash).Replace("-", "").ToLower()
Write-Host "sha256:$hashString"
```

#### DownstreamWatcher - Sağlık Kontrolü

```json
"DownstreamWatcher": {
  "Enabled": true,
  "CheckIntervalSeconds": 60
}
```

| Alan | Açıklama |
|------|----------|
| `Enabled` | Otomatik sağlık kontrolü aktif mi |
| `CheckIntervalSeconds` | Kontrol aralığı (saniye) |

#### Gateway - Genel Ayarlar

```json
"Gateway": {
  "DefaultTarget": "Legacy",
  "LegacyBaseUrl": "http://localhost:39414",
  "NewBaseUrl": "https://localhost:52201",
  "RequestTimeoutSeconds": 120,
  "RetryCount": 2,
  "CircuitBreakerThreshold": 5,
  "BypassPaths": ["/swagger", "/health", "/admin"]
}
```

| Alan | Açıklama |
|------|----------|
| `DefaultTarget` | Varsayılan hedef sistem (`Legacy` veya `New`) |
| `RequestTimeoutSeconds` | İstek zaman aşımı |
| `RetryCount` | Başarısız isteklerde yeniden deneme sayısı |
| `CircuitBreakerThreshold` | Devre kesici eşiği |
| `BypassPaths` | Gateway'den geçirilmeyecek path'ler |

---

### 3.2 gateway-targets.json - Node/Sunucu Tanımları

```json
{
  "GatewayTargets": {
    "Legacy": {
      "Nodes": [
        {
          "Id": "legacy-1",
          "BaseUrl": "http://localhost:39414",
          "Enabled": true,
          "Weight": 100
        }
      ]
    },
    "New": {
      "Nodes": [
        {
          "Id": "new-1",
          "BaseUrl": "https://localhost:52201",
          "Enabled": true,
          "Weight": 100
        }
      ]
    }
  }
}
```

| Alan | Açıklama |
|------|----------|
| `Legacy` | Eski sistem sunucuları |
| `New` | Yeni sistem sunucuları |
| `Id` | Benzersiz sunucu kimliği |
| `BaseUrl` | Sunucu adresi |
| `Enabled` | Sunucu aktif mi (global toggle) |
| `Weight` | Yük dengeleme ağırlığı (1-1000) |

**Load Balancing:**
- `Weight: 100` + `Weight: 100` = Her sunucu %50 trafik alır
- `Weight: 100` + `Weight: 300` = İlk sunucu %25, ikinci %75 alır

---

### 3.3 ocelot.json - Route Tanımları

```json
{
  "Routes": [
    {
      "Key": "auth-login",
      "UpstreamPathTemplate": "/moim/api/v1/internet/auth/login",
      "UpstreamHttpMethod": ["POST"],
      "DownstreamPathTemplate": "/api/auth/login",
      "DownstreamScheme": "https",
      "Priority": 10,
      "Metadata": {
        "Module": "Auth",
        "NewSystemPercentage": 50,
        "DisabledNodes": {
          "Legacy": [],
          "New": ["new-2"]
        }
      }
    }
  ],
  "FeatureManagement": {
    "Auth_UseNew": {
      "EnabledFor": [
        { "Name": "Percentage", "Parameters": { "Value": 80 } }
      ]
    }
  }
}
```

#### Route Alanları

| Alan | Açıklama |
|------|----------|
| `Key` | Benzersiz route anahtarı |
| `UpstreamPathTemplate` | Gelen istek path'i (Gateway'e gelen) |
| `DownstreamPathTemplate` | Hedef path (backend'e giden) |
| `UpstreamHttpMethod` | İzin verilen HTTP metotları |
| `Priority` | Öncelik (düşük = yüksek öncelik) |
| `Metadata.Module` | Modül kodu (opsiyonel, yoksa path'ten çıkarılır) |
| `Metadata.NewSystemPercentage` | Bu endpoint için yeni sistem yüzdesi |
| `Metadata.DisabledNodes` | Bu endpoint için pasif sunucular |

#### FeatureManagement

| Alan | Açıklama |
|------|----------|
| `{Module}_UseNew` | Modül bazlı yeni sisteme yönlendirme oranı |
| `Value` | 0-100 arası yüzde değeri |

---

### 3.4 Admin UI Konfigürasyonu

#### vite.config.ts

```typescript
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  
  const apiBaseUrl = env.VITE_API_BASE_URL || 'http://localhost:53000';
  const devServerPort = parseInt(env.VITE_DEV_SERVER_PORT || '3000', 10);
  
  return {
    server: {
      port: devServerPort,
      proxy: {
        '/api': {
          target: apiBaseUrl,
          changeOrigin: true,
        },
      },
    },
  };
});
```

#### Environment Variables

| Değişken | Varsayılan | Açıklama |
|----------|------------|----------|
| `VITE_API_BASE_URL` | `http://localhost:53000` | Gateway API adresi |
| `VITE_DEV_SERVER_PORT` | `3000` | Dev server portu |

`.env.local` dosyası oluşturarak değiştirilebilir:

```env
VITE_API_BASE_URL=http://localhost:53000
VITE_DEV_SERVER_PORT=3000
```

---

## 4. Yeni Sistem Entegrasyonu

Yeni bir backend sistemi entegre etmek için aşağıdaki adımları izleyin:

### Adım 1: Sunucu Tanımlama

`gateway-targets.json` dosyasına yeni sunucu ekleyin:

```json
{
  "GatewayTargets": {
    "Legacy": { ... },
    "New": { ... },
    "NewSystem": {
      "Nodes": [
        {
          "Id": "newsystem-1",
          "BaseUrl": "https://newsystem.internal:443",
          "Enabled": true,
          "Weight": 100
        }
      ]
    }
  }
}
```

### Adım 2: Modül Tanımlama

`appsettings.json`'a modül bilgisi ekleyin:

```json
"ModuleDefinitions": {
  "NewModule": { 
    "Name": "Yeni Modül", 
    "Description": "Yeni modül açıklaması" 
  }
}
```

Path pattern gerekiyorsa:

```json
"ModuleParsing": {
  "PathPatterns": [
    { "Prefix": "newsystem/api", "ModuleSegmentIndex": 2 }
  ]
}
```

### Adım 3: Route Ekleme

`ocelot.json`'a route ekleyin:

```json
{
  "Key": "newmodule-endpoint",
  "UpstreamPathTemplate": "/newsystem/api/v1/{everything}",
  "UpstreamHttpMethod": ["GET", "POST"],
  "DownstreamPathTemplate": "/api/v1/{everything}",
  "DownstreamScheme": "https",
  "Priority": 10,
  "Metadata": {
    "Module": "NewModule",
    "NewSystemPercentage": 0
  }
}
```

### Adım 4: Feature Flag Ekleme

Modül bazlı yüzde kontrolü için:

```json
"FeatureManagement": {
  "NewModule_UseNew": {
    "EnabledFor": [
      { "Name": "Percentage", "Parameters": { "Value": 0 } }
    ]
  }
}
```

### Adım 5: Kodu Güncelleme (Gerekirse)

Eğer yeni bir target sistem tipi ekliyorsanız (Legacy/New dışında), kod değişikliği gerekir:

1. `TargetSystem` enum'una yeni değer ekle
2. `ITargetNodeSelector` implementasyonunu güncelle
3. `FeatureRoutingHandler`'ı güncelle

---

## 5. Admin UI Bakım Rehberi

### 5.1 Proje Yapısı

```
gateway-admin-ui/src/
├── features/                    # Sayfa bileşenleri
│   ├── auth/                    # Giriş sayfası
│   │   ├── LoginPage.tsx        # Login formu
│   │   └── AuthContext.tsx      # Oturum yönetimi
│   ├── routing/                 # Routing Yönetimi
│   │   ├── RoutingPage.tsx      # Ana sayfa
│   │   └── components/
│   │       ├── ModuleCard.tsx   # Modül kartı
│   │       └── EndpointTable.tsx # Endpoint tablosu
│   ├── load-balancing/          # Yük Dengeleme
│   │   ├── LoadBalancingPage.tsx
│   │   └── components/
│   │       ├── NodeTable.tsx    # Sunucu tablosu
│   │       └── RouteNodeOverrides.tsx
│   └── system-health/           # Sistem Sağlığı
│       └── SystemHealthPage.tsx
├── components/                  # Ortak bileşenler
│   ├── ui/                      # Atomik UI bileşenleri
│   │   ├── StatusDot.tsx        # Durum göstergesi
│   │   ├── NodeToggle.tsx       # Açma/kapama düğmesi
│   │   └── PageHeader.tsx       # Sayfa başlığı
│   └── layout/
│       ├── AppLayout.tsx        # Ana layout
│       └── Sidebar.tsx          # Sol menü
├── services/
│   ├── api.ts                   # Axios instance
│   └── gatewayService.ts        # API fonksiyonları
├── hooks/
│   └── useApi.ts                # React Query hooks
└── types/
    ├── node.ts                  # Node tipleri
    ├── route.ts                 # Route tipleri
    └── health.ts                # Sağlık tipleri
```

### 5.2 Sayfa Düzenleme Rehberi

#### Yeni Sayfa Ekleme

1. **Feature klasörü oluştur:**
   ```
   src/features/new-feature/
   ├── NewFeaturePage.tsx
   ├── components/
   │   └── FeatureComponent.tsx
   └── index.ts
   ```

2. **Route ekle (`App.tsx`):**
   ```tsx
   <Route path="/new-feature" element={<NewFeaturePage />} />
   ```

3. **Menüye ekle (`Sidebar.tsx`):**
   ```tsx
   { key: 'new-feature', icon: <IconComponent />, label: 'Yeni Özellik' }
   ```

#### API Endpoint Ekleme

1. **Tip tanımla (`types/`):**
   ```typescript
   export interface NewFeatureResponse {
     data: string;
   }
   ```

2. **Servis fonksiyonu ekle (`gatewayService.ts`):**
   ```typescript
   export async function getNewFeature(): Promise<NewFeatureResponse> {
     const response = await api.get<NewFeatureResponse>('/new-feature');
     return response.data;
   }
   ```

3. **Hook ekle (`useApi.ts`):**
   ```typescript
   export function useNewFeature() {
     return useQuery({
       queryKey: ['newFeature'],
       queryFn: gatewayService.getNewFeature,
     });
   }
   ```

### 5.3 Ortak Bileşenler

| Bileşen | Dosya | Kullanım |
|---------|-------|----------|
| `StatusDot` | `components/ui/StatusDot.tsx` | Yeşil/kırmızı durum göstergesi |
| `NodeToggle` | `components/ui/NodeToggle.tsx` | Açma/kapama switch |
| `PageHeader` | `components/ui/PageHeader.tsx` | Sayfa başlığı + yenile butonu |
| `DataTable` | `components/ui/DataTable.tsx` | Türkçe tablo bileşeni |
| `ServiceCard` | `components/ui/ServiceCard.tsx` | Servis durum kartı |

### 5.4 Stil ve Tema

- **UI Kütüphanesi:** Ant Design
- **Dil:** Türkçe (`tr_TR` locale)
- **Tarih:** dayjs (`tr` locale)

Tema değişikliği için `main.tsx`:

```tsx
<ConfigProvider
  locale={trTR}
  theme={{
    token: {
      colorPrimary: '#1890ff',
    },
  }}
>
```

---

## 6. Ortam Değişikliği

### 6.1 Development → Production

#### Gateway Tarafı

1. **appsettings.Production.json oluştur:**
   ```json
   {
     "AdminUI": {
       "DevServerUrls": [],
       "ProductionUrl": "https://admin.yourcompany.com",
       "RedirectUrl": "/admin"
     },
     "GatewayAdmin": {
       "ApiKeys": [
         {
           "Name": "Production-Team",
           "KeyHash": "sha256:YOUR_PRODUCTION_HASH",
           "Permissions": ["read", "write"],
           "IsActive": true
         }
       ]
     }
   }
   ```

2. **gateway-targets.json güncelle:**
   - Üretim sunucu URL'lerini ekle
   - `Enabled: false` ile başlat (kontrollü açılış)

3. **ocelot.json güncelle:**
   - Tüm `NewSystemPercentage` değerlerini 0 yap
   - Yavaş yavaş artır (canary deployment)

#### Admin UI Tarafı

1. **Build al:**
   ```bash
   cd gateway-admin-ui
   npm run build
   ```

2. **Dosyaları kopyala:**
   ```bash
   xcopy /E /Y dist\* ..\src\Gateway\ApiGatewayKit.Gateway\wwwroot\admin\
   ```

3. **veya `.env.production` oluştur:**
   ```env
   VITE_API_BASE_URL=https://gateway.yourcompany.com
   ```

### 6.2 Ortam Değişkenleri Tablosu

| Ortam | Gateway Port | Admin UI | API Key |
|-------|-------------|----------|---------|
| Development | 53000 | localhost:3000 | `dev-admin-key-2024` |
| Staging | 53000 | staging-admin.com | Staging hash |
| Production | 443 (HTTPS) | admin.yourcompany.com | Production hash |

### 6.3 Docker ile Deployment

```dockerfile
# Gateway
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY publish/ .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 80 443
ENTRYPOINT ["dotnet", "ApiGatewayKit.Gateway.dll"]
```

```yaml
# docker-compose.yml
version: '3.8'
services:
  gateway:
    build: .
    ports:
      - "53000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    volumes:
      - ./config:/app/config
```

---

## 7. Güvenlik

### 7.1 API Key Yönetimi

- **Development:** `plain:key` formatı kullanılabilir
- **Production:** Sadece `sha256:hash` formatı kabul edilir

```json
{
  "Name": "Service-Account",
  "KeyHash": "sha256:abc123...",
  "Permissions": ["read"],
  "IsActive": true
}
```

### 7.2 IP Whitelist

```json
"IpWhitelist": [
  "10.0.0.0/8",      // Özel ağ
  "192.168.0.0/16",  // Özel ağ
  "203.0.113.50"     // Tek IP
]
```

### 7.3 Rate Limiting

```json
"RateLimit": {
  "Enabled": true,
  "RequestsPerMinute": 30,
  "BlockDurationMinutes": 5
}
```

### 7.4 SSRF Koruması

- Sadece HTTPS URL'ler kabul edilir (Production)
- IP literal ve localhost bloklanır
- Domain allow-list uygulanır

---

## 8. Sorun Giderme

### 8.1 Sık Karşılaşılan Sorunlar

| Sorun | Çözüm |
|-------|-------|
| "Modül bulunamadı" | Gateway çalışıyor mu kontrol et, tarayıcıyı yenile |
| 401 Unauthorized | API Key doğru mu, `X-Admin-Api-Key` header'ı var mı |
| 403 Forbidden | IP whitelist'te misin |
| CORS hatası | `AdminUI.DevServerUrls`'e URL ekle |
| Node'lar görünmüyor | `gateway-targets.json` formatı doğru mu |

### 8.2 Log Dosyaları

```
C:\Logs\ApiGatewayKit.Gateway\
├── all-YYYYMMDD.log        # Tüm loglar
├── error-YYYYMMDD.log      # Sadece hatalar
├── request-YYYYMMDD.log    # HTTP istekleri
└── security-YYYYMMDD.log   # Güvenlik olayları
```

### 8.3 Debug Modu

```json
"Logging": {
  "LogLevel": {
    "Default": "Debug",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

### 8.4 Sağlık Kontrolü

```bash
# Gateway sağlık durumu
curl http://localhost:53000/health

# Admin API testi
curl -H "X-Admin-Api-Key: dev-admin-key-2024" \
     http://localhost:53000/api/gateway/admin/modules
```

---

## Versiyon Geçmişi

| Tarih | Versiyon | Değişiklik |
|-------|----------|------------|
| 2026-01-09 | 1.0.0 | İlk sürüm |

---

*Bu doküman, Gateway ve Admin UI projelerinin bakımı için hazırlanmıştır. Sorularınız için DevOps ekibiyle iletişime geçin.*
