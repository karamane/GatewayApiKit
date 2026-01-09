using System.Text.Json;
using ApiGatewayKit.Core.Application.Interfaces.Caching;
using ApiGatewayKit.Core.Application.Interfaces.Logging;
using ApiGatewayKit.Infrastructure.Caching.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ApiGatewayKit.Infrastructure.Caching.Services;

/// <summary>
/// Redis distributed cache servisi implementasyonu
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly ICorrelationContext _correlationContext;
    private readonly CacheOptions _options;

    public RedisCacheService(
        IDistributedCache cache,
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger,
        ICorrelationContext correlationContext,
        IOptions<CacheOptions> options)
    {
        _cache = cache;
        _redis = redis;
        _logger = logger;
        _correlationContext = correlationContext;
        _options = options.Value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(data))
            {
                _logger.LogDebug(
                    "[{CorrelationId}] Cache MISS: {Key}",
                    _correlationContext.CorrelationId, key);
                return default;
            }

            _logger.LogDebug(
                "[{CorrelationId}] Cache HIT: {Key}",
                _correlationContext.CorrelationId, key);

            return JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{CorrelationId}] Cache GET failed for key: {Key}",
                _correlationContext.CorrelationId, key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions();

            if (absoluteExpiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = absoluteExpiration;
            }
            else
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);
            }

            if (slidingExpiration.HasValue)
            {
                options.SlidingExpiration = slidingExpiration;
            }

            var data = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, data, options, cancellationToken);

            _logger.LogDebug(
                "[{CorrelationId}] Cache SET: {Key}",
                _correlationContext.CorrelationId, key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{CorrelationId}] Cache SET failed for key: {Key}",
                _correlationContext.CorrelationId, key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);

            _logger.LogDebug(
                "[{CorrelationId}] Cache REMOVE: {Key}",
                _correlationContext.CorrelationId, key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{CorrelationId}] Cache REMOVE failed for key: {Key}",
                _correlationContext.CorrelationId, key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{prefix}*").ToArray();

            if (keys.Length > 0)
            {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(keys);

                _logger.LogDebug(
                    "[{CorrelationId}] Cache REMOVE by prefix: {Prefix}, Count: {Count}",
                    _correlationContext.CorrelationId, prefix, keys.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{CorrelationId}] Cache REMOVE by prefix failed: {Prefix}",
                _correlationContext.CorrelationId, prefix);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _cache.GetAsync(key, cancellationToken);
            return data != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{CorrelationId}] Cache EXISTS failed for key: {Key}",
                _correlationContext.CorrelationId, key);
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
        try
        {
            await _cache.RefreshAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{CorrelationId}] Cache REFRESH failed for key: {Key}",
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
        var tasks = items.Select(kvp =>
            SetAsync(kvp.Key, kvp.Value, absoluteExpiration, cancellationToken: cancellationToken));

        await Task.WhenAll(tasks);
    }
}


