namespace ApiGatewayKit.Gateway.Security;

/// <summary>
/// Gateway Admin Panel gÃ¼venlik konfigÃ¼rasyonu.
/// TÃ¼m ayarlar appsettings.json'dan okunur, DB baÄŸÄ±mlÄ±lÄ±ÄŸÄ± yoktur.
/// </summary>
public class AdminSecurityOptions
{
    public const string SectionName = "GatewayAdmin";

    /// <summary>
    /// Admin gÃ¼venliÄŸi aktif mi?
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Ä°zin verilen IP adresleri (CIDR notation destekler: 10.0.0.0/8)
    /// </summary>
    public List<string> IpWhitelist { get; set; } = new() { "127.0.0.1", "::1" };

    /// <summary>
    /// API Key'ler (hash'lenmiÅŸ)
    /// </summary>
    public List<AdminApiKey> ApiKeys { get; set; } = new();

    /// <summary>
    /// Rate limiting ayarlarÄ±
    /// </summary>
    public AdminRateLimitOptions RateLimit { get; set; } = new();

    /// <summary>
    /// Audit logging ayarlarÄ±
    /// </summary>
    public AdminAuditOptions Audit { get; set; } = new();

    /// <summary>
    /// Production'da bazÄ± endpoint'leri kÄ±sÄ±tla
    /// </summary>
    public ProductionRestrictions Production { get; set; } = new();
}

/// <summary>
/// API Key tanÄ±mÄ±
/// </summary>
public class AdminApiKey
{
    /// <summary>
    /// Key sahibi/takÄ±m adÄ± (audit log iÃ§in)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// API Key'in SHA256 hash'i (sha256:abc123... formatÄ±nda)
    /// Plain text key ASLA saklanmaz
    /// </summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Ä°zinler: read, write, emergency
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Key aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Key son kullanma tarihi (opsiyonel)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Admin endpoint'leri iÃ§in rate limit ayarlarÄ±
/// </summary>
public class AdminRateLimitOptions
{
    /// <summary>
    /// Dakikada izin verilen maksimum istek sayÄ±sÄ±
    /// </summary>
    public int RequestsPerMinute { get; set; } = 30;

    /// <summary>
    /// Limit aÅŸÄ±ldÄ±ÄŸÄ±nda blok sÃ¼resi (dakika)
    /// </summary>
    public int BlockDurationMinutes { get; set; } = 5;

    /// <summary>
    /// Rate limiting aktif mi?
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Audit logging ayarlarÄ±
/// </summary>
public class AdminAuditOptions
{
    /// <summary>
    /// Audit logging aktif mi?
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Log dosyasÄ± yolu ({Date} placeholder destekler)
    /// </summary>
    public string FilePath { get; set; } = "logs/admin-audit/audit-{Date}.log";

    /// <summary>
    /// Request body'yi logla (hassas veri iÃ§erebilir)
    /// </summary>
    public bool LogRequestBody { get; set; } = true;

    /// <summary>
    /// Response body'yi logla
    /// </summary>
    public bool LogResponseBody { get; set; } = false;
}

/// <summary>
/// Production ortamÄ± kÄ±sÄ±tlamalarÄ±
/// </summary>
public class ProductionRestrictions
{
    /// <summary>
    /// Production'da yazma endpoint'lerini devre dÄ±ÅŸÄ± bÄ±rak
    /// </summary>
    public bool DisableWriteEndpoints { get; set; } = false;

    /// <summary>
    /// Production'da sadece bu endpoint'lere izin ver (boÅŸsa tÃ¼mÃ¼ne izin)
    /// </summary>
    public List<string> AllowedEndpoints { get; set; } = new();
}

/// <summary>
/// Ä°zin tÃ¼rleri
/// </summary>
public static class AdminPermissions
{
    public const string Read = "read";
    public const string Write = "write";
    public const string Emergency = "emergency";

    /// <summary>
    /// Verilen iznin diÄŸer izni kapsayÄ±p kapsamadÄ±ÄŸÄ±nÄ± kontrol eder.
    /// Emergency > Write > Read
    /// </summary>
    public static bool Covers(string permission, string required)
    {
        if (string.Equals(permission, required, StringComparison.OrdinalIgnoreCase))
            return true;

        return permission.ToLowerInvariant() switch
        {
            Emergency => true, // Emergency her ÅŸeye eriÅŸebilir
            Write => required.Equals(Read, StringComparison.OrdinalIgnoreCase), // Write, read'i kapsar
            _ => false
        };
    }

    /// <summary>
    /// Verilen izin listesinin gerekli izni karÅŸÄ±layÄ±p karÅŸÄ±lamadÄ±ÄŸÄ±nÄ± kontrol eder
    /// </summary>
    public static bool HasPermission(IEnumerable<string> permissions, string required)
    {
        return permissions.Any(p => Covers(p, required));
    }
}


