namespace ApiGatewayKit.Core.Shared.Exceptions;

/// <summary>
/// TÃ¼m Ã¶zel exception'larÄ±n base sÄ±nÄ±fÄ±
/// </summary>
public abstract class BaseException : Exception
{
    public string ErrorCode { get; protected set; }
    public string Layer { get; protected set; }
    public Dictionary<string, object> AdditionalData { get; } = new();

    protected BaseException(string message)
        : base(message)
    {
        ErrorCode = "UNKNOWN";
        Layer = "Unknown";
    }

    protected BaseException(string message, Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = "UNKNOWN";
        Layer = "Unknown";
    }

    protected BaseException(string message, string errorCode, string layer)
        : base(message)
    {
        ErrorCode = errorCode;
        Layer = layer;
    }

    protected BaseException(string message, string errorCode, string layer, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Layer = layer;
    }

    /// <summary>
    /// Ek veri ekler
    /// </summary>
    public BaseException WithData(string key, object value)
    {
        AdditionalData[key] = value;
        return this;
    }
}

/// <summary>
/// Ä°ÅŸ kuralÄ± hatasÄ± - ErrorCode sistemi ile entegre
/// </summary>
public class BusinessException : BaseException
{
    public string? UserFriendlyMessage { get; set; }
    public string? SuggestedAction { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    /// <summary>
    /// ErrorCode nesnesi (opsiyonel)
    /// </summary>
    public ErrorCodes.ErrorCode? ErrorCodeInfo { get; }

    /// <summary>
    /// Orijinal exception (loglama iÃ§in)
    /// </summary>
    public Exception? OriginalException { get; }

    /// <summary>
    /// HTTP Status Code
    /// </summary>
    public int HttpStatusCode => ErrorCodeInfo?.HttpStatusCode ?? 400;

    public BusinessException(string message, string errorCode = "BUSINESS_ERROR")
        : base(message, errorCode, Constants.LogConstants.Layers.Business)
    {
    }

    public BusinessException(string message, string errorCode, Exception innerException)
        : base(message, errorCode, Constants.LogConstants.Layers.Business, innerException)
    {
        OriginalException = innerException;
    }

    /// <summary>
    /// ErrorCode ile BusinessException oluÅŸturur
    /// </summary>
    public BusinessException(ErrorCodes.ErrorCode errorCode)
        : base(errorCode.UserMessage, errorCode.Code, Constants.LogConstants.Layers.Business)
    {
        ErrorCodeInfo = errorCode;
        UserFriendlyMessage = errorCode.UserMessage;
    }

    /// <summary>
    /// ErrorCode ile BusinessException oluÅŸturur (Ã¶zel mesaj ile)
    /// </summary>
    public BusinessException(ErrorCodes.ErrorCode errorCode, string? customMessage)
        : base(customMessage ?? errorCode.UserMessage, errorCode.Code, Constants.LogConstants.Layers.Business)
    {
        ErrorCodeInfo = errorCode;
        UserFriendlyMessage = customMessage ?? errorCode.UserMessage;
    }

    /// <summary>
    /// ErrorCode ile BusinessException oluÅŸturur (inner exception ile)
    /// </summary>
    public BusinessException(ErrorCodes.ErrorCode errorCode, Exception innerException)
        : base(errorCode.UserMessage, errorCode.Code, Constants.LogConstants.Layers.Business, innerException)
    {
        ErrorCodeInfo = errorCode;
        OriginalException = innerException;
        UserFriendlyMessage = errorCode.UserMessage;
    }

    /// <summary>
    /// ErrorCode ile BusinessException oluÅŸturur (ek veri ile)
    /// </summary>
    public BusinessException(ErrorCodes.ErrorCode errorCode, Exception? innerException, Dictionary<string, object>? additionalData)
        : base(errorCode.UserMessage, errorCode.Code, Constants.LogConstants.Layers.Business, innerException!)
    {
        ErrorCodeInfo = errorCode;
        OriginalException = innerException;
        UserFriendlyMessage = errorCode.UserMessage;
        
        if (additionalData != null)
        {
            foreach (var item in additionalData)
            {
                AdditionalData[item.Key] = item.Value;
            }
        }
    }

    public BusinessException WithUserMessage(string userMessage)
    {
        UserFriendlyMessage = userMessage;
        return this;
    }

    public BusinessException WithSuggestion(string suggestion)
    {
        SuggestedAction = suggestion;
        return this;
    }
}

/// <summary>
/// Business exception oluÅŸturma helper'Ä±
/// </summary>
public static class BusinessExceptionFactory
{
    /// <summary>
    /// Orijinal exception'Ä± koruyarak business exception oluÅŸturur
    /// </summary>
    public static BusinessException Create(
        ErrorCodes.ErrorCode errorCode,
        Exception? originalException = null,
        Dictionary<string, object>? additionalData = null)
    {
        return new BusinessException(errorCode, originalException, additionalData);
    }

