namespace ApiGatewayKit.Infrastructure.Caching.Options;

/// <summary>
/// Cache yapÄ±landÄ±rma seÃ§enekleri
/// Redis ve MemoryCache arasÄ±nda switch yapÄ±labilir
/// </summary>
public class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Cache provider tipi (Redis, Memory)
    /// </summary>
    public CacheProvider Provider { get; set; } = CacheProvider.Memory;

    /// <summary>
    /// Redis connection string (Provider = Redis ise)
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Redis instance adÄ±
    /// </summary>
    public string InstanceName { get; set; } = "ApiGatewayKit_";

    /// <summary>
    /// VarsayÄ±lan TTL (dakika)
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Sliding expiration varsayÄ±lan (dakika)
    /// </summary>
    public int SlidingExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Parametrelerin TTL (dakika)
    /// </summary>
    public int ParameterExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Memory cache size limit (MB) - Provider = Memory ise
    /// </summary>
    public int MemoryCacheSizeLimitMb { get; set; } = 100;

    /// <summary>
    /// Memory cache compaction percentage (0-1)
    /// </summary>
    public double CompactionPercentage { get; set; } = 0.25;

    /// <summary>
    /// Redis retry count
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Redis connect timeout (ms)
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Redis sync timeout (ms)
    /// </summary>
    public int SyncTimeoutMs { get; set; } = 1000;

    /// <summary>
    /// Fallback to memory cache on Redis failure
    /// </summary>
    public bool FallbackToMemory { get; set; } = true;
}

/// <summary>
/// Cache provider tipleri
/// </summary>
public enum CacheProvider
{
    /// <summary>
    /// In-memory cache (tek sunucu iÃ§in)
    /// </summary>
    Memory,

    /// <summary>
    /// Redis distributed cache (multi-server iÃ§in)
    /// </summary>
    Redis,

    /// <summary>
    /// Hybrid: Memory + Redis (performans iÃ§in)
    /// L1: Memory, L2: Redis
    /// </summary>
    Hybrid
}

