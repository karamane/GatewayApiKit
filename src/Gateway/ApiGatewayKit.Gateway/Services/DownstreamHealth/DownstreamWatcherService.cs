using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApiGatewayKit.Core.Application.Interfaces.Routing;
using ApiGatewayKit.Core.Application.Models.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiGatewayKit.Gateway.Services.DownstreamHealth;

/// <summary>
/// Downstream servislerin sağlık durumunu periyodik olarak kontrol eden arka plan servisi.
/// </summary>
public sealed class DownstreamWatcherService : BackgroundService
{
    private readonly ILogger<DownstreamWatcherService> _logger;
    private readonly IDownstreamHealthChecker _healthChecker;
    private readonly ISystemStatusRegistry _statusRegistry;
    private readonly IGatewayTargetsProvider _targetsProvider;
    private readonly IOptionsMonitor<DownstreamWatcherOptions> _options;

    public DownstreamWatcherService(
        ILogger<DownstreamWatcherService> logger,
        IDownstreamHealthChecker healthChecker,
        ISystemStatusRegistry statusRegistry,
        IGatewayTargetsProvider targetsProvider,
        IOptionsMonitor<DownstreamWatcherOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _healthChecker = healthChecker ?? throw new ArgumentNullException(nameof(healthChecker));
        _statusRegistry = statusRegistry ?? throw new ArgumentNullException(nameof(statusRegistry));
        _targetsProvider = targetsProvider ?? throw new ArgumentNullException(nameof(targetsProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Downstream Watcher Service başlatılıyor. Kontrol periyodu: {Interval} saniye",
            _options.CurrentValue.CheckIntervalSeconds);

        // İlk kontrolü hemen yap
        await PerformHealthChecksAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var interval = TimeSpan.FromSeconds(_options.CurrentValue.CheckIntervalSeconds);
                await Task.Delay(interval, stoppingToken);

                await PerformHealthChecksAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Downstream Watcher Service durduruluyor.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Downstream Watcher Service'de beklenmeyen hata.");
                
                // Hata durumunda kısa bir bekleme yap ve devam et
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task PerformHealthChecksAsync(CancellationToken cancellationToken)
    {
        var snapshot = _targetsProvider.GetSnapshot();
        var allNodes = new List<(string TargetSystem, GatewayTargetNode Node)>();

        // Legacy node'ları topla
        foreach (var node in snapshot.LegacyNodes)
        {
            allNodes.Add(("Legacy", node));
        }

        // New node'ları topla
        foreach (var node in snapshot.NewNodes)
        {
            allNodes.Add(("New", node));
        }

        if (allNodes.Count == 0)
        {
            _logger.LogWarning("Kontrol edilecek downstream servis bulunamadı.");
            return;
        }

        _logger.LogDebug("Sağlık kontrolü başlıyor: {Count} servis kontrol edilecek", allNodes.Count);

        // Paralel olarak tüm servisleri kontrol et
        var tasks = allNodes.Select(async item =>
        {
            try
            {
                var baseUri = item.Node.BaseUrl;
                if (baseUri == null || !baseUri.IsAbsoluteUri)
                {
                    _logger.LogWarning("Geçersiz BaseUrl: {ServiceId} -> {BaseUrl}", item.Node.Id, item.Node.BaseUrl);
                    
                    _statusRegistry.UpdateStatus(new DownstreamHealthStatus
                    {
                        ServiceId = item.Node.Id,
                        TargetSystem = item.TargetSystem,
                        BaseUrl = item.Node.BaseUrl?.ToString() ?? "N/A",
                        IsOnline = false,
                        Status = "Error",
                        LastCheckedUtc = DateTime.UtcNow,
                        ErrorMessage = "Geçersiz BaseUrl formatı"
                    });
                    return;
                }

                var status = await _healthChecker.CheckHealthAsync(
                    item.Node.Id,
                    item.TargetSystem,
                    baseUri,
                    cancellationToken);

                _statusRegistry.UpdateStatus(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis kontrolünde hata: {ServiceId}", item.Node.Id);
                
                _statusRegistry.UpdateStatus(new DownstreamHealthStatus
                {
                    ServiceId = item.Node.Id,
                    TargetSystem = item.TargetSystem,
                    BaseUrl = item.Node.BaseUrl?.ToString() ?? "N/A",
                    IsOnline = false,
                    Status = "Error",
                    LastCheckedUtc = DateTime.UtcNow,
                    ErrorMessage = $"Kontrol hatası: {ex.Message}"
                });
            }
        });

        await Task.WhenAll(tasks);

        // Özet logla
        var summary = _statusRegistry.GetSummary();
        
        if (summary.HasCriticalAlerts)
        {
            _logger.LogCritical(
                "KRİTİK: {Offline}/{Total} servis çevrimdışı! Legacy: {LegacyStatus}, Yeni: {NewStatus}",
                summary.OfflineServices,
                summary.TotalServices,
                summary.LegacyHealthy ? "Sağlıklı" : "SORUNLU",
                summary.NewSystemHealthy ? "Sağlıklı" : "SORUNLU");
        }
        else
        {
            _logger.LogInformation(
                "Sağlık kontrolü tamamlandı: {Online}/{Total} servis çevrimiçi. Legacy: {LegacyStatus}, Yeni: {NewStatus}",
                summary.OnlineServices,
                summary.TotalServices,
                summary.LegacyHealthy ? "Sağlıklı" : "Sorunlu",
                summary.NewSystemHealthy ? "Sağlıklı" : "Sorunlu");
        }
    }
}

/// <summary>
/// Downstream Watcher konfigürasyon seçenekleri
/// </summary>
public sealed class DownstreamWatcherOptions
{
    /// <summary>
    /// Kontrol periyodu (saniye). Varsayılan: 60 saniye (1 dakika)
    /// </summary>
    public int CheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Servis etkin mi?
    /// </summary>
    public bool Enabled { get; set; } = true;
}
