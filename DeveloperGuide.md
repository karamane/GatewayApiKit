## Developer Guide

### Amaç
Bu repo **SOLID**, **Clean Architecture** ve **Onion** yaklaşımıyla geliştirilir. Hedefimiz basit, anlaşılır, testlenebilir ve sürdürülebilir bir kod tabanı.

### Mimari eksen (Onion / Clean Architecture)
- **Domain (`src/Core/ApiGatewayKit.Core.Domain`)**
  - Saf iş kuralları ve entity’ler.
  - Framework/IO bağımlılığı yok.
- **Application (`src/Core/ApiGatewayKit.Core.Application`)**
  - Use-case’ler, orchestrasyon ve port’lar (interface/abstraction).
  - Domain’e bağımlıdır; Infrastructure’a bağımlı değildir.
- **Infrastructure (`src/Infrastructure/*`)**
  - Adapter’lar / implementasyonlar (cache, logging, external services).
  - Application’daki abstraction’ları uygular.
- **Gateway (`src/Gateway/ApiGatewayKit.Gateway`)**
  - Composition root (DI), HTTP concerns, middleware/controller.
  - İç katmanlara (Application) dayanır, Infrastructure implementasyonlarını DI ile bağlar.

### SOLID kuralları
- **S**: Her sınıf/metot tek sorumluluk.
- **O**: Davranışı genişletilebilir yap (yeni class/strategy), mevcut kodu kırma.
- **L**: Türetilen sınıflar base kontratını bozmaz.
- **I**: Büyük interface yerine küçük/amaçlı interface.
- **D**: Gateway/Infrastructure, Application port’larına bağlanır; concrete bağımlılık içeri akmaz.

### Test kuralları (zorunlu)
- **Her metot için test yazılır**.
  - Saf/IO’suz metotlar: unit test (deterministik assertion).
  - HTTP pipeline (middleware/controller): integration test (TestServer / WebApplicationFactory).
- **Yeni eklenen veya değişen her metot aynı değişiklikte testleriyle birlikte gelmek zorunda.**

### Definition of Done
- İlgili metotların testleri eklendi/güncellendi
- `dotnet test` başarılı
- `DeveloperGuide.md` ve `DeveloperGuide.html` güncellendi

### Güvenlik (zorunlu)
- **Security-first**: her değişiklikte güvenlik etkisi düşünülür ve dokümante edilir.
- **Input validation**: dış girdiler validate/sanitize edilir (HTTP/header/query/config/external). Allow-list tercih edilir.
- **AuthN/AuthZ**: korumalı endpoint’lerde auth zorunlu; deny-by-default; bypass yok.
- **Secrets**: secret/credential repoya girmez; log’lanmaz; güvenli kaynaklardan okunur.
- **PII/logging**: minimum log, hassas alanlar maskelenir, log injection engellenir.
- **Crypto**: custom crypto yok; .NET güvenilir primitive’leri; password hash için güçlü algoritmalar (BCrypt/PBKDF2/Argon2).
- **SSRF/outbound**: user-controlled URL fetch yok; outbound hedef allow-list/validasyon.
- **File/path safety**: path traversal önle; user input ile path birleştirme risklerine dikkat.

### Definition of Done (Security)
- Güvenlik etkisi ve mitigasyonları bu dokümana eklendi
- Değişen/yeni metotların güvenlik-kritik branch’leri test edildi

### Komutlar
```bash
dotnet test .\ApiGatewayKit.Gateway.sln -c Debug
```

### Uygulama kullanımı (Developer + DevOps)

#### Çalıştırma
- **Local (Development)**:
  - `src/Gateway/ApiGatewayKit.Gateway/Properties/launchSettings.json` profillerinde varsayılan URL’ler:
    - HTTP: `http://localhost:53000`
    - HTTPS: `https://localhost:53001` (ek olarak HTTP)
  - Admin panel varsayılan açılış path’i: `/admin`
- **Sağlık kontrolü**:
  - `GET /health`

#### Admin Panel (UI)
- **URL**: `/admin` (statik dosya: `src/Gateway/ApiGatewayKit.Gateway/wwwroot/admin/index.html`)
- **Auth**: Admin panel giriş ekranı **API Key** ister ve backend’e şu header ile çağrı yapar:
  - `X-Admin-Api-Key: <PLAIN_TEXT_KEY>`
- **Not**: API key tarayıcıda `localStorage` altında saklanır; shared machine/VDI ortamlarında dikkat edilir.

#### Admin API (HTTP)
Admin API route prefix: `/api/gateway/admin`

**Okuma (Read) operasyonları**
- `GET /api/gateway/admin/health`  
  - Gateway ortam/versiyon ve ocelot.json varlığı gibi temel durum bilgisi.
- `GET /api/gateway/admin/config`  
  - Gateway config özetini döner.
- `GET /api/gateway/admin/modules`  
  - Modüller + route’lar ve metrikler (toplam modül/endpoint).
