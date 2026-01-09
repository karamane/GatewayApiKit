# Gateway Admin UI

Gateway yönetim panelinin React + TypeScript ile geliştirilmiş modern versiyonu.

## Gereksinimler

- Node.js 18+ 
- npm veya pnpm

## Kurulum

```bash
cd gateway-admin-ui
npm install
```

## Konfigürasyon

Uygulama, aşağıdaki environment variable'lardan yapılandırılabilir. Bu değerler `.env` veya `.env.local` dosyasında tanımlanabilir:

| Değişken | Açıklama | Varsayılan |
|----------|----------|------------|
| `VITE_API_BASE_URL` | Gateway API base URL'i | `http://localhost:53000` |
| `VITE_DEV_SERVER_PORT` | Vite dev server portu | `3000` |

Örnek `.env.local` dosyası:
```env
VITE_API_BASE_URL=http://localhost:53000
VITE_DEV_SERVER_PORT=3000
```

## Geliştirme

```bash
npm run dev
```

Uygulama varsayılan olarak `http://localhost:3000` adresinde başlar (port, `VITE_DEV_SERVER_PORT` ile değiştirilebilir).

> **Not:** Gateway API'nin çalışıyor olması gerekir.
> Vite, `/api` isteklerini otomatik olarak Gateway'e proxy eder.

## Production Build

```bash
npm run build
```

Build çıktısı `dist/` klasörüne oluşturulur.

### Gateway'e Entegrasyon

Build sonrası dosyaları Gateway'in `wwwroot/admin/` klasörüne kopyalayabilirsiniz:

```bash
# Windows
xcopy /E /Y dist\* ..\src\Gateway\ApiGatewayKit.Gateway\wwwroot\admin\

# Linux/Mac
cp -r dist/* ../src/Gateway/ApiGatewayKit.Gateway/wwwroot/admin/
```

## Proje Yapısı

```
src/
├── components/
│   ├── ui/              # Atomik bileşenler (StatusDot, NodeToggle, vb.)
│   └── layout/          # Sidebar, AppLayout
├── features/
│   ├── auth/            # Login, AuthContext
│   ├── routing/         # Routing Yönetimi sayfası
│   ├── load-balancing/  # Yük Dengeleme sayfası
│   └── system-health/   # Sistem Sağlığı sayfası
├── hooks/               # React Query hooks
├── services/            # API servisleri
├── types/               # TypeScript tipleri
├── App.tsx
├── main.tsx
└── index.css
```

## Özellikler

### 🔐 Kimlik Doğrulama
- API Key ile giriş
- sessionStorage'da güvenli saklama
- Otomatik logout (401 hatası)

### 🔀 Routing Yönetimi
- Modül bazlı trafik yüzdesi ayarı
- Endpoint override'ları
- Acil geri alma (Emergency Rollback)

### ⚖️ Yük Dengeleme
- Node listesi ve sağlık durumu
- Global enable/disable
- Route bazlı node override'ları

### 🏥 Sistem Sağlığı
- Downstream servis izleme
- Kritik alarm bildirimleri
- Anlık durum göstergeleri

## Teknoloji Stack

- **React 18** - UI framework
- **TypeScript** - Type safety
- **Vite** - Build tool
- **React Router** - Client-side routing
- **React Query** - Server state management
- **Ant Design** - UI components
- **Axios** - HTTP client

## Güvenlik

- API Key sessionStorage'da saklanır (tab kapatılınca silinir)
- CORS sadece belirlenen origin'lerden izin verir
- TypeScript strict mode aktif
- XSS koruması React default escape ile sağlanır

## Geliştirici Notları

### API Endpoint'leri

Tüm API çağrıları `/api/gateway/admin` prefix'i ile yapılır:

| Endpoint | Açıklama |
|----------|----------|
| `GET /nodes` | Node listesi |
| `GET /nodes/status` | Node sağlık durumları |
| `PUT /nodes/{target}/{nodeId}/enabled` | Global node toggle |
| `GET /routes` | Route listesi |
| `PUT /routes/{routeKey}/nodes` | Route bazlı node toggle |
| `GET /modules` | Modül listesi |
| `PUT /modules/{code}/percentage` | Modül yüzdesi |
| `GET /system-health` | Sistem sağlığı |

### Yeni Bileşen Ekleme

1. `src/components/ui/` altına bileşeni oluştur
2. `src/components/ui/index.ts`'e export ekle
3. Storybook'a örnek ekle (opsiyonel)

### Yeni Sayfa Ekleme

1. `src/features/` altına yeni klasör oluştur
2. `*Page.tsx` ana component'i oluştur
3. `src/App.tsx`'e route ekle
4. `src/components/layout/Sidebar.tsx`'e menü item ekle
