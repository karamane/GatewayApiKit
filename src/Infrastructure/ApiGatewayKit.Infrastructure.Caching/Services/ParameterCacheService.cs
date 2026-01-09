using ApiGatewayKit.Core.Application.Interfaces.Caching;
using ApiGatewayKit.Core.Application.Interfaces.Logging;
using ApiGatewayKit.Core.Shared.Constants;
using ApiGatewayKit.Infrastructure.Caching.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ApiGatewayKit.Infrastructure.Caching.Services;

/// <summary>
/// Parametre cache servisi implementasyonu
/// Uygulama parametrelerini cache'te yÃ¶netir
/// Redis kullanÄ±ldÄ±ÄŸÄ±nda Multi-server deployment iÃ§in Pub/Sub desteÄŸi
/// </summary>
public class ParameterCacheService : IParameterCacheService
{
    private readonly ICacheService _cacheService;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<ParameterCacheService> _logger;
    private readonly ICorrelationContext _correlationContext;
    private readonly CacheOptions _options;
    private readonly bool _isRedisEnabled;

    public ParameterCacheService(
        ICacheService cacheService,
        ILogger<ParameterCacheService> logger,
        ICorrelationContext correlationContext,
        IOptions<CacheOptions> options,
        IConnectionMultiplexer? redis = null)
    {
        _cacheService = cacheService;
        _redis = redis;
        _logger = logger;
        _correlationContext = correlationContext;
        _options = options.Value;
        _isRedisEnabled = _options.Provider == CacheProvider.Redis || _options.Provider == CacheProvider.Hybrid;

        // Redis varsa parametre deÄŸiÅŸiklik event'ini dinle
        if (_isRedisEnabled && _redis != null)
        {
            SubscribeToParameterChanges();
        }
    }

    public async Task<T?> GetParameterAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetParameterCacheKey(key);
        return await _cacheService.GetAsync<T>(cacheKey, cancellationToken);
    }

    public async Task<T> GetParameterAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        var value = await GetParameterAsync<T>(key, cancellationToken);
        return value ?? defaultValue;
    }

    public async Task SetParameterAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetParameterCacheKey(key);

        // Parametreler uzun sÃ¼re cache'de kalÄ±r
        await _cacheService.SetAsync(
            cacheKey,
            value,
            TimeSpan.FromMinutes(CacheConstants.ParameterExpirationMinutes),
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "[{CorrelationId}] Parameter SET: {Key}",
            _correlationContext.CorrelationId, key);
    }

    public async Task RefreshAllParametersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[{CorrelationId}] Refreshing all parameters",
            _correlationContext.CorrelationId);

        // TÃ¼m parametre cache'lerini temizle
        await _cacheService.RemoveByPrefixAsync(CacheConstants.ParametersCacheKey, cancellationToken);

        // Burada veritabanÄ±ndan parametreleri yeniden yÃ¼kleyebilirsiniz
        // await LoadParametersFromDatabaseAsync(cancellationToken);
    }

    public async Task RefreshParameterAsync(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetParameterCacheKey(key);
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);

        _logger.LogInformation(
            "[{CorrelationId}] Parameter REFRESH: {Key}",
            _correlationContext.CorrelationId, key);
    }

    public async Task NotifyParameterChangedAsync(string key, CancellationToken cancellationToken = default)
    {
        // Redis yoksa Pub/Sub kullanÄ±lamaz
        if (!_isRedisEnabled || _redis == null)
        {
            _logger.LogDebug(
                "[{CorrelationId}] Parameter change notification skipped (Redis not enabled): {Key}",
                _correlationContext.CorrelationId, key);
            return;
        }

        try
        {
            var subscriber = _redis.GetSubscriber();
            await subscriber.PublishAsync(
                RedisChannel.Literal(CacheConstants.ParameterUpdateChannel),
                key);

            _logger.LogInformation(
                "[{CorrelationId}] Parameter change notification sent: {Key}",
                _correlationContext.CorrelationId, key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{CorrelationId}] Failed to notify parameter change: {Key}",
                _correlationContext.CorrelationId, key);
        }
    }

    public async Task<Dictionary<string, object>> GetAllParametersAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheConstants.ParametersCacheKey}:all";
        var cached = await _cacheService.GetAsync<Dictionary<string, object>>(cacheKey, cancellationToken);

        if (cached != null)
        {
            return cached;
        }

        // Cache'de yoksa boÅŸ dictionary dÃ¶n
        // GerÃ§ek implementasyonda veritabanÄ±ndan yÃ¼klenebilir
        return new Dictionary<string, object>();
    }

    /// <summary>
    /// Redis Pub/Sub ile parametre deÄŸiÅŸikliklerini dinler
    /// </summary>
    private void SubscribeToParameterChanges()
    {
        if (_redis == null) return;

        try
        {
            var subscriber = _redis.GetSubscriber();

            subscriber.Subscribe(
                RedisChannel.Literal(CacheConstants.ParameterUpdateChannel),
                async (channel, message) =>
                {
                    var key = message.ToString();

                    _logger.LogInformation(
                        "Received parameter change notification: {Key}",
                        key);

                    // Local cache'i refresh et
                    await RefreshParameterAsync(key);
                });

            _logger.LogInformation("Subscribed to parameter change notifications");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to subscribe to parameter changes");
        }
    }

    private static string GetParameterCacheKey(string key)
    {
        return $"{CacheConstants.ParametersCacheKey}:{key}";
    }
}

