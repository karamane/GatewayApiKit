using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ApiGatewayKit.Gateway.Services.DownstreamHealth;

/// <summary>
/// Downstream servislerin sağlık kontrolünü yapan servis.
/// Kök dizinine (/) GET isteği atarak JSON yanıtını parse eder ve Status değerini doğrular.
/// </summary>
public sealed class DownstreamHealthChecker : IDownstreamHealthChecker
{
    private readonly ILogger<DownstreamHealthChecker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Sağlık kontrolü için timeout süresi
    /// </summary>
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// "Online" durumu için beklenen değer
    /// </summary>
    private const string ExpectedOnlineStatus = "Online";

    public DownstreamHealthChecker(
        ILogger<DownstreamHealthChecker> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <inheritdoc />
    public async Task<DownstreamHealthStatus> CheckHealthAsync(
        string serviceId,
        string targetSystem,
        Uri baseUrl,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetSystem);
        ArgumentNullException.ThrowIfNull(baseUrl);

        var checkTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(HealthCheckTimeout);

            // Named HttpClient kullan (SSL sertifika hatalarını tolere eder)
            using var client = _httpClientFactory.CreateClient("DownstreamHealthCheck");
            
            // Kök dizinine GET isteği at
            var requestUri = new Uri(baseUrl, "/");
            
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };

            _logger.LogDebug("Sağlık kontrolü başlatılıyor: {ServiceId} -> {Url}", serviceId, requestUri);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token);
            
            stopwatch.Stop();
            var responseTimeMs = stopwatch.ElapsedMilliseconds;

            // Sunucu yanıt verdi - HTTP durumunu kontrol et
            int httpStatusCode = (int)response.StatusCode;
            
            // JSON yanıtını parse etmeyi dene (opsiyonel)
            var content = await response.Content.ReadAsStringAsync(cts.Token);
            var pingResponse = ParsePingResponse(content);
            
            // Status değerini belirle
            string? reportedStatus = pingResponse?.Status;
            bool hasOnlineStatus = string.Equals(reportedStatus, ExpectedOnlineStatus, StringComparison.OrdinalIgnoreCase);
            
            // Sunucu çalışıyor mu? Mantık:
            // - 2xx: Başarılı yanıt = çalışıyor
            // - 3xx: Yönlendirme = çalışıyor
            // - 4xx (401, 403, 404, 405): İstemci hatası ama sunucu ÇALIŞIYOR (erişim kısıtlı)
            // - 5xx: Sunucu hatası = potansiyel sorun
            bool isServerResponding = httpStatusCode < 500;
            
            string effectiveStatus;
            bool isOnline;
            
            if (pingResponse != null && !string.IsNullOrEmpty(reportedStatus))
            {
                // JSON parse edildi, Status alanını kullan
                effectiveStatus = reportedStatus;
                isOnline = hasOnlineStatus;
            }
            else if (response.IsSuccessStatusCode)
            {
                // HTTP 2xx - sunucu çalışıyor
                effectiveStatus = "Çalışıyor";
                isOnline = true;
            }
            else if (httpStatusCode >= 400 && httpStatusCode < 500)
            {
                // HTTP 4xx - sunucu çalışıyor ama endpoint erişilemez (403/404/405 vb.)
                // Bu durum sunucunun "çalıştığını" gösterir - ping endpoint yok ama servis ayakta
                effectiveStatus = "Sunucu Hazır"; // Ana durum: sunucu ayakta
                isOnline = true; // Sunucu ÇALIŞIYOR, sadece endpoint kısıtlı
            }
            else
            {
                // HTTP 5xx - sunucu hatası
                effectiveStatus = "Sunucu Hatası";
                isOnline = false;
            }

            _logger.LogDebug(
                "Sağlık kontrolü tamamlandı: {ServiceId} -> Online: {IsOnline}, Status: {Status}, HTTP: {HttpStatus}, AppId: {AppId}, Version: {Version}",
                serviceId,
                isOnline,
                effectiveStatus,
                httpStatusCode,
                pingResponse?.ApplicationId,
                pingResponse?.Version);

            return new DownstreamHealthStatus
            {
                ServiceId = serviceId,
                TargetSystem = targetSystem,
                BaseUrl = baseUrl.ToString(),
                IsOnline = isOnline,
                Status = effectiveStatus,
                ApplicationId = pingResponse?.ApplicationId,
                Version = pingResponse?.Version,
                LastCheckedUtc = checkTime,
                LastOnlineUtc = isOnline ? checkTime : null,
                ResponseTimeMs = responseTimeMs,
                ErrorMessage = isOnline ? null : $"Beklenen durum '{ExpectedOnlineStatus}', alınan: '{effectiveStatus}'"
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Dış iptal, tekrar fırlat
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("Sağlık kontrolü zaman aşımı: {ServiceId}", serviceId);

            return CreateOfflineStatus(
                serviceId,
                targetSystem,
                baseUrl.ToString(),
                checkTime,
                stopwatch.ElapsedMilliseconds,
                "Zaman aşımı (timeout)");
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Sağlık kontrolü HTTP hatası: {ServiceId}", serviceId);

            return CreateOfflineStatus(
                serviceId,
                targetSystem,
                baseUrl.ToString(),
                checkTime,
                stopwatch.ElapsedMilliseconds,
                $"Bağlantı hatası: {ex.Message}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Sağlık kontrolü beklenmeyen hata: {ServiceId}", serviceId);

            return CreateOfflineStatus(
                serviceId,
                targetSystem,
                baseUrl.ToString(),
                checkTime,
                stopwatch.ElapsedMilliseconds,
                $"Beklenmeyen hata: {ex.Message}");
        }
    }

    private static DownstreamHealthStatus CreateOfflineStatus(
        string serviceId,
        string targetSystem,
        string baseUrl,
        DateTime checkTime,
        long responseTimeMs,
        string errorMessage)
    {
        return new DownstreamHealthStatus
        {
            ServiceId = serviceId,
            TargetSystem = targetSystem,
            BaseUrl = baseUrl,
            IsOnline = false,
            Status = "Offline",
            LastCheckedUtc = checkTime,
            ResponseTimeMs = responseTimeMs,
            ErrorMessage = errorMessage
        };
    }

    private DownstreamPingResponse? ParsePingResponse(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<DownstreamPingResponse>(json, options);
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Ping yanıtı JSON parse hatası");
            return null;
        }
    }
}
