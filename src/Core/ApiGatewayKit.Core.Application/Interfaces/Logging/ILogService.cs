using ApiGatewayKit.Core.Application.Models.Logging;

namespace ApiGatewayKit.Core.Application.Interfaces.Logging;

/// <summary>
/// Loglama servisi interface'i
/// FarklÄ± log tÃ¼rlerini yÃ¶netir
/// </summary>
public interface ILogService
{
    /// <summary>
    /// Request loglar
    /// </summary>
    Task LogRequestAsync(RequestLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Response loglar
    /// </summary>
    Task LogResponseAsync(ResponseLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exception loglar
    /// </summary>
    Task LogExceptionAsync(ExceptionLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Business exception loglar
    /// </summary>
    Task LogBusinessExceptionAsync(BusinessExceptionLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Audit loglar
    /// </summary>
    Task LogAuditAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performance loglar
    /// </summary>
    Task LogPerformanceAsync(PerformanceLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generic log
    /// </summary>
    Task LogAsync<T>(T entry, CancellationToken cancellationToken = default) where T : BaseLogEntry;
}


