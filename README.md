# ApiGatewayKit - Enterprise API Gateway

ApiGatewayKit, yüksek performanslı, ölçeklenebilir ve güvenli bir **.NET API Gateway** çözümüdür.  
**Ocelot** tabanlı olup, kurumsal ihtiyaçlara yönelik gelişmiş özelliklerle donatılmıştır.

## 🚀 Öne Çıkan Özellikler

### 🛡️ Güvenlik & Throttling
- **Token-Based Rate Limiting:** IP adresi yerine, JWT token (sub/uid) bazlı adil limitlendirme.
- **Distributed Throttling:** **Redis** entegrasyonu sayesinde çoklu sunucu (farm/cluster) ortamında senkronize hız sınırları.
- **Application-Layer DoS Protection:** Kötü niyetli kullanıcıları token bazlı engelleyerek sistem kaynaklarını korur.
- **Admin Güvenliği:** IP Whitelist ve API Key korumalı yönetim paneli.

### ⚡ Performans & Dayanıklılık (Resilience)
- **Concurrency Limiter:** Global eşzamanlı istek sınırlaması ile sunucuyu (Thread Starvation) korur.
- **Polly Entegrasyonu:**
  - **Bulkhead:** Kuyruksuz (Queue=0) fail-fast mekanizması.
  - **Circuit Breaker:** Hatalı servisleri izole etme.
  - **Timeout & Retry:** Akıllı tekrar deneme stratejileri.
- **Response Compression:** Brotli/Gzip desteği ile bant genişliği optimizasyonu.

### 👁️ Gözlemlenebilirlik (Observability)
- **Yapısal Loglama (Serilog):** JSON formatında, ELK/OpenSearch uyumlu loglar.
- **Enrichment:** `TokenHash`, `CorrelationId`, `Service`, `Environment` gibi zengin metadata.
- **Custom Middleware:** Tüm istek, yanıt ve hata yaşam döngüsünün merkezi takibi.

## 🏗️ Mimari Bileşenler

| Bileşen | Teknoloji | Amaç |
|---------|-----------|------|
| **Core Gateway** | Ocelot | Yönlendirme, Load Balancing |
| **Rate Limit** | AspNetCoreRateLimit + Redis | Kota ve Hız Sınırlama |
| **Resilience** | Polly | Hata Toleransı ve Devre Kesici |
| **Logging** | Serilog | Yapısal Loglama |
| **Auth** | JWT (System.IdentityModel) | Kimlik Doğrulama |

## 🛠️ Kurulum ve Çalıştırma

### Gereksinimler
- .NET 8.0 SDK veya üzeri
- Redis (Opsiyonel - Dağıtık Rate Limit için)

### Geliştirme Ortamı
1. Repoyu klonlayın.
2. Bağımlılıkları yükleyin: `dotnet restore`
3. Projeyi ayağa kaldırın:
   ```bash
   dotnet run --project src/Gateway/ApiGatewayKit.Gateway/ApiGatewayKit.Gateway.csproj
   ```
4. Gateway `http://localhost:53000` adresinde çalışacaktır.

### Redis Konfigürasyonu (Opsiyonel)
Dağıtık Rate Limit kullanmak için `appsettings.json` dosyasını düzenleyin:
```json
"RateLimitOptions": {
  "UseRedis": true
},
"ConnectionStrings": {
  "Redis": "localhost:6379"
}
```

## 🧪 Testler
Yük ve güvenlik testleri için `k6` scriptleri `tests/` klasöründe mevcuttur.

**Token Flood Testi:**
```bash
k6 run tests/gateway-load-test.js
```
Detaylı test rehberi için [Docs/TESTING_GUIDE.md](docs/performance-reports/TESTING_GUIDE.md) dosyasına bakabilirsiniz.

## 📂 Dokümantasyon
- [Uygulama Rehberi (Implementation Guide)](IMPLEMENTATION_GUIDE.md)
- [Performans Test Raporu](docs/performance-reports/2026-01-14_token-flood-report.md)

---
© 2026 Enterprise Team. All rights reserved.