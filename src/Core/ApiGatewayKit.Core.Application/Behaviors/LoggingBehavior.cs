using System.Diagnostics;
using ApiGatewayKit.Core.Application.Interfaces.Logging;
using ApiGatewayKit.Core.Application.Models.Logging;
using ApiGatewayKit.Core.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApiGatewayKit.Core.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior - TÃ¼m request'leri loglar
/// Business katmanÄ±nda otomatik loglama saÄŸlar
/// </summary>
/// <typeparam name="TRequest">Request tipi</typeparam>
/// <typeparam name="TResponse">Response tipi</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICorrelationContext _correlationContext;
    private readonly ILogService _logService;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICorrelationContext correlationContext,
        ILogService logService)
    {
        _logger = logger;
        _correlationContext = correlationContext;
        _logService = logService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = _correlationContext.CorrelationId;

        _logger.LogInformation(
            "[{CorrelationId}] Handling {RequestName}",
            correlationId, requestName);

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();

            sw.Stop();

            _logger.LogInformation(
                "[{CorrelationId}] Handled {RequestName} in {ElapsedMs}ms",
                correlationId, requestName, sw.ElapsedMilliseconds);

            // Performance log - yavaÅŸ request'ler iÃ§in
            if (sw.ElapsedMilliseconds > 500) // 500ms threshold
            {
                await _logService.LogPerformanceAsync(new PerformanceLogEntry
                {
                    CorrelationId = correlationId,
                    Layer = LogConstants.Layers.Business,
                    OperationName = requestName,
                    DurationMs = sw.ElapsedMilliseconds,
                    IsSlowRequest = true,
                    SlowRequestThresholdMs = 500,
                    Message = $"Slow request detected: {requestName}"
                }, cancellationToken);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();

            _logger.LogError(ex,
                "[{CorrelationId}] Error handling {RequestName} after {ElapsedMs}ms",
                correlationId, requestName, sw.ElapsedMilliseconds);

            throw;
        }
    }
}


