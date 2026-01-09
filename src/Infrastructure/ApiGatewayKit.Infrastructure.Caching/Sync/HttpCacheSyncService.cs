using System.Text;
using System.Text.Json;
using ApiGatewayKit.Core.Application.Interfaces.Caching;
using ApiGatewayKit.Infrastructure.Caching.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiGatewayKit.Infrastructure.Caching.Sync;

public class HttpCacheSyncService : ICacheSyncService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly CacheProviderOptions _options;
    private readonly ILogger<HttpCacheSyncService> _logger;

    public HttpCacheSyncService(
        IHttpClientFactory httpFactory,
        IOptions<CacheProviderOptions> options,
        ILogger<HttpCacheSyncService> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(string key, string? value, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_options.PeerApiUrl)) return;

        try
        {
            var message = new CacheSyncMessage
            {
                Key = key,
                Value = value,
                Action = CacheSyncAction.Set,
                Source = _options.AppId
            };
            var json = JsonSerializer.Serialize(message);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var http = _httpFactory.CreateClient("CacheSync");
            await http.PostAsync("api/cache/sync", content, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache sync failed for key: {Key}", key);
        }
    }

    public async Task PublishRemoveAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_options.PeerApiUrl)) return;

        try
        {
            var message = new CacheSyncMessage
            {
                Key = key,
                Action = CacheSyncAction.Remove,
                Source = _options.AppId
            };
            var json = JsonSerializer.Serialize(message);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var http = _httpFactory.CreateClient("CacheSync");
            await http.PostAsync("api/cache/sync", content, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache sync remove failed for key: {Key}", key);
        }
    }
}



