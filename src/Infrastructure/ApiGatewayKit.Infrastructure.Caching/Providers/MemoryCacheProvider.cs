using System.Collections.Concurrent;
using System.Text.Json;
using ApiGatewayKit.Core.Application.Interfaces.Caching;
using ApiGatewayKit.Infrastructure.Caching.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ApiGatewayKit.Infrastructure.Caching.Providers;

public class MemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _cache;
    private readonly ICacheSyncService _sync;
    private readonly CacheProviderOptions _options;
    private static readonly ConcurrentDictionary<string, byte> _keys = new();

    public MemoryCacheProvider(
        IMemoryCache cache,
        ICacheSyncService sync,
        IOptions<CacheProviderOptions> options)
    {
        _cache = cache;
        _sync = sync;
        _options = options.Value;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var fullKey = $"{_options.InstanceName}{key}";
        _cache.TryGetValue(fullKey, out T? value);
        return Task.FromResult(value);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var fullKey = $"{_options.InstanceName}{key}";
        var expiry = ttl ?? TimeSpan.FromMinutes(_options.DefaultTtlMinutes);
        _cache.Set(fullKey, value, expiry);
        _keys.TryAdd(fullKey, 0);
        var json = JsonSerializer.Serialize(value);
        await _sync.PublishAsync(key, json, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        var fullKey = $"{_options.InstanceName}{key}";
        _cache.Remove(fullKey);
        _keys.TryRemove(fullKey, out _);
        await _sync.PublishRemoveAsync(key, ct);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var fullKey = $"{_options.InstanceName}{key}";
        return Task.FromResult(_cache.TryGetValue(fullKey, out _));
    }

    public Task<IEnumerable<string>> GetAllKeysAsync(string? pattern = null, CancellationToken ct = default)
    {
        var prefix = _options.InstanceName;
        var result = _keys.Keys
            .Where(k => pattern == null || k.Contains(pattern))
            .Select(k => k.Replace(prefix, ""));
        return Task.FromResult(result);
    }

    public Task SetLocalAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var fullKey = $"{_options.InstanceName}{key}";
        var expiry = ttl ?? TimeSpan.FromMinutes(_options.DefaultTtlMinutes);
        _cache.Set(fullKey, value, expiry);
        _keys.TryAdd(fullKey, 0);
        return Task.CompletedTask;
    }

    public Task RemoveLocalAsync(string key, CancellationToken ct = default)
    {
        var fullKey = $"{_options.InstanceName}{key}";
        _cache.Remove(fullKey);
        _keys.TryRemove(fullKey, out _);
        return Task.CompletedTask;
    }
}








