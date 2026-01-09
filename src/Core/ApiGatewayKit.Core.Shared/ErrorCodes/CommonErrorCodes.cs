namespace ApiGatewayKit.Core.Shared.ErrorCodes;

/// <summary>
/// Ortak hata kodlarÄ±
/// Her modÃ¼l kendi ErrorCodes sÄ±nÄ±fÄ±nÄ± oluÅŸturabilir
/// </summary>
public static class CommonErrorCodes
{
    #region Validation Errors (VAL-XXX)

    public static readonly ErrorCode ValidationFailed = new(
        "VAL-001",
        "Girilen bilgiler geÃ§ersiz",
        "Validation failed",
        400,
        ErrorCategory.Validation);

    public static readonly ErrorCode RequiredFieldMissing = new(
        "VAL-002",
        "Zorunlu alan eksik",
        "Required field is missing",
        400,
        ErrorCategory.Validation);

    public static readonly ErrorCode InvalidFormat = new(
        "VAL-003",
        "GeÃ§ersiz format",
        "Invalid format",
        400,
        ErrorCategory.Validation);

    #endregion

    #region Authentication Errors (AUTH-XXX)

    public static readonly ErrorCode Unauthorized = new(
        "AUTH-001",
        "Oturum aÃ§manÄ±z gerekiyor",
        "Authentication required",
        401,
        ErrorCategory.Authentication);

    public static readonly ErrorCode InvalidCredentials = new(
        "AUTH-002",
        "KullanÄ±cÄ± adÄ± veya ÅŸifre hatalÄ±",
        "Invalid credentials",
        401,
        ErrorCategory.Authentication);

    public static readonly ErrorCode TokenExpired = new(
        "AUTH-003",
        "Oturumunuz sona erdi, lÃ¼tfen tekrar giriÅŸ yapÄ±n",
        "Token expired",
        401,
        ErrorCategory.Authentication);

    #endregion

    #region Authorization Errors (AUTHZ-XXX)

    public static readonly ErrorCode Forbidden = new(
        "AUTHZ-001",
        "Bu iÅŸlem iÃ§in yetkiniz bulunmuyor",
        "Access forbidden",
        403,
        ErrorCategory.Authorization);

    public static readonly ErrorCode InsufficientPermissions = new(
        "AUTHZ-002",
        "Yetersiz yetki",
        "Insufficient permissions",
        403,
        ErrorCategory.Authorization);

    #endregion

    #region NotFound Errors (NF-XXX)

    public static readonly ErrorCode ResourceNotFound = new(
        "NF-001",
        "Kaynak bulunamadÄ±",
        "Resource not found",
        404,
        ErrorCategory.NotFound);

    public static readonly ErrorCode EntityNotFound = new(
        "NF-002",
        "KayÄ±t bulunamadÄ±",
        "Entity not found",
        404,
        ErrorCategory.NotFound);

    #endregion

    #region Business Errors (BUS-XXX)

    public static readonly ErrorCode BusinessRuleViolation = new(
        "BUS-001",
        "Ä°ÅŸ kuralÄ± ihlali",
        "Business rule violation",
        422,
        ErrorCategory.Business);

    public static readonly ErrorCode OperationNotAllowed = new(
        "BUS-002",
        "Bu iÅŸlem ÅŸu an gerÃ§ekleÅŸtirilemiyor",
        "Operation not allowed",
        422,
        ErrorCategory.Business);

    public static readonly ErrorCode DuplicateEntry = new(
        "BUS-003",
        "Bu kayÄ±t zaten mevcut",
        "Duplicate entry",
        409,
        ErrorCategory.Conflict);

    #endregion

    #region External Service Errors (EXT-XXX)

    public static readonly ErrorCode ExternalServiceError = new(
        "EXT-001",
        "DÄ±ÅŸ servis hatasÄ±, lÃ¼tfen daha sonra tekrar deneyin",
        "External service error",
        502,
        ErrorCategory.ExternalService,
        ErrorSeverity.Error);

    public static readonly ErrorCode ExternalServiceTimeout = new(
        "EXT-002",
        "DÄ±ÅŸ servis yanÄ±t vermedi, lÃ¼tfen daha sonra tekrar deneyin",
        "External service timeout",
        504,
        ErrorCategory.ExternalService,
        ErrorSeverity.Warning);

    public static readonly ErrorCode ExternalServiceUnavailable = new(
        "EXT-003",
        "DÄ±ÅŸ servis ÅŸu an kullanÄ±lamÄ±yor",
        "External service unavailable",
        503,
        ErrorCategory.ExternalService,
        ErrorSeverity.Warning);

    #endregion

    #region System Errors (SYS-XXX)

    public static readonly ErrorCode GeneralError = new(
        "SYS-000",
        "Bir hata oluÅŸtu",
        "General error occurred",
        500,
        ErrorCategory.System,
        ErrorSeverity.Error);

    public static readonly ErrorCode InternalError = new(
        "SYS-001",
        "Beklenmeyen bir hata oluÅŸtu, lÃ¼tfen daha sonra tekrar deneyin",
        "Internal server error",
        500,
        ErrorCategory.System,
        ErrorSeverity.Critical);

    public static readonly ErrorCode ServiceUnavailable = new(
        "SYS-002",
        "Servis ÅŸu an kullanÄ±lamÄ±yor",
        "Service unavailable",
        503,
        ErrorCategory.System,
        ErrorSeverity.Critical);

    public static readonly ErrorCode DatabaseError = new(
        "SYS-003",
        "VeritabanÄ± hatasÄ±",
        "Database error",
        500,
        ErrorCategory.System,
        ErrorSeverity.Critical);

    #endregion
}

/// <summary>
/// MÃ¼ÅŸteri modÃ¼lÃ¼ hata kodlarÄ± Ã¶rneÄŸi
/// </summary>
public static class CustomerErrorCodes
{
    public static readonly ErrorCode CustomerNotFound = new(
        "CUST-001",
        "MÃ¼ÅŸteri bulunamadÄ±",
        "Customer not found",
        404,
        ErrorCategory.NotFound);

    public static readonly ErrorCode EmailAlreadyExists = new(
        "CUST-002",
        "Bu email adresi zaten kayÄ±tlÄ±",
        "Email already exists",
        409,
        ErrorCategory.Conflict);

    public static readonly ErrorCode CustomerInactive = new(
        "CUST-003",
        "MÃ¼ÅŸteri hesabÄ± aktif deÄŸil",
        "Customer account is inactive",
        422,
        ErrorCategory.Business);

    public static readonly ErrorCode InvalidPhoneNumber = new(
        "CUST-004",
        "GeÃ§ersiz telefon numarasÄ±",
        "Invalid phone number format",
        400,
        ErrorCategory.Validation);
}


