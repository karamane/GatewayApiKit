# Gateway Yük Testi Kılavuzu

Bu kılavuz, [k6](https://k6.io/) kullanarak Gateway üzerinde performans ve güvenlik testlerinin nasıl çalıştırılacağını açıklar.

## Ön Gereksinimler
1. **k6 Kurulumu**:
   - **Windows (Winget):** `winget install k6`
   - **Chocolatey:** `choco install k6`
   - **İndirme:** [https://k6.io/docs/get-started/installation/](https://k6.io/docs/get-started/installation/)
2. **Gateway Çalışır Durumda Olmalı**: `ApiGatewayKit.Gateway` projesinin `http://localhost:53000` adresinde çalıştığından emin olun.

## Test Scriptleri Konumu
Tüm test scriptleri projenin `tests/` klasöründe bulunur.

- `tests/gateway-load-test.js`: Token Flood & Yük testi için ana script.

## Doğrulanmış Testler Nasıl Çalıştırılır

### 1. Token Flood Saldırısı (Rate Limit Doğrulama)
Bu senaryo, tek bir kullanıcının (tek bir JWT token kullanarak) izin verilenden daha hızlı istek göndermesini simüle eder.

**Komut:**
```powershell
k6 run tests/gateway-load-test.js
```

**Beklenen Çıktı:**
- `throttled_requests` sayacının arttığını görmelisiniz.
- `http_req_failed` oranı yüksek olabilir (bu iyidir, 429 hatalarının alındığını gösterir).
- Konsol loglarında "Status 429" görülebilir.

### 2. Testi Özelleştirme
`tests/gateway-load-test.js` dosyasını açarak şunları değiştirebilirsiniz:
- **`VUS`**: Sanal Kullanıcı sayısı (Eşzamanlılık).
- **`DURATION`**: Testin ne kadar süreceği.
- **`TOKEN`**: Kimlik doğrulama için kullanılan JWT token (geçerli olduğundan ve süresinin dolmadığından emin olun).
- **Payload**: POST isteklerinde gönderilen JSON gövdesi.

## Sonuçları Yorumlama

| Metrik | Anlamı | İdeal Değer |
|--------|--------|-------------|
| `throttled_requests` | 429 yanıtlarının sayısı | **> 0** (Limit testi yapılıyorsa) |
| `http_req_duration` | Gecikme (Yanıt süresi) | **< 2000ms** (p95) |
| `checks_succeeded` | Backend'e iletilen istekler | Limite bağlıdır |

## Sorun Giderme
- **Hepsi 401 mi?** Script içindeki JWT token süresinin dolup dolmadığını kontrol edin.
- **Hepsi 404 mü?** Script içindeki `BASE_URL` ile `ocelot.json` yollarının eşleştiğini kontrol edin.
- **Bağlantı Reddedildi (Connection Refused)?** Gateway'in `53000` portunda çalıştığından emin olun.
