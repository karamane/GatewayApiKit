using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace ApiGatewayKit.Gateway.Security;

/// <summary>
/// Admin endpoint'leri iÃ§in in-memory rate limiting middleware.
/// Sliding window algoritmasÄ± kullanÄ±r, DB baÄŸÄ±mlÄ±lÄ±ÄŸÄ± yoktur.
/// </summary>
public class AdminRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AdminRateLimitMiddleware> _logger;
    private readonly AdminSecurityOptions _options;
    private readonly IWebHostEnvironment _environment;

    // ClientId -> (RequestTimestamps, BlockedUntil)
    private static readonly ConcurrentDictionary<string, RateLimitEntry> RateLimitStore = new();

    // Cleanup iÃ§in son Ã§alÄ±ÅŸma zamanÄ±
    private static DateTime _lastCleanup = DateTime.UtcNow;
    private static readonly object CleanupLock = new();

    public AdminRateLimitMiddleware(
        RequestDelegate next,
        ILogger<AdminRateLimitMiddleware> logger,
        IOptions<AdminSecurityOptions> options,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Sadece admin endpoint'lerini kontrol et
        if (!context.Request.Path.StartsWithSegments("/api/gateway/admin"))
        {
            await _next(context);
            return;
        }

        // Tools endpoint'i sadece Development'ta rate limit kontrolÃ¼nden muaf
        if (_environment.IsDevelopment() && context.Request.Path.StartsWithSegments("/api/gateway/admin/tools"))
        {
            await _next(context);
            return;
        }

        // Rate limiting devre dÄ±ÅŸÄ± ise geÃ§
        if (!_options.Enabled || !_options.RateLimit.Enabled)
        {
            await _next(context);
            return;
        }

        // Periyodik cleanup (5 dakikada bir)
        CleanupOldEntries();

        // Client identifier: API Key name veya IP adresi
        var clientId = GetClientIdentifier(context);

        // Mevcut entry'yi al veya oluÅŸtur
        var entry = RateLimitStore.GetOrAdd(clientId, _ => new RateLimitEntry());

        lock (entry.Lock)
        {
            var now = DateTime.UtcNow;

            // Bloklu mu kontrol et
            if (entry.BlockedUntil.HasValue && entry.BlockedUntil.Value > now)
            {
                var remainingSeconds = (entry.BlockedUntil.Value - now).TotalSeconds;

                _logger.LogWarning(
                    "Admin rate limit: Client {ClientId} is blocked for {Remaining:F0} more seconds. Path: {Path}",
                    clientId,
                    remainingSeconds,
                    context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = ((int)Math.Ceiling(remainingSeconds)).ToString();
                context.Response.WriteAsJsonAsync(new
                {
                    error = "Too Many Requests",
                    message = $"Rate limit exceeded. Try again in {(int)Math.Ceiling(remainingSeconds)} seconds.",
                    retryAfter = (int)Math.Ceiling(remainingSeconds)
                }).Wait();
                return;
            }

            // Blok sÃ¼resi geÃ§tiyse temizle
            if (entry.BlockedUntil.HasValue)
            {
                entry.BlockedUntil = null;
                entry.RequestTimestamps.Clear();
            }

            // Sliding window: Son 1 dakikadaki istekleri say
            var windowStart = now.AddMinutes(-1);
            var recentRequests = entry.RequestTimestamps.Count(t => t > windowStart);

            // Limit aÅŸÄ±ldÄ± mÄ±?
            if (recentRequests >= _options.RateLimit.RequestsPerMinute)
            {
                entry.BlockedUntil = now.AddMinutes(_options.RateLimit.BlockDurationMinutes);

                _logger.LogWarning(
                    "Admin rate limit: Client {ClientId} exceeded limit ({Limit}/min). Blocked until {BlockedUntil}",
                    clientId,
                    _options.RateLimit.RequestsPerMinute,
                    entry.BlockedUntil);

                var blockDurationSeconds = _options.RateLimit.BlockDurationMinutes * 60;
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = blockDurationSeconds.ToString();
                context.Response.WriteAsJsonAsync(new
                {
                    error = "Too Many Requests",
                    message = $"Rate limit exceeded. Try again in {blockDurationSeconds} seconds.",
                    retryAfter = blockDurationSeconds
                }).Wait();
                return;
            }

            // Ä°stek kaydÄ± ekle
            entry.RequestTimestamps.Add(now);

            // Eski timestamp'leri temizle (1 dakikadan eski)
            entry.RequestTimestamps.RemoveAll(t => t <= windowStart);
        }

        // Kalan limit bilgisini header'a ekle
        var remainingLimit = _options.RateLimit.RequestsPerMinute - entry.RequestTimestamps.Count;
        context.Response.Headers["X-RateLimit-Limit"] = _options.RateLimit.RequestsPerMinute.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, remainingLimit).ToString();
        context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString();

        await _next(context);
    }

    /// <summary>
    /// Client identifier'Ä± belirler (API Key name veya IP)
    /// </summary>
    private static string GetClientIdentifier(HttpContext context)
    {
        // API Key authenticated ise key name kullan
        if (context.Items.TryGetValue("AdminKeyName", out var keyName) && keyName is string name)
        {
            return $"key:{name}";
        }

        // DeÄŸilse IP adresi kullan
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ip}";
    }

    /// <summary>
    /// Eski entry'leri periyodik olarak temizler
    /// </summary>
    private void CleanupOldEntries()
    {
        var now = DateTime.UtcNow;

        // 5 dakikada bir Ã§alÄ±ÅŸ
        if ((now - _lastCleanup).TotalMinutes < 5)
            return;

        lock (CleanupLock)
        {
            if ((now - _lastCleanup).TotalMinutes < 5)
                return;

            _lastCleanup = now;

            var expiredKeys = new List<string>();
            var cutoff = now.AddMinutes(-10); // 10 dakikadan eski entry'leri sil

            foreach (var kvp in RateLimitStore)
            {
                var entry = kvp.Value;

                // Bloklu deÄŸil ve son istek 10 dakikadan eskiyse sil
                if (!entry.BlockedUntil.HasValue &&
                    entry.RequestTimestamps.All(t => t < cutoff))
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                RateLimitStore.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogDebug("Rate limit cleanup: Removed {Count} expired entries", expiredKeys.Count);
            }
        }
    }

    /// <summary>
    /// Rate limit entry (per client)
    /// </summary>
    private class RateLimitEntry
    {
        public List<DateTime> RequestTimestamps { get; } = new();
        public DateTime? BlockedUntil { get; set; }
        public object Lock { get; } = new();
    }
}

/// <summary>
/// Extension methods for AdminRateLimitMiddleware
/// </summary>
public static class AdminRateLimitMiddlewareExtensions
{
    public static IApplicationBuilder UseAdminRateLimit(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AdminRateLimitMiddleware>();
    }
}


