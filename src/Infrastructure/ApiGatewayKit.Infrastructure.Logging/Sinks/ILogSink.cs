using ApiGatewayKit.Core.Application.Models.Logging;

namespace ApiGatewayKit.Infrastructure.Logging.Sinks;

/// <summary>
/// Log sink interface
/// FarklÄ± hedeflere log yazmak iÃ§in
/// </summary>
public interface ILogSink
{
    /// <summary>
    /// Sink adÄ±
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Sink aktif mi?
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Tek bir log yazar
    /// </summary>
    Task WriteAsync(BaseLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch log yazar
    /// </summary>
    Task WriteBatchAsync(IEnumerable<BaseLogEntry> entries, CancellationToken cancellationToken = default);
}

/// <summary>
/// Log sink manager interface
/// </summary>
public interface ILogSinkManager
{
    /// <summary>
    /// TÃ¼m aktif sink'lere yazar
    /// </summary>
    Task WriteAsync(IEnumerable<BaseLogEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir sink'e yazar
    /// </summary>
    Task WriteToSinkAsync(string sinkName, IEnumerable<BaseLogEntry> entries, CancellationToken cancellationToken = default);
}


