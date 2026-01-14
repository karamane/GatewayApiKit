using Serilog;
using Serilog.Context;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace ApiGatewayKit.Gateway.Middleware;

public class GatewayLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayLoggingMiddleware> _logger;

    public GatewayLoggingMiddleware(RequestDelegate next, ILogger<GatewayLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // 1. Correlation ID
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() 
                            ?? context.TraceIdentifier 
                            ?? Guid.NewGuid().ToString();
        
        context.Request.Headers["X-Correlation-Id"] = correlationId; // Propagate downstream via Ocelot automatically if configured, or manually

        // 2. Extract Basic Info
        var request = context.Request;
        var method = request.Method;
        var path = request.Path;
        var forwardedFor = request.Headers["X-Forwarded-For"].FirstOrDefault();

        // 3. Push Properties to Serilog Context
        // Using LogContext to ensure these properties are attached to ALL logs generated within this scope
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"))
        using (LogContext.PushProperty("Service", "ApiGateway"))
        using (LogContext.PushProperty("InboundMethod", method))
        using (LogContext.PushProperty("InboundPath", path))
        using (LogContext.PushProperty("ClientIp", forwardedFor ?? context.Connection.RemoteIpAddress?.ToString())) // Caution: F5 IP vs Real IP
        {
            try
            {
                // 4. Token Analysis (Safe Logging) & Auth Type
                var authHeader = request.Headers["Authorization"].ToString();
                var authType = "anonymous";
                string tokenHash = null;

                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    authType = "jwt";
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    tokenHash = ComputeSha256Hash(token); // NEVER Log the actual token
                }

                // Push Security Context
                LogContext.PushProperty("AuthType", authType);
                if (tokenHash != null)
                {
                    LogContext.PushProperty("TokenHash", tokenHash);
                }

                // 5. Proceed Pipeline
                await _next(context);

                // 6. Response Analysis
                var statusCode = context.Response.StatusCode;
                stopwatch.Stop();

                // Check Throttling/Failures
                bool isThrottled = statusCode == 429;
                bool isServerError = statusCode >= 500;
                
                // Determine Log Level based on Status
                var level = isServerError ? Serilog.Events.LogEventLevel.Error :
                            isThrottled ? Serilog.Events.LogEventLevel.Warning :
                            Serilog.Events.LogEventLevel.Information;

                // Log the final consolidated request event
                // This mimics standard RequestLogging but with our custom structure
                // We use a specific message template to make it easy to grep
                Log.Write(level, 
                    "Gateway Request [{Method}] {Path} responded {StatusCode} in {Elapsed:0.0000} ms. Throttled: {IsThrottled}", 
                    method, path, statusCode, stopwatch.Elapsed.TotalMilliseconds, isThrottled);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Log.Error(ex, "Unhandled Exception in Gateway Pipeline: {Message}", ex.Message);
                throw; // Re-throw to let global handler catch or crash
            }
        }
    }

    private static string ComputeSha256Hash(string rawData)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