    /// <summary>
    /// NotFound exception
    /// </summary>
    public static BusinessException NotFound(string resourceName, object? id = null)
    {
        var data = id != null
            ? new Dictionary<string, object> { ["ResourceId"] = id }
            : null;

        return new BusinessException(
            ErrorCodes.CommonErrorCodes.EntityNotFound,
            null,
            data);
    }

    /// <summary>
    /// Duplicate exception
    /// </summary>
    public static BusinessException Duplicate(string fieldName, object? value = null)
    {
        var data = value != null
            ? new Dictionary<string, object> { ["FieldName"] = fieldName, ["Value"] = value }
            : null;

        return new BusinessException(
            ErrorCodes.CommonErrorCodes.DuplicateEntry,
            null,
            data);
    }
}

/// <summary>
/// Kaynak bulunamadÄ± hatasÄ±
/// </summary>
public class NotFoundException : BaseException
{
    public string ResourceType { get; }
    public string ResourceId { get; }

    public NotFoundException(string resourceType, string resourceId)
        : base($"{resourceType} with ID '{resourceId}' was not found.", "NOT_FOUND", Constants.LogConstants.Layers.Domain)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}

/// <summary>
/// Validation hatasÄ±
/// </summary>
public class ValidationException : BaseException
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(Dictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", "VALIDATION_ERROR", Constants.LogConstants.Layers.Business)
    {
        Errors = errors;
    }

    public ValidationException(string field, string error)
        : base($"Validation error on field '{field}': {error}", "VALIDATION_ERROR", Constants.LogConstants.Layers.Business)
    {
        Errors = new Dictionary<string, string[]> { { field, new[] { error } } };
    }
}

/// <summary>
/// DÄ±ÅŸ servis hatasÄ±
/// </summary>
public class ExternalServiceException : BaseException
{
    public string ServiceName { get; }
    public int? StatusCode { get; }
    public string? ResponseBody { get; }

    public ExternalServiceException(string serviceName, string message)
        : base(message, "EXTERNAL_SERVICE_ERROR", Constants.LogConstants.Layers.Proxy)
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, int statusCode, string? responseBody)
        : base($"External service '{serviceName}' returned status code {statusCode}", "EXTERNAL_SERVICE_ERROR", Constants.LogConstants.Layers.Proxy)
    {
        ServiceName = serviceName;
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}

/// <summary>
/// VeritabanÄ± hatasÄ±
/// </summary>
public class DatabaseException : BaseException
{
    public string? SqlState { get; }
    public int? SqlErrorNumber { get; }

    public DatabaseException(string message, Exception innerException)
        : base(message, "DATABASE_ERROR", Constants.LogConstants.Layers.Domain, innerException)
    {
    }

    public DatabaseException(string message, string? sqlState, int? sqlErrorNumber, Exception innerException)
        : base(message, "DATABASE_ERROR", Constants.LogConstants.Layers.Domain, innerException)
    {
        SqlState = sqlState;
        SqlErrorNumber = sqlErrorNumber;
    }
}

/// <summary>
/// Yetkilendirme hatasÄ±
/// </summary>
public class UnauthorizedException : BaseException
{
    public UnauthorizedException(string message = "Unauthorized access")
        : base(message, "UNAUTHORIZED", Constants.LogConstants.Layers.ServerApi)
    {
    }
}

/// <summary>
/// EriÅŸim engellendi hatasÄ±
/// </summary>
public class ForbiddenException : BaseException
{
    public ForbiddenException(string message = "Access forbidden")
        : base(message, "FORBIDDEN", Constants.LogConstants.Layers.ServerApi)
    {
    }
}


