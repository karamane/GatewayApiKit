using System.Text.Json;
using ApiGatewayKit.Core.Application.Interfaces.Caching;
using ApiGatewayKit.Infrastructure.Caching.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ApiGatewayKit.Infrastructure.Caching.Sync;

public class RedisCacheSyncService : ICacheSyncService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly CacheProviderOptions _options;

    public RedisCacheSyncService(
        IConnectionMultiplexer redis,
        IOptions<CacheProviderOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    public async Task PublishAsync(string key, string? value, CancellationToken ct = default)
    {
        var message = JsonSerializer.Serialize(new CacheSyncMessage
        {
            Key = key,
            Value = value,
            Action = CacheSyncAction.Set,
            Source = _options.AppId
        });
        await _redis.GetSubscriber().PublishAsync(RedisChannel.Literal(_options.SyncChannel), message);
    }

    public async Task PublishRemoveAsync(string key, CancellationToken ct = default)
    {
        var message = JsonSerializer.Serialize(new CacheSyncMessage
        {
            Key = key,
            Action = CacheSyncAction.Remove,
            Source = _options.AppId
        });
        await _redis.GetSubscriber().PublishAsync(RedisChannel.Literal(_options.SyncChannel), message);
    }
}

public class CacheSyncMessage
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public CacheSyncAction Action { get; set; }
    public string Source { get; set; } = string.Empty;
}

public enum CacheSyncAction
{
    Set,
    Remove
}


