using ApiGatewayKit.Core.Application.Interfaces.Logging;
using ApiGatewayKit.Core.Shared.Constants;
using ApiGatewayKit.Infrastructure.Logging.Context;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace ApiGatewayKit.Infrastructure.Logging.Middleware;

/// <summary>
/// Correlation ID middleware
/// Her request iÃ§in correlation ID oluÅŸturur veya mevcut olanÄ± kullanÄ±r
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlationContext)
    {
        // Header'dan correlation ID al
        var correlationId = context.Request.Headers[LogConstants.Headers.CorrelationId].FirstOrDefault();
        var parentCorrelationId = context.Request.Headers[LogConstants.Headers.ParentCorrelationId].FirstOrDefault();

        // Correlation context'i gÃ¼ncelle
        if (!string.IsNullOrEmpty(correlationId) && correlationContext is CorrelationContext ctx)
        {
            ctx.CorrelationId = correlationId;
        }

        // Context bilgilerini doldur
        correlationContext.ClientIp = GetClientIp(context);
        correlationContext.UserAgent = context.Request.Headers.UserAgent.FirstOrDefault();
        correlationContext.RequestPath = context.Request.Path;
        correlationContext.UserId = context.User?.Identity?.Name;

        // Response header'a ekle
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[LogConstants.Headers.CorrelationId] = correlationContext.CorrelationId;
            context.Response.Headers[LogConstants.Headers.ServerName] = correlationContext.ServerName;
            return Task.CompletedTask;
        });

        // Serilog LogContext'e ekle
        using (LogContext.PushProperty("CorrelationId", correlationContext.CorrelationId))
        using (LogContext.PushProperty("ParentCorrelationId", parentCorrelationId))
        using (LogContext.PushProperty("ClientIp", correlationContext.ClientIp))
        using (LogContext.PushProperty("UserId", correlationContext.UserId))
        using (LogContext.PushProperty("ServerName", correlationContext.ServerName))
        using (LogContext.PushProperty("RequestPath", correlationContext.RequestPath))
        {
            await _next(context);
        }
    }

    /// <summary>
    /// Client IP adresini alÄ±r (Load balancer arkasÄ±nda da Ã§alÄ±ÅŸÄ±r)
    /// </summary>
    private static string? GetClientIp(HttpContext context)
    {
        // X-Forwarded-For header'Ä± kontrol et
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // X-Real-IP header'Ä± kontrol et
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Direkt baÄŸlantÄ± IP'si
        return context.Connection.RemoteIpAddress?.ToString();
    }
}


