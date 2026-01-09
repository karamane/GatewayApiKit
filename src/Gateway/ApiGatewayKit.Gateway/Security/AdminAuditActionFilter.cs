using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace ApiGatewayKit.Gateway.Security;

/// <summary>
/// Admin iÅŸlemlerini dosya bazlÄ± audit log'a yazan action filter.
/// Her istek iÃ§in: timestamp, user, IP, path, method, request body, response status, duration
/// </summary>
public class AdminAuditActionFilter : IAsyncActionFilter
{
    private readonly ILogger<AdminAuditActionFilter> _logger;
    private readonly AdminSecurityOptions _options;
    private readonly string _basePath;

    private static readonly SemaphoreSlim WriteLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminAuditActionFilter(
        ILogger<AdminAuditActionFilter> logger,
        IOptions<AdminSecurityOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        // Log path'ini belirle
        _basePath = _options.Audit.FilePath;
        if (string.IsNullOrEmpty(_basePath))
        {
            _basePath = "logs/admin-audit/audit-{Date}.log";
        }
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Audit devre dÄ±ÅŸÄ± ise geÃ§
        if (!_options.Audit.Enabled)
        {
            await next();
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var httpContext = context.HttpContext;

        // Request bilgilerini topla
        var auditEntry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            CorrelationId = GetCorrelationId(httpContext),
            ClientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            ApiKeyName = httpContext.Items.TryGetValue("AdminKeyName", out var keyName)
                ? keyName?.ToString() ?? "anonymous"
                : "anonymous",
            Method = httpContext.Request.Method,
            Path = httpContext.Request.Path.ToString(),
            QueryString = httpContext.Request.QueryString.ToString(),
            ActionName = context.ActionDescriptor.DisplayName ?? "unknown"
        };

        // Request body (opsiyonel)
        if (_options.Audit.LogRequestBody && context.ActionArguments.Count > 0)
        {
            try
            {
                auditEntry.RequestBody = JsonSerializer.Serialize(context.ActionArguments, JsonOptions);

                // Sensitive data maskeleme
                auditEntry.RequestBody = MaskSensitiveData(auditEntry.RequestBody);
            }
            catch
            {
                auditEntry.RequestBody = "[serialization error]";
            }
        }

        // Action'Ä± Ã§alÄ±ÅŸtÄ±r
        ActionExecutedContext resultContext;
        try
        {
            resultContext = await next();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            auditEntry.DurationMs = stopwatch.ElapsedMilliseconds;
            auditEntry.StatusCode = 500;
            auditEntry.ErrorMessage = ex.Message;
            auditEntry.Success = false;

            await WriteAuditLogAsync(auditEntry);
            throw;
        }

        stopwatch.Stop();
        auditEntry.DurationMs = stopwatch.ElapsedMilliseconds;

        // Response bilgilerini ekle
        if (resultContext.Exception != null)
        {
            auditEntry.StatusCode = 500;
            auditEntry.ErrorMessage = resultContext.Exception.Message;
            auditEntry.Success = false;
        }
        else
        {
            auditEntry.StatusCode = httpContext.Response.StatusCode;
            auditEntry.Success = auditEntry.StatusCode is >= 200 and < 300;
        }

        await WriteAuditLogAsync(auditEntry);
    }

    /// <summary>
    /// Audit log'u dosyaya yazar
    /// </summary>
    private async Task WriteAuditLogAsync(AuditLogEntry entry)
    {
        try
        {
            var logPath = GetLogFilePath();

            // Dizin yoksa oluÅŸtur
            var directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // JSON formatÄ±nda log satÄ±rÄ±
            var logLine = JsonSerializer.Serialize(entry, JsonOptions);

            await WriteLock.WaitAsync();
            try
            {
                await File.AppendAllTextAsync(logPath, logLine + Environment.NewLine, Encoding.UTF8);
            }
            finally
            {
                WriteLock.Release();
            }

            _logger.LogDebug(
                "Admin audit logged: {Method} {Path} by {User} - {Status} ({Duration}ms)",
                entry.Method,
                entry.Path,
                entry.ApiKeyName,
                entry.StatusCode,
                entry.DurationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write admin audit log");
        }
    }

    /// <summary>
    /// Log dosyasÄ± yolunu belirler ({Date} placeholder'Ä± deÄŸiÅŸtirilir)
    /// </summary>
    private string GetLogFilePath()
    {
        var path = _basePath;

        if (path.Contains("{Date}"))
        {
            path = path.Replace("{Date}", DateTime.UtcNow.ToString("yyyyMMdd"));
        }

        // Relative path ise working directory'e gÃ¶re Ã§Ã¶zÃ¼mle
        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), path);
        }

        return path;
    }

    /// <summary>
    /// Correlation ID'yi alÄ±r veya oluÅŸturur
    /// </summary>
    private static string GetCorrelationId(HttpContext context)
    {
        // Ã–nce mevcut header'dan dene
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            return correlationId.FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        }

        // Context'ten dene
        if (context.Items.TryGetValue("CorrelationId", out var ctxCorrelationId))
        {
            return ctxCorrelationId?.ToString() ?? Guid.NewGuid().ToString("N");
        }

        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Hassas verileri maskeler
    /// </summary>
    private static string MaskSensitiveData(string json)
    {
        // Basit maskeleme: password, secret, key iÃ§eren field'larÄ± maskele
        var sensitivePatterns = new[]
        {
            "\"password\"", "\"Password\"",
            "\"secret\"", "\"Secret\"",
            "\"apiKey\"", "\"ApiKey\"", "\"api_key\"",
            "\"token\"", "\"Token\"",
            "\"connectionString\"", "\"ConnectionString\""
        };

        foreach (var pattern in sensitivePatterns)
        {
            if (json.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                // DeÄŸeri maskele (basit regex yerine string iÅŸleme)
                var index = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                while (index >= 0)
                {
                    var valueStart = json.IndexOf(':', index) + 1;
                    if (valueStart > 0)
                    {
                        // String value baÅŸlangÄ±cÄ±nÄ± bul
                        var quoteStart = json.IndexOf('"', valueStart);
                        if (quoteStart > 0 && quoteStart < valueStart + 10)
                        {
                            var quoteEnd = json.IndexOf('"', quoteStart + 1);
                            if (quoteEnd > quoteStart)
                            {
                                json = string.Concat(json.AsSpan(0, quoteStart + 1), "***MASKED***", json.AsSpan(quoteEnd));
                            }
                        }
                    }
                    index = json.IndexOf(pattern, index + 1, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        return json;
    }
}

/// <summary>
/// Audit log entry modeli
/// </summary>
public class AuditLogEntry
{
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
    public string ApiKeyName { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string? RequestBody { get; set; }
    public int StatusCode { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
}


