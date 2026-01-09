using System.Text.Json;
using System.Threading.Channels;
using ApiGatewayKit.Core.Application.Interfaces.Logging;
using ApiGatewayKit.Core.Application.Models.Logging;
using ApiGatewayKit.Infrastructure.Logging.Sinks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiGatewayKit.Infrastructure.Logging.Services;

/// <summary>
/// Log servisi implementasyonu
/// Async, non-blocking log yazÄ±mÄ± saÄŸlar
/// </summary>
public class LogService : ILogService, IAsyncDisposable
{
    private readonly ILogger<LogService> _logger;
    private readonly ICorrelationContext _correlationContext;
    private readonly LoggingOptions _options;
    private readonly Channel<BaseLogEntry> _logChannel;
    private readonly ILogSinkManager _sinkManager;
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _cts;

    public LogService(
        ILogger<LogService> logger,
        ICorrelationContext correlationContext,
        IOptions<LoggingOptions> options,
        ILogSinkManager sinkManager)
    {
        _logger = logger;
        _correlationContext = correlationContext;
        _options = options.Value;
        _sinkManager = sinkManager;

        _cts = new CancellationTokenSource();
        _logChannel = Channel.CreateBounded<BaseLogEntry>(
            new BoundedChannelOptions(_options.BufferSize)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

        // Background log processor baÅŸlat
        _processingTask = Task.Run(ProcessLogsAsync);
    }

    public async Task LogRequestAsync(RequestLogEntry entry, CancellationToken cancellationToken = default)
    {
        EnrichLogEntry(entry);
        await EnqueueLogAsync(entry, cancellationToken);
    }

    public async Task LogResponseAsync(ResponseLogEntry entry, CancellationToken cancellationToken = default)
    {
        EnrichLogEntry(entry);
        await EnqueueLogAsync(entry, cancellationToken);
    }

    public async Task LogExceptionAsync(ExceptionLogEntry entry, CancellationToken cancellationToken = default)
    {
        EnrichLogEntry(entry);
        await EnqueueLogAsync(entry, cancellationToken);

        // Critical exception'larÄ± hemen serilog'a da yaz
        _logger.LogError(
            "[{CorrelationId}] Exception: {ExceptionType} - {ExceptionMessage}",
            entry.CorrelationId, entry.ExceptionType, entry.ExceptionMessage);
    }

    public async Task LogBusinessExceptionAsync(BusinessExceptionLogEntry entry, CancellationToken cancellationToken = default)
    {
        EnrichLogEntry(entry);
        await EnqueueLogAsync(entry, cancellationToken);
    }

    public async Task LogAuditAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        EnrichLogEntry(entry);
        await EnqueueLogAsync(entry, cancellationToken);
    }

    public async Task LogPerformanceAsync(PerformanceLogEntry entry, CancellationToken cancellationToken = default)
    {
        EnrichLogEntry(entry);
        await EnqueueLogAsync(entry, cancellationToken);
    }

    public async Task LogAsync<T>(T entry, CancellationToken cancellationToken = default) where T : BaseLogEntry
    {
        EnrichLogEntry(entry);
        await EnqueueLogAsync(entry, cancellationToken);
    }

    /// <summary>
    /// Log entry'yi context bilgileri ile zenginleÅŸtirir
    /// </summary>
    private void EnrichLogEntry(BaseLogEntry entry)
    {
        entry.CorrelationId = _correlationContext.CorrelationId;
        entry.ParentCorrelationId = _correlationContext.ParentCorrelationId;
        entry.UserId = _correlationContext.UserId;
        entry.ClientIp = _correlationContext.ClientIp;
        entry.ServerName = _correlationContext.ServerName;
        entry.ServerIp = _correlationContext.ServerIp;
        entry.SessionId = _correlationContext.SessionId;
        entry.Environment = _options.Environment;
        entry.ApplicationVersion = _options.ApplicationVersion;

        if (string.IsNullOrEmpty(entry.ApplicationName))
        {
            entry.ApplicationName = _options.ApplicationName;
        }
    }

