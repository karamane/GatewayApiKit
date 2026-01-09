using System.Diagnostics;
using System.Text;
using ApiGatewayKit.Core.Application.Interfaces.Logging;
using ApiGatewayKit.Core.Application.Models.Logging;
using ApiGatewayKit.Core.Shared.Constants;
using ApiGatewayKit.Infrastructure.Logging.Helpers;
using ApiGatewayKit.Infrastructure.Logging.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IO;

namespace ApiGatewayKit.Infrastructure.Logging.Middleware;

/// <summary>
/// Request/Response logging middleware
/// TÃ¼m HTTP request ve response'larÄ± loglar
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly LoggingOptions _options;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IOptions<LoggingOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICorrelationContext correlationContext,
        ILogService logService,
        ISensitiveDataMasker sensitiveDataMasker)
    {
        var requestLog = await CreateRequestLogAsync(context, correlationContext, sensitiveDataMasker);
        await logService.LogRequestAsync(requestLog);

        var sw = Stopwatch.StartNew();

        // Response body'yi yakalamak iÃ§in stream'i wrap et
        var originalBodyStream = context.Response.Body;
        await using var responseBodyStream = _recyclableMemoryStreamManager.GetStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);

            sw.Stop();

            var responseLog = await CreateResponseLogAsync(
                context,
                correlationContext,
                requestLog.LogId,
                sw.ElapsedMilliseconds,
                responseBodyStream,
                sensitiveDataMasker);

            await logService.LogResponseAsync(responseLog);

            // Original stream'e yaz
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            sw.Stop();

            // Exception log
            var exceptionLog = ExceptionLogEntry.FromException(
                ex,
                LogConstants.Layers.ServerApi,
                correlationContext.CorrelationId);

            exceptionLog.RequestPath = context.Request.Path;
            exceptionLog.HttpMethod = context.Request.Method;

            await logService.LogExceptionAsync(exceptionLog);

            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task<RequestLogEntry> CreateRequestLogAsync(
        HttpContext context,
        ICorrelationContext correlationContext,
        ISensitiveDataMasker sensitiveDataMasker)
    {
        var request = context.Request;

        // Request body oku
        request.EnableBuffering();
        string? requestBody = null;

        if (request.ContentLength > 0 && request.ContentLength < _options.RequestBodyMaxLength)
        {
            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            requestBody = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            // Sensitive data maskele
            requestBody = sensitiveDataMasker.MaskJson(requestBody);
        }

        return new RequestLogEntry
        {
            CorrelationId = correlationContext.CorrelationId,
            Layer = LogConstants.Layers.ServerApi,
            HttpMethod = request.Method,
            RequestPath = request.Path,
            QueryString = request.QueryString.ToString(),
            RequestBody = requestBody?.Truncate(_options.RequestBodyMaxLength),
            RequestHeaders = GetSafeHeaders(request.Headers),
            ContentType = request.ContentType,
            ContentLength = request.ContentLength,
            ClientIp = correlationContext.ClientIp,
            UserAgent = correlationContext.UserAgent,
            UserId = correlationContext.UserId,
            IsAuthenticated = context.User?.Identity?.IsAuthenticated ?? false,
            UserRoles = context.User?.Claims
                .Where(c => c.Type == "role")
                .Select(c => c.Value)
                .ToArray(),
            ApplicationName = _options.ApplicationName,
            ServerName = correlationContext.ServerName
        };
    }

    private async Task<ResponseLogEntry> CreateResponseLogAsync(
        HttpContext context,
        ICorrelationContext correlationContext,
        string requestLogId,
        long durationMs,
        Stream responseBodyStream,
        ISensitiveDataMasker sensitiveDataMasker)
    {
        string? responseBody = null;

        if (responseBodyStream.Length > 0 && responseBodyStream.Length < _options.ResponseBodyMaxLength)
        {
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(responseBodyStream, leaveOpen: true);
            responseBody = await reader.ReadToEndAsync();

            // Sensitive data maskele
            responseBody = sensitiveDataMasker.MaskJson(responseBody);
        }

        return new ResponseLogEntry
        {
            CorrelationId = correlationContext.CorrelationId,
            Layer = LogConstants.Layers.ServerApi,
            RequestLogId = requestLogId,
            StatusCode = context.Response.StatusCode,
            ResponseBody = responseBody?.Truncate(_options.ResponseBodyMaxLength),
            ContentType = context.Response.ContentType,
            ContentLength = context.Response.ContentLength ?? responseBodyStream.Length,
            DurationMs = durationMs,
            ResponseHeaders = GetSafeHeaders(context.Response.Headers),
            ApplicationName = _options.ApplicationName,
            ServerName = correlationContext.ServerName
        };
    }

    /// <summary>
    /// GÃ¼venli header'larÄ± alÄ±r (Authorization gibi sensitive header'larÄ± maskeler)
    /// </summary>
    private static Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
    {
        var sensitiveHeaders = new[] { "authorization", "cookie", "x-api-key" };

        return headers
            .Where(h => !sensitiveHeaders.Contains(h.Key.ToLower()))
            .ToDictionary(
                h => h.Key,
                h => h.Value.ToString());
    }
}

/// <summary>
/// String extension for truncation
/// </summary>
internal static class StringExtensions
{
    public static string? Truncate(this string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.Length <= maxLength ? value : value[..maxLength] + "...[truncated]";
    }
}


