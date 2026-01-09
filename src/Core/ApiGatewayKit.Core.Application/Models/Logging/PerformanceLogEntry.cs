using ApiGatewayKit.Core.Shared.Constants;

namespace ApiGatewayKit.Core.Application.Models.Logging;

/// <summary>
/// Performance log entry
/// Performans metrikleri iÃ§in
/// </summary>
public class PerformanceLogEntry : BaseLogEntry
{
    public PerformanceLogEntry()
    {
        LogType = LogConstants.LogTypes.Performance;
    }

    /// <summary>
    /// Ä°ÅŸlem adÄ±
    /// </summary>
    public string? OperationName { get; set; }

    /// <summary>
    /// Toplam sÃ¼re (ms)
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// YavaÅŸ request eÅŸiÄŸi aÅŸÄ±ldÄ± mÄ±?
    /// </summary>
    public bool IsSlowRequest { get; set; }

    /// <summary>
    /// YavaÅŸ request eÅŸiÄŸi (ms)
    /// </summary>
    public long? SlowRequestThresholdMs { get; set; }

    /// <summary>
    /// Ä°ÅŸlem tipi (HttpAction, MediatR, Database, Cache, ExternalService)
    /// </summary>
    public string? OperationType { get; set; }

    /// <summary>
    /// Ä°ÅŸlem baÅŸarÄ±lÄ± mÄ±?
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Ek metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    #region DetaylÄ± Metrikler

    /// <summary>
    /// CPU sÃ¼resi (ms)
    /// </summary>
    public long? CpuTimeMs { get; set; }

    /// <summary>
    /// Bellek kullanÄ±mÄ± (bytes)
    /// </summary>
    public long? MemoryUsedBytes { get; set; }

    /// <summary>
    /// Allocation sayÄ±sÄ±
    /// </summary>
    public long? AllocationCount { get; set; }

    /// <summary>
    /// Database sÃ¼releri
    /// </summary>
    public DatabaseMetrics? Database { get; set; }

    /// <summary>
    /// Cache sÃ¼releri
    /// </summary>
    public CacheMetrics? Cache { get; set; }

    /// <summary>
    /// External service sÃ¼releri
    /// </summary>
    public ExternalServiceMetrics? ExternalServices { get; set; }

    #endregion
}

/// <summary>
/// Database performans metrikleri
/// </summary>
public class DatabaseMetrics
{
    /// <summary>
    /// Toplam sorgu sayÄ±sÄ±
    /// </summary>
    public int QueryCount { get; set; }

    /// <summary>
    /// Toplam sorgu sÃ¼resi (ms)
    /// </summary>
    public long TotalDurationMs { get; set; }

    /// <summary>
    /// En yavaÅŸ sorgu sÃ¼resi (ms)
    /// </summary>
    public long? SlowestQueryMs { get; set; }

    /// <summary>
    /// Connection wait sÃ¼resi (ms)
    /// </summary>
    public long? ConnectionWaitMs { get; set; }
}

/// <summary>
/// Cache performans metrikleri
/// </summary>
public class CacheMetrics
{
    /// <summary>
    /// Cache hit sayÄ±sÄ±
    /// </summary>
    public int HitCount { get; set; }

    /// <summary>
    /// Cache miss sayÄ±sÄ±
    /// </summary>
    public int MissCount { get; set; }

    /// <summary>
    /// Hit ratio
    /// </summary>
    public double HitRatio => HitCount + MissCount > 0 
        ? (double)HitCount / (HitCount + MissCount) 
        : 0;

    /// <summary>
    /// Toplam cache sÃ¼resi (ms)
    /// </summary>
    public long TotalDurationMs { get; set; }
}

/// <summary>
/// External service performans metrikleri
/// </summary>
public class ExternalServiceMetrics
{
    /// <summary>
    /// Toplam Ã§aÄŸrÄ± sayÄ±sÄ±
    /// </summary>
    public int CallCount { get; set; }

    /// <summary>
    /// Toplam sÃ¼re (ms)
    /// </summary>
    public long TotalDurationMs { get; set; }

    /// <summary>
    /// BaÅŸarÄ±sÄ±z Ã§aÄŸrÄ± sayÄ±sÄ±
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Retry sayÄ±sÄ±
    /// </summary>
    public int RetryCount { get; set; }
}


