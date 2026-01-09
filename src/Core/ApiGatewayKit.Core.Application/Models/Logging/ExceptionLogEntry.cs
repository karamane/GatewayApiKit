using ApiGatewayKit.Core.Shared.Constants;

namespace ApiGatewayKit.Core.Application.Models.Logging;

/// <summary>
/// Exception log entry
/// </summary>
public class ExceptionLogEntry : BaseLogEntry
{
    public ExceptionLogEntry()
    {
        LogType = LogConstants.LogTypes.Exception;
        LogLevel = "Error";
    }

    #region Exception DetaylarÄ±

    /// <summary>
    /// Exception tipi
    /// </summary>
    public string? ExceptionType { get; set; }

    /// <summary>
    /// Exception mesajÄ±
    /// </summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>
    /// Stack trace
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Source
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Target site
    /// </summary>
    public string? TargetSite { get; set; }

    /// <summary>
    /// HResult
    /// </summary>
    public int? HResult { get; set; }

    #endregion

    #region Inner Exception

    /// <summary>
    /// Inner exception tipi
    /// </summary>
    public string? InnerExceptionType { get; set; }

    /// <summary>
    /// Inner exception mesajÄ±
    /// </summary>
    public string? InnerExceptionMessage { get; set; }

    /// <summary>
    /// Inner exception stack trace
    /// </summary>
    public string? InnerStackTrace { get; set; }

    #endregion

    #region Hata Konteksti

    /// <summary>
    /// Request path
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// HTTP method
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Request body
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// Method parametreleri
    /// </summary>
    public Dictionary<string, object>? MethodParameters { get; set; }

    #endregion

    #region Kategorisazyon

    /// <summary>
    /// Exception kategorisi (System, Database, Network, Business, Validation)
    /// </summary>
    public string? ExceptionCategory { get; set; }

    /// <summary>
    /// Handle edilmiÅŸ mi?
    /// </summary>
    public bool IsHandled { get; set; }

    /// <summary>
    /// Transient hata mÄ±? (Retry edilebilir)
    /// </summary>
    public bool IsTransient { get; set; }

    /// <summary>
    /// Retry sayÄ±sÄ±
    /// </summary>
    public int? RetryCount { get; set; }

    /// <summary>
    /// HatanÄ±n alÄ±ndÄ±ÄŸÄ± metod
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// HatanÄ±n alÄ±ndÄ±ÄŸÄ± katman (API, Business, Domain, Proxy)
    /// </summary>
    public string? LayerName { get; set; }

    /// <summary>
    /// Hata ÅŸiddeti (Critical, Error, Warning)
    /// </summary>
    public string? Severity { get; set; }

    /// <summary>
    /// Ek veri
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }

    #endregion

    /// <summary>
    /// Exception'dan log entry oluÅŸturur
    /// </summary>
    public static ExceptionLogEntry FromException(Exception ex, string layer, string? correlationId = null)
    {
        var entry = new ExceptionLogEntry
        {
            CorrelationId = correlationId ?? string.Empty,
            Layer = layer,
            ExceptionType = ex.GetType().FullName,
            ExceptionMessage = ex.Message,
            StackTrace = ex.StackTrace,
            Source = ex.Source,
            TargetSite = ex.TargetSite?.ToString(),
            HResult = ex.HResult
        };

        if (ex.InnerException != null)
        {
            entry.InnerExceptionType = ex.InnerException.GetType().FullName;
            entry.InnerExceptionMessage = ex.InnerException.Message;
            entry.InnerStackTrace = ex.InnerException.StackTrace;
        }

        // Kategorisazyon
        entry.ExceptionCategory = CategorizeException(ex);
        entry.IsTransient = IsTransientException(ex);

        return entry;
    }

    private static string CategorizeException(Exception ex)
    {
        return ex switch
        {
            Shared.Exceptions.BusinessException => LogConstants.ExceptionCategories.Business,
            Shared.Exceptions.ValidationException => LogConstants.ExceptionCategories.Validation,
            Shared.Exceptions.DatabaseException => LogConstants.ExceptionCategories.Database,
            Shared.Exceptions.ExternalServiceException => LogConstants.ExceptionCategories.External,
            Shared.Exceptions.UnauthorizedException => LogConstants.ExceptionCategories.Security,
            Shared.Exceptions.ForbiddenException => LogConstants.ExceptionCategories.Security,
            HttpRequestException => LogConstants.ExceptionCategories.Network,
            TimeoutException => LogConstants.ExceptionCategories.Network,
            _ => LogConstants.ExceptionCategories.System
        };
    }

    private static bool IsTransientException(Exception ex)
    {
        return ex is TimeoutException or
               HttpRequestException or
               TaskCanceledException;
    }
}