    /// <summary>
    /// Log entry'yi queue'ya ekler
    /// </summary>
    private async Task EnqueueLogAsync(BaseLogEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await _logChannel.Writer.WriteAsync(entry, cancellationToken);
        }
        catch (ChannelClosedException)
        {
            // Channel kapanmÄ±ÅŸ, direkt yaz
            _logger.LogWarning("Log channel closed, writing directly: {LogType}", entry.LogType);
        }
    }

    /// <summary>
    /// Background log processor
    /// </summary>
    private async Task ProcessLogsAsync()
    {
        var batch = new List<BaseLogEntry>();

        try
        {
            await foreach (var entry in _logChannel.Reader.ReadAllAsync(_cts.Token))
            {
                batch.Add(entry);

                // Batch dolu veya channel boÅŸ
                if (batch.Count >= _options.BatchSize || 
                    !_logChannel.Reader.TryPeek(out _))
                {
                    await FlushBatchAsync(batch);
                    batch.Clear();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            // Kalan loglarÄ± yaz
            if (batch.Count > 0)
            {
                await FlushBatchAsync(batch);
            }
        }
    }

    /// <summary>
    /// Batch'i tÃ¼m sink'lere yazar
    /// </summary>
    private async Task FlushBatchAsync(List<BaseLogEntry> batch)
    {
        try
        {
            await _sinkManager.WriteAsync(batch, _cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing log batch of {Count} entries", batch.Count);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logChannel.Writer.Complete();
        _cts.Cancel();

        try
        {
            await _processingTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        _cts.Dispose();
    }
}

/// <summary>
/// Logging options
/// </summary>
public class LoggingOptions
{
    public const string SectionName = "Logging";

    public string ApplicationName { get; set; } = "ApiGatewayKit";
    public string? ApplicationVersion { get; set; }
    public string Environment { get; set; } = "Development";
    public int BufferSize { get; set; } = 10000;
    public int BatchSize { get; set; } = 100;
    public int FlushIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Database log yapÄ±landÄ±rmasÄ±
    /// </summary>
    public DatabaseLogOptions Database { get; set; } = new();

    /// <summary>
    /// ELK log yapÄ±landÄ±rmasÄ±
    /// </summary>
    public ElkLogOptions Elk { get; set; } = new();

    /// <summary>
    /// Maskelenmesi gereken alanlar
    /// </summary>
    public string[] SensitiveFields { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Request body max length
    /// </summary>
    public int RequestBodyMaxLength { get; set; } = 32768;

    /// <summary>
    /// Response body max length
    /// </summary>
    public int ResponseBodyMaxLength { get; set; } = 32768;
}

/// <summary>
/// Database log options
/// </summary>
public class DatabaseLogOptions
{
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Database provider: SqlServer, Oracle
    /// </summary>
    public string Provider { get; set; } = "SqlServer";
    
    /// <summary>
    /// Provider'a gÃ¶re connection string'ler
    /// </summary>
    public Dictionary<string, string> ConnectionStrings { get; set; } = new();
    
    /// <summary>
    /// Aktif provider'Ä±n connection string'i
    /// </summary>
    public string? ConnectionString => ConnectionStrings.TryGetValue(Provider, out var cs) 
        ? cs 
        : ConnectionStrings.Values.FirstOrDefault();
    
    public int BatchSize { get; set; } = 100;
    public int FlushIntervalSeconds { get; set; } = 5;
    
    /// <summary>
    /// Hangi log tiplerinin database'e yazÄ±lacaÄŸÄ±
    /// </summary>
    public LogTypeOptions LogTypes { get; set; } = new();
}

/// <summary>
/// Log tipi aÃ§ma/kapama seÃ§enekleri
/// File ve Database iÃ§in ayrÄ± ayrÄ± yapÄ±landÄ±rÄ±labilir
/// </summary>
public class LogTypeOptions
{
    /// <summary>
    /// Request loglarÄ± (HTTP request bilgileri)
    /// </summary>
    public bool RequestLogs { get; set; } = true;
    
    /// <summary>
    /// Response loglarÄ± (HTTP response bilgileri)
    /// </summary>
    public bool ResponseLogs { get; set; } = true;
    
    /// <summary>
    /// Exception loglarÄ± (sistem hatalarÄ±)
    /// </summary>
    public bool ExceptionLogs { get; set; } = true;
    
    /// <summary>
    /// Business exception loglarÄ± (iÅŸ kuralÄ± hatalarÄ±)
    /// </summary>
    public bool BusinessExceptionLogs { get; set; } = true;
    
    /// <summary>
    /// Audit loglarÄ± (veri deÄŸiÅŸiklikleri)
    /// </summary>
    public bool AuditLogs { get; set; } = true;
    
    /// <summary>
    /// Performance loglarÄ± (yavaÅŸ sorgular vb.)
    /// </summary>
    public bool PerformanceLogs { get; set; } = true;
    
    /// <summary>
    /// Security loglarÄ± (auth, yetkilendirme)
    /// </summary>
    public bool SecurityLogs { get; set; } = true;
}

/// <summary>
/// ELK log options
/// </summary>
public class ElkLogOptions
{
    public bool Enabled { get; set; }
    public string ElasticsearchUrl { get; set; } = "http://localhost:9200";
    public string? IndexFormat { get; set; }
    public int? NumberOfShards { get; set; }
    public int? NumberOfReplicas { get; set; }
    public int BatchSize { get; set; } = 50;
    public int FlushIntervalSeconds { get; set; } = 2;
    public string? BufferPath { get; set; }
    public int? BufferSizeLimitMb { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseSSL { get; set; }
}


