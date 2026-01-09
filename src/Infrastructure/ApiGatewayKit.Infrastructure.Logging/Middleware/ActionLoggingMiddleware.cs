using System.Diagnostics;
using ApiGatewayKit.Core.Application.Interfaces.Logging;
using ApiGatewayKit.Core.Application.Models.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ApiGatewayKit.Infrastructure.Logging.Middleware;

/// <summary>
/// MVC Action seviyesinde otomatik loglama middleware
/// Her action Ã§aÄŸrÄ±sÄ±nÄ± otomatik loglar - metod bazlÄ± log yazÄ±lmasÄ±na gerek kalmaz
/// </summary>
public class ActionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ActionLoggingMiddleware> _logger;

    public ActionLoggingMiddleware(RequestDelegate next, ILogger<ActionLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICorrelationContext correlationContext,
        ILogService logService)
    {
        var stopwatch = Stopwatch.StartNew();
        var actionName = GetActionName(context);

        // Action baÅŸlangÄ±cÄ±nÄ± logla
        _logger.LogInformation(
            "[{CorrelationId}] Action START: {ActionName} | Method: {Method} | Path: {Path}",
            correlationContext.CorrelationId,
            actionName,
            context.Request.Method,
            context.Request.Path);

        Exception? exception = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Action bitiÅŸini logla
            var statusCode = context.Response.StatusCode;
            var logLevel = statusCode >= 500 ? LogLevel.Error :
                          statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            _logger.Log(logLevel,
                "[{CorrelationId}] Action END: {ActionName} | Status: {StatusCode} | Duration: {Duration}ms",
                correlationContext.CorrelationId,
                actionName,
                statusCode,
                stopwatch.ElapsedMilliseconds);

            // Performance log (async)
            _ = Task.Run(async () =>
            {
                await logService.LogPerformanceAsync(new PerformanceLogEntry
                {
                    OperationName = actionName,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    OperationType = "HttpAction",
                    Success = exception == null && statusCode < 400,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Method"] = context.Request.Method,
                        ["Path"] = context.Request.Path.Value ?? "",
                        ["StatusCode"] = statusCode,
                        ["QueryString"] = context.Request.QueryString.Value ?? ""
                    }
                });
            });

            // Exception log
            if (exception != null)
            {
                await logService.LogExceptionAsync(new ExceptionLogEntry
                {
                    ExceptionType = exception.GetType().FullName ?? "Unknown",
                    ExceptionMessage = exception.Message,
                    StackTrace = exception.StackTrace,
                    MethodName = actionName,
                    LayerName = "API",
                    Severity = "Error"
                });
            }
        }
    }

    private static string GetActionName(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            var actionDescriptor = endpoint.Metadata
                .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                .FirstOrDefault();

            if (actionDescriptor != null)
            {
                return $"{actionDescriptor.ControllerName}.{actionDescriptor.ActionName}";
            }
        }

        return $"{context.Request.Method} {context.Request.Path}";
    }
}

/// <summary>
/// Exception logging middleware
/// Global exception handling with automatic logging
/// </summary>
public class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionLoggingMiddleware> _logger;

    public ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICorrelationContext correlationContext,
        ILogService logService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{CorrelationId}] Unhandled exception: {ExceptionType} - {Message}",
                correlationContext.CorrelationId,
                ex.GetType().Name,
                ex.Message);

            // Exception'Ä± async logla
            await logService.LogExceptionAsync(new ExceptionLogEntry
            {
                ExceptionType = ex.GetType().FullName ?? "Unknown",
                ExceptionMessage = ex.Message,
                StackTrace = ex.StackTrace,
                InnerExceptionMessage = ex.InnerException?.Message,
                InnerExceptionType = ex.InnerException?.GetType().FullName,
                MethodName = context.Request.Path,
                LayerName = "API",
                Severity = "Critical",
                AdditionalData = new Dictionary<string, object>
                {
                    ["RequestPath"] = context.Request.Path.Value ?? "",
                    ["RequestMethod"] = context.Request.Method,
                    ["QueryString"] = context.Request.QueryString.Value ?? ""
                }
            });

            throw;
        }
    }
}


