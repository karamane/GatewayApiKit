namespace ApiGatewayKit.Core.Shared.ErrorCodes;

/// <summary>
/// Hata kodu tanÄ±mlama sistemi
/// Developer'larÄ±n kolayca hata kodu tanÄ±mlayabilmesini saÄŸlar
/// </summary>
public record ErrorCode
{
    /// <summary>
    /// Hata kodu (Ã¶r: CUST-001, ORD-002, PAY-003)
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// KullanÄ±cÄ±ya gÃ¶sterilecek mesaj
    /// </summary>
    public string UserMessage { get; init; }

    /// <summary>
    /// Teknik/detaylÄ± mesaj (log iÃ§in)
    /// </summary>
    public string TechnicalMessage { get; init; }

    /// <summary>
    /// HTTP status code (varsayÄ±lan 400)
    /// </summary>
    public int HttpStatusCode { get; init; }

    /// <summary>
    /// Hata kategorisi
    /// </summary>
    public ErrorCategory Category { get; init; }

    /// <summary>
    /// Hata ÅŸiddeti
    /// </summary>
    public ErrorSeverity Severity { get; init; }

    public ErrorCode(
        string code,
        string userMessage,
        string? technicalMessage = null,
        int httpStatusCode = 400,
        ErrorCategory category = ErrorCategory.Business,
        ErrorSeverity severity = ErrorSeverity.Error)
    {
        Code = code;
        UserMessage = userMessage;
        TechnicalMessage = technicalMessage ?? userMessage;
        HttpStatusCode = httpStatusCode;
        Category = category;
        Severity = severity;
    }

    public override string ToString() => $"[{Code}] {UserMessage}";
}

/// <summary>
/// Hata kategorileri
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// Validasyon hatasÄ±
    /// </summary>
    Validation,

    /// <summary>
    /// Ä°ÅŸ kuralÄ± hatasÄ±
    /// </summary>
    Business,

    /// <summary>
    /// Yetkilendirme hatasÄ±
    /// </summary>
    Authorization,

    /// <summary>
    /// Kimlik doÄŸrulama hatasÄ±
    /// </summary>
    Authentication,

    /// <summary>
    /// Kaynak bulunamadÄ±
    /// </summary>
    NotFound,

    /// <summary>
    /// Conflict (Ã§akÄ±ÅŸma)
    /// </summary>
    Conflict,

    /// <summary>
    /// DÄ±ÅŸ servis hatasÄ±
    /// </summary>
    ExternalService,

    /// <summary>
    /// Sistem hatasÄ±
    /// </summary>
    System
}

/// <summary>
/// Hata ÅŸiddeti
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Bilgi - iÅŸlem devam edebilir
    /// </summary>
    Info,

    /// <summary>
    /// UyarÄ± - dikkat edilmeli
    /// </summary>
    Warning,

    /// <summary>
    /// Hata - iÅŸlem durdu
    /// </summary>
    Error,

    /// <summary>
    /// Kritik - sistem seviyesi hata
    /// </summary>
    Critical
}


