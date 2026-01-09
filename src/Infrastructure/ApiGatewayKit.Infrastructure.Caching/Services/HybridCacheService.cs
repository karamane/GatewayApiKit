using ApiGatewayKit.Core.Application.Interfaces.Caching;
using ApiGatewayKit.Core.Application.Interfaces.Logging;
using ApiGatewayKit.Infrastructure.Caching.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ApiGatewayKit.Infrastructure.Caching.Services;

/// <summary>
/// Hybrid cache servisi - L1: Memory, L2: Redis
/// Maksimum performans iÃ§in iki katmanlÄ± cache
/// </summary>
public class HybridCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<HybridCacheService> _logger;
    private readonly ICorrelationContext _correlationContext;
    private readonly CacheOptions _options;

    // L1 cache iÃ§in kÄ±sa TTL
    private static readonly TimeSpan L1DefaultExpiration = TimeSpan.FromMinutes(5);

    public HybridCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<HybridCacheService> logger,
        ICorrelationContext correlationContext,
        IOptions<CacheOptions> options)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _correlationContext = correlationContext;
        _options = options.Value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // L1: Memory cache'e bak
        if (_memoryCache.TryGetValue(key, out T? l1Value))
        {
            _logger.LogDebug(
                "[{CorrelationId}] HybridCache L1 HIT: {Key}",
                _correlationContext.CorrelationId, key);
            return l1Value;
        }

        // L2: Redis'e bak
        try
        {
            var data = await _distributedCache.GetStringAsync(key, cancellationToken);

            if (!string.IsNullOrEmpty(data))
            {
                var l2Value = JsonSerializer.Deserialize<T>(data);

                // L2'de bulundu, L1'e de ekle
                if (l2Value != null)
                {
                    _memoryCache.Set(key, l2Value, L1DefaultExpiration);

                    _logger.LogDebug(
                        "[{CorrelationId}] HybridCache L2 HIT (promoted to L1): {Key}",
                        _correlationContext.CorrelationId, key);
                }

                return l2Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{CorrelationId}] HybridCache L2 GET failed for key: {Key}",
                _correlationContext.CorrelationId, key);
        }

        _logger.LogDebug(
            "[{CorrelationId}] HybridCache MISS: {Key}",
            _correlationContext.CorrelationId, key);

        return default;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default)
    {
        var l2Expiration = absoluteExpiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

        // L1: Memory cache'e ekle (kÄ±sa TTL)
        var l1Expiration = l2Expiration < L1DefaultExpiration ? l2Expiration : L1DefaultExpiration;
        _memoryCache.Set(key, value, l1Expiration);

        // L2: Redis'e ekle
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = l2Expiration
            };

            if (slidingExpiration.HasValue)
            {
                options.SlidingExpiration = slidingExpiration;
            }

            var data = JsonSerializer.Serialize(value);
            await _distributedCache.SetStringAsync(key, data, options, cancellationToken);

            _logger.LogDebug(
                "[{CorrelationId}] HybridCache SET (L1 + L2): {Key}",
                _correlationContext.CorrelationId, key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{CorrelationId}] HybridCache L2 SET failed for key: {Key}",
                _correlationContext.CorrelationId, key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        // L1'den sil
        _memoryCache.Remove(key);

        // L2'den sil
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);

            _logger.LogDebug(
                "[{CorrelationId}] HybridCache REMOVE (L1 + L2): {Key}",
                _correlationContext.CorrelationId, key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{CorrelationId}] HybridCache L2 REMOVE failed for key: {Key}",
                _correlationContext.CorrelationId, key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // L1: Memory cache prefix silme desteklemiyor
        _logger.LogWarning(
            "[{CorrelationId}] HybridCache L1 does not support prefix removal",
            _correlationContext.CorrelationId);

        // L2: Redis iÃ§in RedisCacheService kullanÄ±lmalÄ±
        // Bu implementasyonda sadece logluyoruz
        _logger.LogWarning(
            "[{CorrelationId}] HybridCache prefix removal requires Redis connection. Prefix: {Prefix}",
            _correlationContext.CorrelationId, prefix);

        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        // L1'de var mÄ±?
        if (_memoryCache.TryGetValue(key, out _))
        {
            return true;
        }

        // L2'de var mÄ±?
        try
        {
            var data = await _distributedCache.GetAsync(key, cancellationToken);
            return data != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);

        if (cached != null)
        {
            return cached;
        }

        var value = await factory();

        await SetAsync(key, value, absoluteExpiration, slidingExpiration, cancellationToken);

        return value;
    }

    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        // L1'de refresh (get ile otomatik)
        _memoryCache.TryGetValue(key, out _);

        // L2'de refresh
        try
        {
            await _distributedCache.RefreshAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{CorrelationId}] HybridCache L2 REFRESH failed for key: {Key}",
                _correlationContext.CorrelationId, key);
        }
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, T?>();

        foreach (var key in keys)
        {
            result[key] = await GetAsync<T>(key, cancellationToken);
        }

        return result;
    }

    public async Task SetManyAsync<T>(
        Dictionary<string, T> items,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var kvp in items)
        {
            await SetAsync(kvp.Key, kvp.Value, absoluteExpiration, cancellationToken: cancellationToken);
        }
    }
}


