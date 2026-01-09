using ApiGatewayKit.Core.Application.Interfaces.Caching;
using ApiGatewayKit.Infrastructure.Caching.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiGatewayKit.Infrastructure.Caching.Services;

public class CacheResetService : ICacheResetService
{
    private readonly ICacheProvider _cache;
    private readonly IEnumerable<ICachePreloadProvider> _preloadProviders;
    private readonly CacheProviderOptions _options;
    private readonly ILogger<CacheResetService> _logger;

    public CacheResetService(
        ICacheProvider cache,
        IEnumerable<ICachePreloadProvider> preloadProviders,
        IOptions<CacheProviderOptions> options,
        ILogger<CacheResetService> logger)
    {
        _cache = cache;
        _preloadProviders = preloadProviders;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Cache reset starting...");
        
        var keys = await _cache.GetAllKeysAsync(ct: ct);
        foreach (var key in keys)
        {
            await _cache.RemoveAsync(key, ct);
        }
        
        _logger.LogInformation("Cache cleared. Starting preload...");
        await PreloadAsync(ct);
    }

    public async Task PreloadAsync(CancellationToken ct = default)
    {
        if (!_options.EnablePreload)
        {
            _logger.LogInformation("Cache preload disabled");
            return;
        }

        foreach (var provider in _preloadProviders)
        {
            try
            {
                var data = await provider.GetPreloadDataAsync(ct);
                foreach (var (key, value) in data)
                {
                    await _cache.SetAsync(key, value, ct: ct);
                }
                _logger.LogInformation("Preloaded {Count} items from {Provider}", 
                    data.Count, provider.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Preload failed for {Provider}", provider.GetType().Name);
            }
        }
    }
}


