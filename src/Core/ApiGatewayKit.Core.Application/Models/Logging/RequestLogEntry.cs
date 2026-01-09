using ApiGatewayKit.Core.Shared.Constants;

namespace ApiGatewayKit.Core.Application.Models.Logging;

/// <summary>
/// HTTP Request log entry
/// </summary>
public class RequestLogEntry : BaseLogEntry
{
    public RequestLogEntry()
    {
        LogType = LogConstants.LogTypes.Request;
    }

    #region HTTP Request Bilgileri

    /// <summary>
    /// HTTP Method (GET, POST, PUT, DELETE, etc.)
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Request path
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// Query string
    /// </summary>
    public string? QueryString { get; set; }

    /// <summary>
    /// Request body (maskelenmiÅŸ)
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// Request header'larÄ±
    /// </summary>
    public Dictionary<string, string>? RequestHeaders { get; set; }

    /// <summary>
    /// Content type
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Content length
    /// </summary>
    public long? ContentLength { get; set; }

    #endregion

    #region Client Bilgileri

    /// <summary>
    /// User agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Referer
    /// </summary>
    public string? Referer { get; set; }

    /// <summary>
    /// Origin
    /// </summary>
    public string? Origin { get; set; }

    #endregion

    #region Security Bilgileri

    /// <summary>
    /// Authorization tÃ¼rÃ¼ (Bearer, Basic, etc.)
    /// </summary>
    public string? AuthorizationType { get; set; }

    /// <summary>
    /// Authenticated mi?
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// KullanÄ±cÄ± rolleri
    /// </summary>
    public string[]? UserRoles { get; set; }

    #endregion
}


