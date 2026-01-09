using System.Text.Json.Serialization;

namespace ApiGatewayKit.Gateway.Configuration;

/// <summary>
/// Yonlendirme kurali - T_Parameter tablosundan cache'e yuklenir
/// </summary>
public class RoutingRule
{
    /// <summary>
    /// URL pattern (ornek: /api/auth/*)
    /// </summary>
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Yonlendirme hedefi
    /// </summary>
    [JsonPropertyName("target")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RoutingTarget Target { get; set; } = RoutingTarget.Legacy;

    /// <summary>
    /// Eski sistem URL'i
    /// </summary>
    [JsonPropertyName("legacyUrl")]
    public string? LegacyUrl { get; set; }

    /// <summary>
    /// Yeni sistem URL'i
    /// </summary>
    [JsonPropertyName("newUrl")]
    public string? NewUrl { get; set; }

    /// <summary>
    /// Split modunda yeni sisteme yonlendirilecek yuzde (0-100)
    /// </summary>
    [JsonPropertyName("percentage")]
    public int Percentage { get; set; }

    /// <summary>
    /// Kural aktif mi?
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Oncelik (dusuk deger = yuksek oncelik)
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Son guncelleme zamani
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.MinValue;
}



