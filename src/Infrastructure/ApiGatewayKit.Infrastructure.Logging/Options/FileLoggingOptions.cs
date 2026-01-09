namespace ApiGatewayKit.Infrastructure.Logging.Options;

/// <summary>
/// Dosya bazlÄ± loglama yapÄ±landÄ±rmasÄ±
/// </summary>
public class FileLoggingOptions
{
    public const string SectionName = "Logging:File";

    /// <summary>
    /// Dosya loglamasÄ± aktif mi?
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Ana log klasÃ¶rÃ¼ (varsayÄ±lan: logs)
    /// </summary>
    public string BasePath { get; set; } = "logs";

    /// <summary>
    /// Uygulama adÄ±nÄ± alt klasÃ¶r olarak kullan
    /// </summary>
    public bool UseApplicationSubfolder { get; set; } = true;

    /// <summary>
    /// Log dosyasÄ± tutma sÃ¼resi (gÃ¼n)
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Maksimum dosya boyutu (MB) - aÅŸÄ±lÄ±rsa yeni dosya oluÅŸturulur
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 100;

    /// <summary>
    /// Dosya sÄ±kÄ±ÅŸtÄ±rma (eski dosyalar iÃ§in)
    /// </summary>
    public bool CompressOldFiles { get; set; } = false;

    /// <summary>
    /// JSON formatÄ±nda log yaz
    /// </summary>
    public bool UseJsonFormat { get; set; } = false;

    /// <summary>
    /// AyrÄ± log dosyalarÄ± yapÄ±landÄ±rmasÄ±
    /// </summary>
    public SeparateLogFilesOptions SeparateFiles { get; set; } = new();

    /// <summary>
    /// Rolling interval (gÃ¼nlÃ¼k, saatlik vs.)
    /// </summary>
    public LogRollingInterval RollingInterval { get; set; } = LogRollingInterval.Day;
}

/// <summary>
/// AyrÄ± log dosyalarÄ± yapÄ±landÄ±rmasÄ±
/// </summary>
public class SeparateLogFilesOptions
{
    /// <summary>
    /// TÃ¼m loglarÄ± tek dosyaya yaz
    /// </summary>
    public bool AllLogs { get; set; } = true;

    /// <summary>
    /// Error ve Ã¼stÃ¼ loglarÄ± ayrÄ± dosyaya yaz
    /// </summary>
    public bool ErrorLogs { get; set; } = true;

    /// <summary>
    /// Request/Response loglarÄ±nÄ± ayrÄ± dosyaya yaz
    /// </summary>
    public bool RequestLogs { get; set; } = true;

    /// <summary>
    /// Performance loglarÄ±nÄ± ayrÄ± dosyaya yaz
    /// </summary>
    public bool PerformanceLogs { get; set; } = true;

    /// <summary>
    /// Business exception loglarÄ±nÄ± ayrÄ± dosyaya yaz
    /// </summary>
    public bool BusinessLogs { get; set; } = true;

    /// <summary>
    /// Security/Audit loglarÄ±nÄ± ayrÄ± dosyaya yaz
    /// </summary>
    public bool SecurityLogs { get; set; } = true;
}

/// <summary>
/// Log dosyasÄ± rolling aralÄ±ÄŸÄ±
/// </summary>
public enum LogRollingInterval
{
    /// <summary>
    /// SÄ±nÄ±rsÄ±z (tek dosya)
    /// </summary>
    Infinite,

    /// <summary>
    /// YÄ±lda bir
    /// </summary>
    Year,

    /// <summary>
    /// Ayda bir
    /// </summary>
    Month,

    /// <summary>
    /// GÃ¼nde bir
    /// </summary>
    Day,

    /// <summary>
    /// Saatte bir
    /// </summary>
    Hour,

    /// <summary>
    /// Dakikada bir (test iÃ§in)
    /// </summary>
    Minute
}


