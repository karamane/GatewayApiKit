using System.Collections.Generic;

namespace ApiGatewayKit.Gateway.Services.DownstreamHealth;

/// <summary>
/// Downstream servislerin sağlık durumlarını yöneten merkezi kayıt defteri.
/// Thread-safe singleton olarak implemente edilmelidir.
/// </summary>
public interface ISystemStatusRegistry
{
    /// <summary>
    /// Belirli bir servisin sağlık durumunu günceller.
    /// </summary>
    /// <param name="status">Güncel sağlık durumu</param>
    void UpdateStatus(DownstreamHealthStatus status);

    /// <summary>
    /// Belirli bir servisin sağlık durumunu getirir.
    /// </summary>
    /// <param name="serviceId">Servis tanımlayıcısı</param>
    /// <returns>Sağlık durumu veya null</returns>
    DownstreamHealthStatus? GetStatus(string serviceId);

    /// <summary>
    /// Tüm servislerin sağlık durumlarını getirir.
    /// </summary>
    /// <returns>Sağlık durumları listesi</returns>
    IReadOnlyList<DownstreamHealthStatus> GetAllStatuses();

    /// <summary>
    /// Belirli bir hedef sistemin (Legacy/New) tüm servislerinin durumlarını getirir.
    /// </summary>
    /// <param name="targetSystem">Hedef sistem (Legacy veya New)</param>
    /// <returns>Sağlık durumları listesi</returns>
    IReadOnlyList<DownstreamHealthStatus> GetStatusesByTarget(string targetSystem);

    /// <summary>
    /// Herhangi bir servisin kritik durumda olup olmadığını kontrol eder.
    /// </summary>
    /// <returns>True ise en az bir servis kritik durumda</returns>
    bool HasCriticalAlerts();

    /// <summary>
    /// Kritik durumdaki servislerin listesini getirir.
    /// </summary>
    /// <returns>Kritik durumdaki servisler</returns>
    IReadOnlyList<DownstreamHealthStatus> GetCriticalServices();

    /// <summary>
    /// Genel sistem sağlık özeti
    /// </summary>
    /// <returns>Sistem sağlık özeti</returns>
    SystemHealthSummary GetSummary();
}

/// <summary>
/// Genel sistem sağlık özeti
/// </summary>
public sealed record SystemHealthSummary
{
    /// <summary>
    /// Toplam izlenen servis sayısı
    /// </summary>
    public int TotalServices { get; init; }

    /// <summary>
    /// Çevrimiçi servis sayısı
    /// </summary>
    public int OnlineServices { get; init; }

    /// <summary>
    /// Çevrimdışı servis sayısı
    /// </summary>
    public int OfflineServices { get; init; }

    /// <summary>
    /// Legacy sistemin genel durumu
    /// </summary>
    public bool LegacyHealthy { get; init; }

    /// <summary>
    /// Yeni sistemin genel durumu
    /// </summary>
    public bool NewSystemHealthy { get; init; }

    /// <summary>
    /// Son güncelleme zamanı (UTC)
    /// </summary>
    public DateTime LastUpdatedUtc { get; init; }

    /// <summary>
    /// Kritik alarm var mı?
    /// </summary>
    public bool HasCriticalAlerts { get; init; }
}
