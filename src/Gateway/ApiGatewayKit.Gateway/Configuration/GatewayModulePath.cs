using Newtonsoft.Json;

namespace ApiGatewayKit.Gateway.Configuration;

/// <summary>
/// T_GATEWAY_MODULE_PATH tablosundan okunan modÃ¼l path tanÄ±mÄ±
/// Her modÃ¼le birden fazla path atanabilir
/// </summary>
public class GatewayModulePath
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
    /// API path pattern (/api/auth/*, /api/token/*)
    /// </summary>
    [JsonProperty("pathPattern")]
    public string PathPattern { get; set; } = string.Empty;

    /// <summary>
    /// EÅŸleÅŸme Ã¶nceliÄŸi (dÃ¼ÅŸÃ¼k deÄŸer = yÃ¼ksek Ã¶ncelik)
    /// </summary>
    [JsonProperty("priority")]
    public int Priority { get; set; } = 100;

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
    /// Path pattern'Ä± wildcard olarak kontrol eder
    /// </summary>
    public bool MatchesPath(string requestPath)
    {
        if (string.IsNullOrEmpty(PathPattern))
            return false;

        // Exact match
        if (PathPattern.Equals(requestPath, StringComparison.OrdinalIgnoreCase))
            return true;

        // Wildcard match: /api/auth/* matches /api/auth/login
        if (PathPattern.EndsWith("/*"))
        {
            var prefix = PathPattern[..^2]; // Remove /*
            return requestPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        // Wildcard match: /api/auth* matches /api/auth, /api/auth/login
        if (PathPattern.EndsWith("*"))
        {
            var prefix = PathPattern[..^1]; // Remove *
            return requestPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}



