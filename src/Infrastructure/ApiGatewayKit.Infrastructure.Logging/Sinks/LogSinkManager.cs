using ApiGatewayKit.Core.Application.Models.Logging;
using Microsoft.Extensions.Logging;

namespace ApiGatewayKit.Infrastructure.Logging.Sinks;

/// <summary>
/// Multi-sink log yÃ¶neticisi
/// </summary>
public class LogSinkManager : ILogSinkManager
{
    private readonly IEnumerable<ILogSink> _sinks;
    private readonly ILogger<LogSinkManager> _logger;

    public LogSinkManager(
        IEnumerable<ILogSink> sinks,
        ILogger<LogSinkManager> logger)
    {
        _sinks = sinks;
        _logger = logger;
    }

    public async Task WriteAsync(IEnumerable<BaseLogEntry> entries, CancellationToken cancellationToken = default)
    {
        var entriesList = entries.ToList();
        if (entriesList.Count == 0) return;

        var activeSinks = _sinks.Where(s => s.IsEnabled).ToList();

        var tasks = activeSinks.Select(async sink =>
        {
            try
            {
                await sink.WriteBatchAsync(entriesList, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Log sink {SinkName} failed to write {Count} entries",
                    sink.Name, entriesList.Count);
            }
        });

        await Task.WhenAll(tasks);
    }

    public async Task WriteToSinkAsync(
        string sinkName,
        IEnumerable<BaseLogEntry> entries,
        CancellationToken cancellationToken = default)
    {
        var sink = _sinks.FirstOrDefault(s => s.Name.Equals(sinkName, StringComparison.OrdinalIgnoreCase));

        if (sink == null)
        {
            _logger.LogWarning("Log sink {SinkName} not found", sinkName);
            return;
        }

        if (!sink.IsEnabled)
        {
            _logger.LogDebug("Log sink {SinkName} is disabled", sinkName);
            return;
        }

        try
        {
            await sink.WriteBatchAsync(entries, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log sink {SinkName} failed", sinkName);
        }
    }
}