- `GET /api/gateway/admin/modules/{moduleCode}`  
  - Tek modül detayı.
- `GET /api/gateway/admin/module-definitions`  
  - Modül sözlüğü (Türkçe ad/açıklama).
- `GET /api/gateway/admin/feature-flags`  
  - Feature flag durumları.
- `GET /api/gateway/admin/routes/overrides`  
  - Aktif endpoint override listesi.

**Yazma (Write) operasyonları**
- `PUT /api/gateway/admin/modules/{moduleCode}/percentage`  
  - Body: `{ "percentage": 0..100 }`  
  - İlgili modülün yeni sisteme yönlenme yüzdesini günceller (ocelot.json FeatureManagement bölümünü günceller).
- `PUT /api/gateway/admin/routes/override`  
  - Body: `{ "moduleCode": "<code>", "path": "<upstreamPathTemplate>", "percentage": 0..100 | null }`  
  - Belirli endpoint için override yazar/kaldırır (`percentage:null` => override kaldır).
- `DELETE /api/gateway/admin/routes/override?path=<upstreamPathTemplate>`  
  - Tek endpoint override kaldırır (üstteki override çağrısını kullanır).
- `DELETE /api/gateway/admin/routes/overrides/clear`  
  - Tüm endpoint override’larını temizler.

**Acil durum (Emergency) operasyonları**
- `POST /api/gateway/admin/emergency/rollback`  
  - Tüm trafiği legacy’ye çeker (ocelot.json içindeki yüzdeleri 0 yapar ve route override’ları temizler).

#### Admin Security modeli (GatewayAdmin)
Admin güvenliği **DB kullanmadan**, `appsettings*.json` üzerinden yönetilir.

- **Config section**: `GatewayAdmin` (bkz. `src/Gateway/ApiGatewayKit.Gateway/appsettings.json`)
- **Katmanlar (sıra önemlidir)**:
  - IP Whitelist → API Key Auth → Admin Rate Limit

##### IP Whitelist
- `GatewayAdmin:IpWhitelist` listesi admin endpoint’lerine erişebilecek IP/CIDR’ları tanımlar.
- CIDR destekler (örn: `10.0.0.0/8`, `192.168.0.0/16`).

##### API Key üretimi ve konfigürasyonu
- **Plain text key repoda tutulmaz**; config’e **SHA256 hash** yazılır.
- **Header ile kullanım (önerilen)**:
  - `X-Admin-Api-Key: <PLAIN_TEXT_KEY>`
- **Query string ile kullanım (sadece troubleshooting)**:
  - `?api_key=<PLAIN_TEXT_KEY>`
- **Hash formatı**:
  - `sha256:<hex>`

**Development ortamında hash üretme (tools endpoint)**
- Bu endpoint **sadece Development**’ta aktiftir; Production’da 404 döner.
- `GET /api/gateway/admin/tools/generate-key-hash?key=<plainText>`
  - `key` verilmezse rastgele bir key üretir.
- Öneri (DevOps):
  - PLAIN TEXT key’i secret store’da sakla (örn. environment/secret manager).
  - App config’te sadece `KeyHash` tut.

##### Permission (Read/Write/Emergency)
`GatewayAdmin:ApiKeys[*]:Permissions` içinde tanımlanır:
- `read`: okuma endpoint’leri
- `write`: yazma endpoint’leri (+ `read`)
- `emergency`: acil durum operasyonları (+ `write` + `read`)

##### Rate Limit (Admin)
Admin endpoint’leri için ayrı in-memory rate limit vardır:
- `GatewayAdmin:RateLimit:RequestsPerMinute`
- `GatewayAdmin:RateLimit:BlockDurationMinutes`
- Response header’ları:
  - `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`
- Limit aşımında:
  - HTTP `429` + `Retry-After`

##### Production kısıtları
Production’da admin yazma operasyonlarını kısıtlamak için:
- `GatewayAdmin:Production:DisableWriteEndpoints`
- `GatewayAdmin:Production:AllowedEndpoints` (boşsa hepsine izin)

##### Audit Log
Admin API çağrıları dosyaya audit log olarak yazılabilir:
- `GatewayAdmin:Audit:FilePath` örn: `logs/admin-audit/audit-{Date}.log`
- `LogRequestBody` hassas veri içerebilir; güvenlik gereksinimlerine göre kapatılabilir.

#### Operasyonel notlar (DevOps)
- **ocelot.json**:
  - Gateway routing ve feature flag yüzdeleri bu dosya üzerinden yönetilir.
  - Admin write operasyonları ocelot.json’ı günceller; container/host üzerinde dosyanın **writeable** olması gerekir.
- **Reverse proxy / gerçek IP**:
  - Genel IP rate limiting `X-Real-IP` header’ını kullanacak şekilde ayarlı (`IpRateLimiting:RealIpHeader`).
  - Reverse proxy arkasında doğru client IP’yi taşımak için header forwarding/allow-list uygulanır.

