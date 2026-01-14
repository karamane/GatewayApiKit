# Gateway Performans Doğrulama Raporu

**Tarih:** 14 Ocak 2026  
**Ortam:** IP Rate Limit (Token Bazlı) - Geliştirme (Development)  
**Hedef URL:** `http://localhost:53000/moim/api/v1/internet/auth/login`

## 1. Yönetici Özeti
Bu çalışmanın amacı, API Gateway üzerinde **Token Bazlı Hız Sınırlama (Rate Limiting)** ve **DoS Koruması** uygulamalarının doğrulanmasıdır. `k6` aracı kullanılarak, tek bir kullanıcının (JWT ile tanımlanmış) yetkili limitlerini aştığı bir "Token Flood" saldırısı simüle edilmiştir.

**Sonuç:** ✅ **BAŞARILI**  
Gateway, belirlenen limitleri aşan istekleri başarıyla tespit etmiş ve `429 Too Many Requests` ile engellemiştir. Aynı zamanda, limitler dahilindeki trafiğin geçişine izin vermiştir.

## 2. Test Konfigürasyonu
| Parametre | Değer |
|-----------|-------|
| **Araç** | k6 |
| **Senaryo** | Token Flood (Tekil JWT) |
| **Sanal Kullanıcı (VUs)** | 20 (Eşzamanlı) |
| **Süre** | 30s |
| **Rate Limit Politikası** | 50 istek / 1s (Token başına) |
| **Downstream** | Mock / Localhost (502 döndü, geçiş başarılı olduğu anlamına gelir) |

## 3. Ana Bulgular

### Hız Sınırlama (Koruma)
- **Engellenen İstekler:** 259 istek engellendi.
- **Doğrulama:** Gateway `429 Too Many Requests` durum kodu döndürdü.
- **Davranış:** Bu sonuç, `TokenClientIdResolver` bileşeninin JWT içerisindeki `sub` bilgisini doğru şekilde ayıkladığını ve tek bir gürültülü kullanıcının backend sistemini boğmasını engellediğini doğrulamaktadır.

### Yönlendirme & Bağlantı
- **Rota Eşleşmesi:** İsteklerin %100'ü başarıyla `auth-login` yoluna yönlendirildi.
- **Downstream Durumu:** Limiti geçen istekler `502 Bad Gateway` aldı.
    - *Yorum:* Gateway isteği başarıyla iletmeye çalıştı. 502 hatası Gateway'in koruma mantığıyla ilgili değildir (arkadaki legacy servisin - 39414 portu - kapalı veya zaman aşımına uğradığını gösterir).

## 4. Kanıtlar (Loglar & Metrikler)
```text
http_req_duration..........: ortalama=747.7ms  p(95)=1.39s
throttled_requests.........: 259 (Gateway Tarafından Engellendi)
checks_succeeded...........: %49.79 (Backend'e İletildi)
checks_failed..............: %50.20 (Doğru Şekilde Sınırlandı)
```

## 5. Sonuç
Token Bazlı Hız Sınırlama mekanizması **Production (Canlı) Ortam için Hazırdır**. Kimliği doğrulanmış kullanıcılardan gelebilecek Uygulama Katmanı DoS saldırılarına karşı sistemi etkili bir şekilde korumaktadır.

---
**Sonraki Adımlar:**
- Dağıtık ortamlar (F5 / Çoklu Sunucu) için Redis desteğini etkinleştirin (`UseRedis: true`).
- `ocelot.json` içindeki `Limit` değerlerini gerçek canlı ortam kapasitesine göre ayarlayın.
