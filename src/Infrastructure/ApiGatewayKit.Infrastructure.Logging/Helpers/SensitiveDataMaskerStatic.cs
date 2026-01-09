using Microsoft.Extensions.Options;

namespace ApiGatewayKit.Infrastructure.Logging.Helpers;

/// <summary>
/// SensitiveDataMasker iÃ§in static helper class
/// DI olmadan kullanÄ±m iÃ§in (eski kod uyumluluÄŸu)
/// </summary>
public static class SensitiveDataMaskerStatic
{
    private static readonly Lazy<SensitiveDataMasker> _instance = new(() =>
        new SensitiveDataMasker(Microsoft.Extensions.Options.Options.Create(new SensitiveDataOptions())));

    /// <summary>
    /// JSON iÃ§indeki hassas alanlarÄ± maskeler
    /// </summary>
    public static string? MaskJson(string? json) => _instance.Value.MaskJson(json);

    /// <summary>
    /// JSON iÃ§indeki hassas alanlarÄ± maskeler (ek alanlar ile)
    /// </summary>
    public static string? MaskJson(string? json, string[]? additionalSensitiveFields) 
        => _instance.Value.MaskJson(json);

    /// <summary>
    /// Text iÃ§indeki hassas verileri maskeler
    /// </summary>
    public static string MaskText(string text) => _instance.Value.MaskText(text);

    /// <summary>
    /// Log injection saldÄ±rÄ±larÄ±nÄ± Ã¶nler
    /// </summary>
    public static string SanitizeForLogging(string? input) => _instance.Value.SanitizeForLogging(input);

    /// <summary>
    /// Dictionary iÃ§indeki hassas alanlarÄ± maskeler
    /// </summary>
    public static Dictionary<string, string> MaskDictionary(Dictionary<string, string>? dictionary) 
        => _instance.Value.MaskDictionary(dictionary);
}


