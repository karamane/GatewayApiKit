using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace ApiGatewayKit.Gateway.Services;

public sealed class GatewayNodeHealthChecker : IGatewayNodeHealthChecker
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GatewayNodeHealthChecker> _logger;

    public GatewayNodeHealthChecker(IHttpClientFactory httpClientFactory, ILogger<GatewayNodeHealthChecker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<NodeHealthResult> CheckAsync(Uri baseUrl, CancellationToken cancellationToken)
    {
        if (!baseUrl.IsAbsoluteUri)
        {
            return new NodeHealthResult(Ready: false, StatusCode: null, Hint: "Sunucu erişilemiyor - geçersiz adres");
        }

        Uri probeUri = new(baseUrl, "/");

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ProbeTimeout);

            // Ignore SSL errors for health probes (common in legacy systems)
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            using HttpClient client = new HttpClient(handler);
            client.Timeout = Timeout.InfiniteTimeSpan;

            using var head = new HttpRequestMessage(HttpMethod.Head, probeUri);
            head.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };

            using HttpResponseMessage headResponse = await client.SendAsync(
                head,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);

            int statusCode = (int)headResponse.StatusCode;

            // 2xx/3xx/4xx = sunucu yanıt veriyor, yani ayakta
            // 5xx = sunucu hatası ama yine de "erişilebilir"
            return new NodeHealthResult(Ready: true, StatusCode: statusCode, Hint: "Sunucu hazır - trafik yönlendirilebilir");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "Node probe failed for {ProbeUri}", probeUri);
            return new NodeHealthResult(Ready: false, StatusCode: null, Hint: "Sunucu erişilemiyor - bağlantı hatası");
        }
        catch (TaskCanceledException)
        {
            return new NodeHealthResult(Ready: false, StatusCode: null, Hint: "Sunucu erişilemiyor - zaman aşımı");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Node probe failed for {ProbeUri}", probeUri);
            return new NodeHealthResult(Ready: false, StatusCode: null, Hint: "Sunucu erişilemiyor - beklenmeyen hata");
        }
    }
}


