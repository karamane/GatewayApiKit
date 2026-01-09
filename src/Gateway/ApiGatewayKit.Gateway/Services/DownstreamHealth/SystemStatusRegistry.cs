using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ApiGatewayKit.Gateway.Services.DownstreamHealth;

/// <summary>
/// Downstream servislerin sağlık durumlarını yöneten thread-safe singleton kayıt defteri.
/// </summary>
public sealed class SystemStatusRegistry : ISystemStatusRegistry
{
    private readonly ConcurrentDictionary<string, DownstreamHealthStatus> _statuses = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<SystemStatusRegistry> _logger;
    private readonly object _summaryLock = new();
    private DateTime _lastUpdatedUtc = DateTime.UtcNow;

    /// <summary>
    /// Ardışık başarısızlık eşiği - bu değerin üzerinde kritik alarm verilir
    /// </summary>
    private const int CriticalFailureThreshold = 3;

    public SystemStatusRegistry(ILogger<SystemStatusRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void UpdateStatus(DownstreamHealthStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (string.IsNullOrWhiteSpace(status.ServiceId))
        {
            throw new ArgumentException("ServiceId boş olamaz.", nameof(status));
        }

        _statuses.AddOrUpdate(
            status.ServiceId,
            status,
            (_, existing) =>
            {
                // Ardışık başarısızlık sayısını hesapla
                int consecutiveFailures = status.IsOnline ? 0 : existing.ConsecutiveFailures + 1;
                
                // Last online zamanını koru veya güncelle
                DateTime? lastOnline = status.IsOnline ? status.LastCheckedUtc : existing.LastOnlineUtc;

                return status with
                {
                    ConsecutiveFailures = consecutiveFailures,
                    LastOnlineUtc = lastOnline
                };
            });

        lock (_summaryLock)
        {
            _lastUpdatedUtc = DateTime.UtcNow;
        }

        // Kritik durum loglaması
        var currentStatus = _statuses[status.ServiceId];
        if (currentStatus.ConsecutiveFailures >= CriticalFailureThreshold)
        {
            _logger.LogCritical(
                "KRİTİK ALARM: {ServiceId} ({TargetSystem}) servisi {FailCount} ardışık kontrolde başarısız! Son hata: {Error}",
                status.ServiceId,
                status.TargetSystem,
                currentStatus.ConsecutiveFailures,
                status.ErrorMessage ?? "Bilinmiyor");
        }
        else if (!status.IsOnline)
        {
            _logger.LogWarning(
                "Servis çevrimdışı: {ServiceId} ({TargetSystem}) - {Error}",
                status.ServiceId,
                status.TargetSystem,
                status.ErrorMessage ?? "Bilinmiyor");
        }
        else
        {
            _logger.LogDebug(
                "Servis durumu güncellendi: {ServiceId} ({TargetSystem}) - Online: {IsOnline}, Yanıt: {ResponseTime}ms",
                status.ServiceId,
                status.TargetSystem,
                status.IsOnline,
                status.ResponseTimeMs);
        }
    }

    /// <inheritdoc />
    public DownstreamHealthStatus? GetStatus(string serviceId)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
        {
            return null;
        }

        return _statuses.TryGetValue(serviceId, out var status) ? status : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<DownstreamHealthStatus> GetAllStatuses()
    {
        return _statuses.Values.OrderBy(s => s.TargetSystem).ThenBy(s => s.ServiceId).ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<DownstreamHealthStatus> GetStatusesByTarget(string targetSystem)
    {
        if (string.IsNullOrWhiteSpace(targetSystem))
        {
            return Array.Empty<DownstreamHealthStatus>();
        }

        return _statuses.Values
            .Where(s => string.Equals(s.TargetSystem, targetSystem, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.ServiceId)
            .ToList();
    }

    /// <inheritdoc />
    public bool HasCriticalAlerts()
    {
        return _statuses.Values.Any(s => s.ConsecutiveFailures >= CriticalFailureThreshold);
    }

    /// <inheritdoc />
    public IReadOnlyList<DownstreamHealthStatus> GetCriticalServices()
    {
        return _statuses.Values
            .Where(s => s.ConsecutiveFailures >= CriticalFailureThreshold)
            .OrderByDescending(s => s.ConsecutiveFailures)
            .ToList();
    }

    /// <inheritdoc />
    public SystemHealthSummary GetSummary()
    {
        var allStatuses = _statuses.Values.ToList();
        var legacyStatuses = allStatuses.Where(s => string.Equals(s.TargetSystem, "Legacy", StringComparison.OrdinalIgnoreCase)).ToList();
        var newStatuses = allStatuses.Where(s => string.Equals(s.TargetSystem, "New", StringComparison.OrdinalIgnoreCase)).ToList();

        DateTime lastUpdated;
        lock (_summaryLock)
        {
            lastUpdated = _lastUpdatedUtc;
        }

        return new SystemHealthSummary
        {
            TotalServices = allStatuses.Count,
            OnlineServices = allStatuses.Count(s => s.IsOnline),
            OfflineServices = allStatuses.Count(s => !s.IsOnline),
            LegacyHealthy = legacyStatuses.Count == 0 || legacyStatuses.Any(s => s.IsOnline),
            NewSystemHealthy = newStatuses.Count == 0 || newStatuses.Any(s => s.IsOnline),
            LastUpdatedUtc = lastUpdated,
            HasCriticalAlerts = HasCriticalAlerts()
        };
    }
}
