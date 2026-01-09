using Newtonsoft.Json;

namespace ApiGatewayKit.Gateway.Configuration;

/// <summary>
/// T_GATEWAY_MODULE tablosundan okunan modÃ¼l tanÄ±mÄ±
/// </summary>
public class GatewayModule
{
    /// <summary>
    /// VeritabanÄ± ID
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    /// Teknik modÃ¼l kodu (LOGIN, AUTH, GUEST, vb.)
    /// </summary>
    [JsonProperty("moduleCode")]
    public string ModuleCode { get; set; } = string.Empty;

    /// <summary>
    /// TÃ¼rkÃ§e gÃ¶rÃ¼nen modÃ¼l adÄ±
    /// </summary>
    [JsonProperty("moduleName")]
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// Legacy path prefix (Ã¶rn: /moim/api/v1/internet/auth)
    /// </summary>
    [JsonProperty("legacyPathPrefix")]
    public string LegacyPathPrefix { get; set; } = string.Empty;

    /// <summary>
    /// New path prefix (Ã¶rn: /api/auth)
    /// </summary>
    [JsonProperty("newPathPrefix")]
    public string NewPathPrefix { get; set; } = string.Empty;

    /// <summary>
    /// GÃ¶rÃ¼ntÃ¼leme sÄ±rasÄ±
    /// </summary>
    [JsonProperty("displayOrder")]
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Legacy path'i New path'e dÃ¶nÃ¼ÅŸtÃ¼rÃ¼r
    /// Ã–rn: /moim/api/v1/internet/auth/login/v2 -> /api/auth/login/v2
    /// </summary>
    public string TransformToNewPath(string legacyPath)
    {
        if (string.IsNullOrEmpty(LegacyPathPrefix) || string.IsNullOrEmpty(NewPathPrefix))
            return legacyPath;

        var prefix = LegacyPathPrefix.TrimEnd('/');
        if (!legacyPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return legacyPath;

        var remaining = legacyPath[prefix.Length..];
        return $"{NewPathPrefix.TrimEnd('/')}{remaining}";
    }

    /// <summary>
    /// New path'i Legacy path'e dÃ¶nÃ¼ÅŸtÃ¼rÃ¼r
    /// Ã–rn: /api/auth/login/v2 -> /moim/api/v1/internet/auth/login/v2
    /// </summary>
    public string TransformToLegacyPath(string newPath)
    {
        if (string.IsNullOrEmpty(LegacyPathPrefix) || string.IsNullOrEmpty(NewPathPrefix))
            return newPath;

        var prefix = NewPathPrefix.TrimEnd('/');
        if (!newPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return newPath;

        var remaining = newPath[prefix.Length..];
        return $"{LegacyPathPrefix.TrimEnd('/')}{remaining}";
    }

    /// <summary>
    /// Aktif mi?
    /// </summary>
    [JsonProperty("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// AÃ§Ä±klama
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Bu modÃ¼le ait path'ler (birden fazla olabilir)
    /// </summary>
    [JsonProperty("paths")]
    public List<GatewayModulePath> Paths { get; set; } = new();
}

/// <summary>
/// T_GATEWAY_SWITCH tablosundan okunan yÃ¶nlendirme kuralÄ± (modÃ¼l iliÅŸkili)
/// </summary>
public class GatewaySwitchRule
{
    /// <summary>
    /// VeritabanÄ± ID
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    /// ModÃ¼l ID (T_GATEWAY_MODULE.ID)
    /// </summary>
    [JsonProperty("moduleId")]
    public int ModuleId { get; set; }

    /// <summary>
    /// ModÃ¼l kodu (join ile gelir)
    /// </summary>
    [JsonProperty("moduleCode")]
    public string ModuleCode { get; set; } = string.Empty;

    /// <summary>
    /// ModÃ¼l adÄ± (join ile gelir)
    /// </summary>
    [JsonProperty("moduleName")]
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// Legacy path prefix (modÃ¼lden gelir)
    /// </summary>
    [JsonProperty("legacyPathPrefix")]
    public string LegacyPathPrefix { get; set; } = string.Empty;

    /// <summary>
    /// New path prefix (modÃ¼lden gelir)
    /// </summary>
    [JsonProperty("newPathPrefix")]
    public string NewPathPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Metot pattern (* = tÃ¼m metotlar, veya spesifik metot adÄ±)
    /// </summary>
    [JsonProperty("methodPattern")]
    public string MethodPattern { get; set; } = "*";

    /// <summary>
    /// Hedef sistem (NEW, LEGACY, SPLIT)
    /// </summary>
    [JsonProperty("targetSystem")]
    public string TargetSystem { get; set; } = "LEGACY";

    /// <summary>
    /// SPLIT modunda yeni sisteme yÃ¶nlendirilecek yÃ¼zde (0-100)
    /// </summary>
    [JsonProperty("newPercentage")]
    public int NewPercentage { get; set; }

    /// <summary>
    /// Kural aktif mi?
    /// </summary>
    [JsonProperty("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Ã–ncelik (dÃ¼ÅŸÃ¼k deÄŸer = yÃ¼ksek Ã¶ncelik)
    /// </summary>
    [JsonProperty("priority")]
    public int Priority { get; set; } = 100;

    /// <summary>
    /// AÃ§Ä±klama
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Son gÃ¼ncelleme tarihi
    /// </summary>
    [JsonProperty("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// GÃ¶rÃ¼ntÃ¼leme sÄ±rasÄ± (modÃ¼lden gelir)
    /// </summary>
    [JsonProperty("displayOrder")]
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Cache key'i oluÅŸturur
    /// </summary>
    public string GetCacheKey() => $"switch:{ModuleCode}:{MethodPattern}".ToLowerInvariant();

    /// <summary>
    /// Bu istek iÃ§in hedef sistemi belirler (SPLIT modunda rastgele)
    /// </summary>
    public string ResolveTarget()
    {
        if (TargetSystem.Equals("SPLIT", StringComparison.OrdinalIgnoreCase))
        {
            var random = Random.Shared.Next(1, 101);
            return random <= NewPercentage ? "NEW" : "LEGACY";
        }

        return TargetSystem.ToUpperInvariant();
    }

    /// <summary>
    /// Legacy path'i New path'e dÃ¶nÃ¼ÅŸtÃ¼rÃ¼r (modÃ¼l prefix bilgilerini kullanarak)
    /// </summary>
    public string TransformToNewPath(string legacyPath)
    {
        if (string.IsNullOrEmpty(LegacyPathPrefix) || string.IsNullOrEmpty(NewPathPrefix))
            return legacyPath;

        var prefix = LegacyPathPrefix.TrimEnd('/');
        if (!legacyPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return legacyPath;

        var remaining = legacyPath[prefix.Length..];
        return $"{NewPathPrefix.TrimEnd('/')}";
    }

    /// <summary>
    /// New path'i Legacy path'e dÃ¶nÃ¼ÅŸtÃ¼rÃ¼r (modÃ¼l prefix bilgilerini kullanarak)
    /// </summary>
    public string TransformToLegacyPath(string newPath)
    {
        if (string.IsNullOrEmpty(LegacyPathPrefix) || string.IsNullOrEmpty(NewPathPrefix))
            return newPath;

        var prefix = NewPathPrefix.TrimEnd('/');
        if (!newPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return newPath;

        var remaining = newPath[prefix.Length..];
        return $"{LegacyPathPrefix.TrimEnd('/')}{remaining}";
    }
}

