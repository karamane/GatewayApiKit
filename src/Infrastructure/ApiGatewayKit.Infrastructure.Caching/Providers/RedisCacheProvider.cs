using ApiGatewayKit.Core.Application.Interfaces.Caching;
using ApiGatewayKit.Infrastructure.Caching.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;

namespace ApiGatewayKit.Infrastructure.Caching.Providers;

public class RedisCacheProvider : ICacheProvider
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ICacheSyncService _sync;
    private readonly CacheProviderOptions _options;
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheProvider>? _logger;

    // Newtonsoft.Json ayarlarÄ± - camelCase serialization (Gateway modelleriyle uyumlu)
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Include
    };

    public RedisCacheProvider(
        IConnectionMultiplexer redis,
        ICacheSyncService sync,
        IOptions<CacheProviderOptions> options,
        ILogger<RedisCacheProvider>? logger = null)
    {
        _redis = redis;
        _sync = sync;
        _options = options.Value;
        _db = _redis.GetDatabase();
        _logger = logger;
    }

    /// <summary>
    /// Cache'den veri okur. Deserialize hatasÄ± olursa eski veriyi siler ve null dÃ¶ndÃ¼rÃ¼r.
    /// Bu sayede uyumsuz cache verileri otomatik temizlenir.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var fullKey = $"{_options.InstanceName}{key}";
        
        try
        {
            var value = await _db.StringGetAsync(fullKey);
            if (!value.HasValue)
                return default;

            return JsonConvert.DeserializeObject<T>(value.ToString(), JsonSettings);
        }
        catch (JsonException ex)
        {
            // Eski/uyumsuz format - otomatik temizle
            _logger?.LogWarning(
                "Cache deserialize hatasÄ±, eski veri temizleniyor: {Key} - {Error}", 
                key, ex.Message);
            
            await _db.KeyDeleteAsync(fullKey);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var fullKey = $"{_options.InstanceName}{key}";
        var json = JsonConvert.SerializeObject(value, JsonSettings);
        var expiry = ttl ?? TimeSpan.FromMinutes(_options.DefaultTtlMinutes);
        await _db.StringSetAsync(fullKey, json, expiry);
        await _sync.PublishAsync(key, json, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        var fullKey = $"{_options.InstanceName}{key}";
        await _db.KeyDeleteAsync(fullKey);
        await _sync.PublishRemoveAsync(key, ct);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var fullKey = $"{_options.InstanceName}{key}";
        return await _db.KeyExistsAsync(fullKey);
    }

    public async Task<IEnumerable<string>> GetAllKeysAsync(string? pattern = null, CancellationToken ct = default)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var searchPattern = $"{_options.InstanceName}{pattern ?? "*"}";
        var keys = server.Keys(pattern: searchPattern);
        var prefix = _options.InstanceName;
        return keys.Select(k => k.ToString().Replace(prefix, "")).ToList();
    }

    public async Task SetLocalAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var fullKey = $"{_options.InstanceName}{key}";
        var json = JsonConvert.SerializeObject(value, JsonSettings);
        var expiry = ttl ?? TimeSpan.FromMinutes(_options.DefaultTtlMinutes);
        await _db.StringSetAsync(fullKey, json, expiry);
    }

    public async Task RemoveLocalAsync(string key, CancellationToken ct = default)
    {
        var fullKey = $"{_options.InstanceName}{key}";
        await _db.KeyDeleteAsync(fullKey);
    }
}


