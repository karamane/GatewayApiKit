using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApiGatewayKit.Gateway.Services.DownstreamHealth;

namespace ApiGatewayKit.Gateway.Controllers;

/// <summary>
/// Downstream servislerin sağlık durumlarını sağlayan Admin API controller'ı.
/// </summary>
[ApiController]
[Route("api/gateway/admin/system-health")]
public sealed class SystemHealthController : ControllerBase
{
    private readonly ISystemStatusRegistry _statusRegistry;
    private readonly ILogger<SystemHealthController> _logger;

    public SystemHealthController(
        ISystemStatusRegistry statusRegistry,
        ILogger<SystemHealthController> logger)
    {
        _statusRegistry = statusRegistry ?? throw new ArgumentNullException(nameof(statusRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tüm downstream servislerin sağlık durumlarını ve genel özeti döner.
    /// </summary>
    /// <returns>Sistem sağlık raporu</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SystemHealthResponse), 200)]
    public ActionResult<SystemHealthResponse> GetSystemHealth()
    {
        _logger.LogDebug("Sistem sağlık durumu sorgulanıyor");

        var summary = _statusRegistry.GetSummary();
        var allStatuses = _statusRegistry.GetAllStatuses();

        var response = new SystemHealthResponse
        {
            Ozet = new SystemHealthSummaryDto
            {
                ToplamServis = summary.TotalServices,
                CevrimiciServis = summary.OnlineServices,
                CevrimdisiServis = summary.OfflineServices,
                LegacySaglikli = summary.LegacyHealthy,
                YeniSistemSaglikli = summary.NewSystemHealthy,
                SonGuncelleme = summary.LastUpdatedUtc,
                KritikAlarmVar = summary.HasCriticalAlerts
            },
            Legacy = allStatuses
                .Where(s => string.Equals(s.TargetSystem, "Legacy", StringComparison.OrdinalIgnoreCase))
                .Select(MapToDto)
                .ToList(),
            Yeni = allStatuses
                .Where(s => string.Equals(s.TargetSystem, "New", StringComparison.OrdinalIgnoreCase))
                .Select(MapToDto)
                .ToList(),
            KritikServisler = _statusRegistry.GetCriticalServices()
                .Select(MapToDto)
                .ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Sadece sistem sağlık özetini döner (hızlı durum kontrolü için).
    /// </summary>
    /// <returns>Sistem sağlık özeti</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(SystemHealthSummaryDto), 200)]
    public ActionResult<SystemHealthSummaryDto> GetSummary()
    {
        var summary = _statusRegistry.GetSummary();

        return Ok(new SystemHealthSummaryDto
        {
            ToplamServis = summary.TotalServices,
            CevrimiciServis = summary.OnlineServices,
            CevrimdisiServis = summary.OfflineServices,
            LegacySaglikli = summary.LegacyHealthy,
            YeniSistemSaglikli = summary.NewSystemHealthy,
            SonGuncelleme = summary.LastUpdatedUtc,
            KritikAlarmVar = summary.HasCriticalAlerts
        });
    }

    /// <summary>
    /// Belirli bir hedef sistemin (Legacy/New) servis durumlarını döner.
    /// </summary>
    /// <param name="target">Hedef sistem (legacy veya new)</param>
    /// <returns>Servis durumları listesi</returns>
    [HttpGet("{target}")]
    [ProducesResponseType(typeof(IEnumerable<ServiceHealthDto>), 200)]
    [ProducesResponseType(400)]
    public ActionResult<IEnumerable<ServiceHealthDto>> GetByTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return BadRequest("Hedef sistem belirtilmelidir (legacy veya new).");
        }

        // Normalize target
        string normalizedTarget = target.Trim().ToLowerInvariant() switch
        {
            "legacy" => "Legacy",
            "new" or "yeni" => "New",
            _ => target.Trim()
        };

        var statuses = _statusRegistry.GetStatusesByTarget(normalizedTarget);

        return Ok(statuses.Select(MapToDto));
    }

    /// <summary>
    /// Kritik durumdaki servisleri döner.
    /// </summary>
    /// <returns>Kritik servisler listesi</returns>
    [HttpGet("critical")]
    [ProducesResponseType(typeof(IEnumerable<ServiceHealthDto>), 200)]
    public ActionResult<IEnumerable<ServiceHealthDto>> GetCriticalServices()
    {
        var criticalServices = _statusRegistry.GetCriticalServices();
        return Ok(criticalServices.Select(MapToDto));
    }

    /// <summary>
    /// Belirli bir servisin sağlık durumunu döner.
    /// </summary>
    /// <param name="serviceId">Servis tanımlayıcısı</param>
    /// <returns>Servis sağlık durumu</returns>
    [HttpGet("service/{serviceId}")]
    [ProducesResponseType(typeof(ServiceHealthDto), 200)]
    [ProducesResponseType(404)]
    public ActionResult<ServiceHealthDto> GetServiceStatus(string serviceId)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
        {
            return BadRequest("Servis ID'si belirtilmelidir.");
        }

        var status = _statusRegistry.GetStatus(serviceId.Trim());

        if (status == null)
        {
            return NotFound($"'{serviceId}' servis bulunamadı.");
        }

        return Ok(MapToDto(status));
    }

    private static ServiceHealthDto MapToDto(DownstreamHealthStatus status)
    {
        return new ServiceHealthDto
        {
            ServisId = status.ServiceId,
            HedefSistem = status.TargetSystem,
            BaseUrl = status.BaseUrl,
            Cevrimici = status.IsOnline,
            Durum = status.Status,
            UygulamaKimligi = status.ApplicationId,
            Versiyon = status.Version,
            SonKontrol = status.LastCheckedUtc,
            SonCevrimici = status.LastOnlineUtc,
            YanitSuresiMs = status.ResponseTimeMs,
            ArdisikHataSayisi = status.ConsecutiveFailures,
            HataMesaji = status.ErrorMessage,
            Kritik = status.ConsecutiveFailures >= 3
        };
    }
}

#region DTOs

/// <summary>
/// Sistem sağlık yanıtı
/// </summary>
public sealed class SystemHealthResponse
{
    /// <summary>
    /// Genel özet
    /// </summary>
    public required SystemHealthSummaryDto Ozet { get; init; }

    /// <summary>
    /// Legacy sistem servisleri
    /// </summary>
    public required List<ServiceHealthDto> Legacy { get; init; }

    /// <summary>
    /// Yeni sistem servisleri
    /// </summary>
    public required List<ServiceHealthDto> Yeni { get; init; }

    /// <summary>
    /// Kritik durumdaki servisler
    /// </summary>
    public required List<ServiceHealthDto> KritikServisler { get; init; }
}

/// <summary>
/// Sistem sağlık özeti DTO
/// </summary>
public sealed class SystemHealthSummaryDto
{
    /// <summary>
    /// Toplam izlenen servis sayısı
    /// </summary>
    public int ToplamServis { get; init; }

    /// <summary>
    /// Çevrimiçi servis sayısı
    /// </summary>
    public int CevrimiciServis { get; init; }

    /// <summary>
    /// Çevrimdışı servis sayısı
    /// </summary>
    public int CevrimdisiServis { get; init; }

    /// <summary>
    /// Legacy sistemin genel durumu
    /// </summary>
    public bool LegacySaglikli { get; init; }

    /// <summary>
    /// Yeni sistemin genel durumu
    /// </summary>
    public bool YeniSistemSaglikli { get; init; }

    /// <summary>
    /// Son güncelleme zamanı (UTC)
    /// </summary>
    public DateTime SonGuncelleme { get; init; }

    /// <summary>
    /// Kritik alarm var mı?
    /// </summary>
    public bool KritikAlarmVar { get; init; }
}

/// <summary>
/// Servis sağlık durumu DTO
/// </summary>
public sealed class ServiceHealthDto
{
    /// <summary>
    /// Servis tanımlayıcısı
    /// </summary>
    public required string ServisId { get; init; }

    /// <summary>
    /// Hedef sistem (Legacy/New)
    /// </summary>
    public required string HedefSistem { get; init; }

    /// <summary>
    /// Servisin base URL'i
    /// </summary>
    public required string BaseUrl { get; init; }

    /// <summary>
    /// Çevrimiçi durumu
    /// </summary>
    public bool Cevrimici { get; init; }

    /// <summary>
    /// Durum metni
    /// </summary>
    public string? Durum { get; init; }

    /// <summary>
    /// Uygulama kimliği
    /// </summary>
    public string? UygulamaKimligi { get; init; }

    /// <summary>
    /// Versiyon bilgisi
    /// </summary>
    public string? Versiyon { get; init; }

    /// <summary>
    /// Son kontrol zamanı (UTC)
    /// </summary>
    public DateTime SonKontrol { get; init; }

    /// <summary>
    /// Son çevrimiçi zamanı (UTC)
    /// </summary>
    public DateTime? SonCevrimici { get; init; }

    /// <summary>
    /// HTTP yanıt süresi (ms)
    /// </summary>
    public long? YanitSuresiMs { get; init; }

    /// <summary>
    /// Ardışık hata sayısı
    /// </summary>
    public int ArdisikHataSayisi { get; init; }

    /// <summary>
    /// Hata mesajı
    /// </summary>
    public string? HataMesaji { get; init; }

    /// <summary>
    /// Kritik durumda mı?
    /// </summary>
    public bool Kritik { get; init; }
}

#endregion
