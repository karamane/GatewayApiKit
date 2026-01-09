using ApiGatewayKit.Core.Shared.Constants;

namespace ApiGatewayKit.Core.Application.Models.Logging;

/// <summary>
/// Business exception log entry
/// Ä°ÅŸ kuralÄ± ihlalleri iÃ§in Ã¶zel log
/// </summary>
public class BusinessExceptionLogEntry : ExceptionLogEntry
{
    public BusinessExceptionLogEntry()
    {
        LogType = LogConstants.LogTypes.BusinessException;
        LogLevel = "Warning";
        ExceptionCategory = LogConstants.ExceptionCategories.Business;
    }

    #region Business Context

    /// <summary>
    /// Ä°ÅŸ operasyonu adÄ±
    /// </summary>
    public string? BusinessOperation { get; set; }

    /// <summary>
    /// Ä°ÅŸ hatasÄ± kodu
    /// </summary>
    public string? BusinessErrorCode { get; set; }

    /// <summary>
    /// Ä°ÅŸ hatasÄ± mesajÄ±
    /// </summary>
    public string? BusinessErrorMessage { get; set; }

    /// <summary>
    /// Etkilenen entity tipi
    /// </summary>
    public string? AffectedEntity { get; set; }

    /// <summary>
    /// Etkilenen entity ID
    /// </summary>
    public string? AffectedEntityId { get; set; }

    #endregion

    #region KullanÄ±cÄ± MesajlarÄ±

    /// <summary>
    /// KullanÄ±cÄ± dostu mesaj
    /// </summary>
    public string? UserFriendlyMessage { get; set; }

    /// <summary>
    /// Ã–nerilen aksiyon
    /// </summary>
    public string? SuggestedAction { get; set; }

    #endregion

    #region Ä°ÅŸ KuralÄ± DetaylarÄ±

    /// <summary>
    /// Kural adÄ±
    /// </summary>
    public string? RuleName { get; set; }

    /// <summary>
    /// Kural aÃ§Ä±klamasÄ±
    /// </summary>
    public string? RuleDescription { get; set; }

    /// <summary>
    /// Validation hatalarÄ±
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    #endregion

    /// <summary>
    /// BusinessException'dan log entry oluÅŸturur
    /// </summary>
    public static BusinessExceptionLogEntry FromBusinessException(
        Shared.Exceptions.BusinessException ex,
        string correlationId,
        string layer)
    {
        return new BusinessExceptionLogEntry
        {
            CorrelationId = correlationId,
            Layer = layer,
            ExceptionType = ex.GetType().FullName,
            ExceptionMessage = ex.Message,
            StackTrace = ex.StackTrace,
            BusinessErrorCode = ex.ErrorCode,
            BusinessErrorMessage = ex.Message,
            UserFriendlyMessage = ex.UserFriendlyMessage,
            SuggestedAction = ex.SuggestedAction,
            ValidationErrors = ex.ValidationErrors
        };
    }
}


