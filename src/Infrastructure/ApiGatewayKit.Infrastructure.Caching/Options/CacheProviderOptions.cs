namespace ApiGatewayKit.Infrastructure.Caching.Options;

public class CacheProviderOptions
{
    public const string SectionName = "CacheProvider";

    public CacheProviderType Provider { get; set; } = CacheProviderType.Redis;
    public string ConnectionString { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = "ApiGatewayKit:";
    public string AppId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public int DefaultTtlMinutes { get; set; } = 30;
    public string SyncChannel { get; set; } = "cache-updates";
    public bool EnablePreload { get; set; } = true;
    public string? PeerApiUrl { get; set; }
    public int MemorySizeLimitMb { get; set; } = 100;
}

public enum CacheProviderType
{
    Memory,
    Redis
}



