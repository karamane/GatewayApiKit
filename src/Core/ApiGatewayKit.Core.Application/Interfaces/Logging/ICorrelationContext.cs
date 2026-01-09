namespace ApiGatewayKit.Core.Application.Interfaces.Logging;

/// <summary>
/// Request boyunca taÅŸÄ±nan correlation context bilgileri
/// UÃ§tan uca request tracking iÃ§in kullanÄ±lÄ±r
/// </summary>
public interface ICorrelationContext
{
    /// <summary>
    /// Unique correlation ID - TÃ¼m katmanlarda aynÄ± kalÄ±r
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Parent correlation ID - Nested call'lar iÃ§in
    /// </summary>
    string? ParentCorrelationId { get; }

    /// <summary>
    /// Authenticated user ID
    /// </summary>
    string? UserId { get; set; }

    /// <summary>
    /// Client IP adresi
    /// </summary>
    string? ClientIp { get; set; }

    /// <summary>
    /// Client user agent
    /// </summary>
    string? UserAgent { get; set; }

    /// <summary>
    /// Request path
    /// </summary>
    string? RequestPath { get; set; }

    /// <summary>
    /// Server adÄ± (multi-server deployment iÃ§in)
    /// </summary>
    string ServerName { get; }

    /// <summary>
    /// Server IP
    /// </summary>
    string? ServerIp { get; }

    /// <summary>
    /// Request baÅŸlangÄ±Ã§ zamanÄ±
    /// </summary>
    DateTime RequestStartTime { get; }

    /// <summary>
    /// Session ID
    /// </summary>
    string? SessionId { get; set; }

    /// <summary>
    /// Ek Ã¶zellikler
    /// </summary>
    Dictionary<string, object> CustomProperties { get; }

    /// <summary>
    /// Ã–zel Ã¶zellik ekler
    /// </summary>
    void SetProperty(string key, object value);

    /// <summary>
    /// Ã–zel Ã¶zellik getirir
    /// </summary>
    T? GetProperty<T>(string key);
}


