using System;

namespace ApiGatewayKit.Gateway.Services.DownstreamHealth;

/// <summary>
/// Downstream servisin sağlık durumunu temsil eder.
/// </summary>
public sealed record DownstreamHealthStatus
{
    /// <summary>
    /// Servis tanımlayıcısı (örn: "legacy-1", "new-1")
    /// </summary>
    public required string ServiceId { get; init; }

    /// <summary>
    /// Hedef sistem tipi (Legacy veya New)
    /// </summary>
    public required string TargetSystem { get; init; }

    /// <summary>
    /// Servisin base URL'i
    /// </summary>
    public required string BaseUrl { get; init; }

    /// <summary>
    /// Servisin çevrimiçi olup olmadığı
    /// </summary>
    public bool IsOnline { get; init; }

    /// <summary>
    /// Servisten dönen durum metni (örn: "Online", "Offline", "Error")
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Servisin uygulama kimliği/versiyonu
    /// </summary>
    public string? ApplicationId { get; init; }

    /// <summary>
    /// Servisin versiyon bilgisi
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Son kontrol zamanı (UTC)
    /// </summary>
    public DateTime LastCheckedUtc { get; init; }

    /// <summary>
    /// Son başarılı kontrol zamanı (UTC)
    /// </summary>
    public DateTime? LastOnlineUtc { get; init; }

    /// <summary>
    /// Kontrol sırasında oluşan hata mesajı
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// HTTP yanıt süresi (milisaniye)
    /// </summary>
    public long? ResponseTimeMs { get; init; }

    /// <summary>
    /// Ardışık başarısız kontrol sayısı
    /// </summary>
    public int ConsecutiveFailures { get; init; }
}

/// <summary>
/// Downstream servisin ping yanıtı (/ endpoint'inden dönen JSON)
/// </summary>
public sealed record DownstreamPingResponse
{
    /// <summary>
    /// Servis durumu (örn: "Online")
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Uygulama kimliği
    /// </summary>
    public string? ApplicationId { get; init; }

    /// <summary>
    /// Versiyon bilgisi
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Sunucu adı
    /// </summary>
    public string? ServerName { get; init; }

    /// <summary>
    /// Ek bilgiler
    /// </summary>
    public string? Message { get; init; }
}
