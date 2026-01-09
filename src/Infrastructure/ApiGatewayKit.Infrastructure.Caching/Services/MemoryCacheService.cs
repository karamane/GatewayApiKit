using ApiGatewayKit.Core.Application.Interfaces.Caching;
using ApiGatewayKit.Core.Application.Interfaces.Logging;
using ApiGatewayKit.Infrastructure.Caching.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiGatewayKit.Infrastructure.Caching.Services;

/// <summary>
/// In-Memory cache servisi implementasyonu
/// Tek sunucu senaryolarÄ± iÃ§in
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ICorrelationContext _correlationContext;
    private readonly CacheOptions _options;

    public MemoryCacheService(
        IMemoryCache cache,
        ILogger<MemoryCacheService> logger,
        ICorrelationContext correlationContext,
        IOptions<CacheOptions> options)
    {
        _cache = cache;
        _logger = logger;
        _correlationContext = correlationContext;
        _options = options.Value;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _logger.LogDebug(
                    "[{CorrelationId}] MemoryCache HIT: {Key}",
                    _correlationContext.CorrelationId, key);
                return Task.FromResult(value);
            }

            _logger.LogDebug(
                "[{CorrelationId}] MemoryCache MISS: {Key}",
                _correlationContext.CorrelationId, key);
            return Task.FromResult<T?>(default);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{CorrelationId}] MemoryCache GET failed for key: {Key}",
                _correlationContext.CorrelationId, key);
            return Task.FromResult<T?>(default);
        }
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new MemoryCacheEntryOptions();

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
            else if (_options.SlidingExpirationMinutes > 0)
            {
                options.SlidingExpiration = TimeSpan.FromMinutes(_options.SlidingExpirationMinutes);
            }

            _cache.Set(key, value, options);

            _logger.LogDebug(
                "[{CorrelationId}] MemoryCache SET: {Key}",
                _correlationContext.CorrelationId, key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{CorrelationId}] MemoryCache SET failed for key: {Key}",
                _correlationContext.CorrelationId, key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _cache.Remove(key);

            _logger.LogDebug(
                "[{CorrelationId}] MemoryCache REMOVE: {Key}",
                _correlationContext.CorrelationId, key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{CorrelationId}] MemoryCache REMOVE failed for key: {Key}",
                _correlationContext.CorrelationId, key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // MemoryCache prefix ile silme desteklemiyor
        // TÃ¼m cache'i silmek yerine logluyoruz
        _logger.LogWarning(
            "[{CorrelationId}] MemoryCache does not support prefix removal. Prefix: {Prefix}",
            _correlationContext.CorrelationId, prefix);

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(_cache.TryGetValue(key, out _));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{CorrelationId}] MemoryCache EXISTS failed for key: {Key}",
                _correlationContext.CorrelationId, key);
            return Task.FromResult(false);
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

    public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        // MemoryCache'de refresh otomatik (sliding expiration ile)
        // Sadece var mÄ± kontrolÃ¼ yapÄ±yoruz
        _ = _cache.TryGetValue(key, out _);
        return Task.CompletedTask;
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


