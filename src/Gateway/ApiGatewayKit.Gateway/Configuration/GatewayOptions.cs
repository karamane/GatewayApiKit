namespace ApiGatewayKit.Gateway.Configuration;

/// <summary>
/// Gateway yapilandirma secenekleri
/// </summary>
public class GatewayOptions
{
    public const string SectionName = "Gateway";

    /// <summary>
    /// Varsayilan yonlendirme hedefi (kural bulunamazsa)
    /// </summary>
    public RoutingTarget DefaultTarget { get; set; } = RoutingTarget.Legacy;

    /// <summary>
    /// Varsayilan eski sistem base URL
    /// </summary>
    public string LegacyBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Client API base URL (Gateway yonetimi ve yeni sistem islemleri icin)
    /// Client API uzerinden Server API'ye erisir
    /// </summary>
    public string NewBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Legacy sistem API path prefix'i
    /// Ã–rnek: /moim/api/v1/internet
    /// </summary>
    public string LegacyPathPrefix { get; set; } = "/moim/api/v1/internet";

    /// <summary>
    /// Yeni sistem API path prefix'i
    /// Ã–rnek: /api
    /// </summary>
    public string NewPathPrefix { get; set; } = "/api";

    /// <summary>
    /// Shadow mode aktif mi? (Her iki sisteme gonder, karsilastir)
    /// </summary>
    public bool EnableShadowMode { get; set; }

    /// <summary>
    /// Cache'deki routing kurallari icin prefix
    /// </summary>
    public string RoutingRuleCachePrefix { get; set; } = "route:";

    /// <summary>
    /// HTTP istek timeout suresi (saniye)
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Retry politikasi - deneme sayisi
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Circuit breaker - acilmadan once izin verilen hata sayisi
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker - acik kaldigi sure (saniye)
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Health check endpoint'leri yonlendirilmeden bypass edilsin mi?
    /// </summary>
    public bool BypassHealthChecks { get; set; } = true;

    /// <summary>
    /// Bypass edilecek path listesi (ornek: /swagger, /health)
    /// </summary>
    public List<string> BypassPaths { get; set; } = new() { "/swagger", "/health", "/favicon.ico", "/admin", "/api/gateway" };

    /// <summary>
    /// Legacy path'ten modÃ¼l ve metot bilgisini Ã§Ä±karÄ±r
    /// /moim/api/v1/internet/auth/login/v2 -> (auth, login/v2)
    /// </summary>
    public (string? moduleCode, string? methodPath) ParseLegacyPath(string path)
    {
        var prefix = LegacyPathPrefix.TrimEnd('/');
        
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return (null, null);

        // Prefix'i kaldÄ±r
        var remaining = path[prefix.Length..].TrimStart('/');
        
        if (string.IsNullOrEmpty(remaining))
            return (null, null);

        // Ä°lk segment modÃ¼l, geri kalanÄ± metot path'i
        var segments = remaining.Split('/', 2);
        var moduleCode = segments[0].ToUpperInvariant();
        var methodPath = segments.Length > 1 ? segments[1] : null;

        return (moduleCode, methodPath);
    }

    /// <summary>
    /// Her tÃ¼rlÃ¼ path formatÄ±nÄ± Legacy formatÄ±na normalize eder
    /// </summary>
    public string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "/";

        // Query string temizle, kÃ¼Ã§Ã¼k harfe Ã§evir, sondaki slash'Ä± at
        var normalized = path.Split('?')[0].TrimEnd('/').ToLowerInvariant();
        if (!normalized.StartsWith('/')) normalized = "/" + normalized;

        var newPrefix = NewPathPrefix.TrimEnd('/').ToLowerInvariant();
        var legacyPrefix = LegacyPathPrefix.TrimEnd('/').ToLowerInvariant();

        // EÄŸer yeni sistem path formatÄ± ise (/api/...) legacy'ye Ã§evir
        if (normalized.StartsWith(newPrefix + "/"))
        {
            return legacyPrefix + normalized.Substring(newPrefix.Length);
        }
        
        if (normalized == newPrefix)
        {
            return legacyPrefix;
        }

        return normalized;
    }

    /// <summary>
    /// Path'i Legacy sistem formatÄ±na dÃ¶nÃ¼ÅŸtÃ¼rÃ¼r (Forwarding iÃ§in)
    /// </summary>
    public string TransformToLegacyPath(string path)
    {
        var legacyPrefix = LegacyPathPrefix.TrimEnd('/');
        var newPrefix = NewPathPrefix.TrimEnd('/');

        // Zaten legacy format ise deÄŸiÅŸtirme
        if (path.StartsWith(legacyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        // Yeni format ise prefix'i deÄŸiÅŸtir
        if (path.StartsWith(newPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var remaining = path[newPrefix.Length..];
            return $"{legacyPrefix}{remaining}";
        }

        return path;
    }

    /// <summary>
    /// Yeni sistem iÃ§in path dÃ¶nÃ¼ÅŸÃ¼mÃ¼ yapar
    /// </summary>
    public string TransformToNewPath(string legacyPath)
    {
        var legacyPrefix = LegacyPathPrefix.TrimEnd('/');
        var newPrefix = NewPathPrefix.TrimEnd('/');
        
        if (!legacyPath.StartsWith(legacyPrefix, StringComparison.OrdinalIgnoreCase))
            return legacyPath;

        var remaining = legacyPath[legacyPrefix.Length..];
        return $"{newPrefix}{remaining}";
    }
}

