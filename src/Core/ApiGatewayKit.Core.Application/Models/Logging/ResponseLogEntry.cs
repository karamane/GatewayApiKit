using ApiGatewayKit.Core.Shared.Constants;

namespace ApiGatewayKit.Core.Application.Models.Logging;

/// <summary>
/// HTTP Response log entry
/// </summary>
public class ResponseLogEntry : BaseLogEntry
{
    public ResponseLogEntry()
    {
        LogType = LogConstants.LogTypes.Response;
    }

    #region HTTP Response Bilgileri

    /// <summary>
    /// HTTP Status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Status description
    /// </summary>
    public string? StatusDescription { get; set; }

    /// <summary>
    /// Response body (maskelenmiÅŸ ve truncate edilmiÅŸ)
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Response header'larÄ±
    /// </summary>
    public Dictionary<string, string>? ResponseHeaders { get; set; }

    /// <summary>
    /// Content type
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Content length
    /// </summary>
    public long? ContentLength { get; set; }

    #endregion

    #region Performans Metrikleri

    /// <summary>
    /// Toplam iÅŸlem sÃ¼resi (ms)
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// VeritabanÄ± sorgu sayÄ±sÄ±
    /// </summary>
    public int? DbQueryCount { get; set; }

    /// <summary>
    /// VeritabanÄ± sorgu sÃ¼resi (ms)
    /// </summary>
    public long? DbQueryDurationMs { get; set; }

    /// <summary>
    /// Cache hit sayÄ±sÄ±
    /// </summary>
    public int? CacheHitCount { get; set; }

    /// <summary>
    /// Cache miss sayÄ±sÄ±
    /// </summary>
    public int? CacheMissCount { get; set; }

    /// <summary>
    /// External service call count
    /// </summary>
    public int? ExternalCallCount { get; set; }

    /// <summary>
    /// External service call duration (ms)
    /// </summary>
    public long? ExternalCallDurationMs { get; set; }

    #endregion

    /// <summary>
    /// Ä°liÅŸkili request log ID
    /// </summary>
    public string? RequestLogId { get; set; }
}


