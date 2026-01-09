using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace ApiGatewayKit.Gateway.Security;

/// <summary>
/// Admin endpoint'leri iÃ§in API Key authentication middleware.
/// Key'ler SHA256 hash olarak saklanÄ±r, plain text ASLA loglanmaz.
/// </summary>
public class ApiKeyAuthMiddleware
{
    private const string ApiKeyHeaderName = "X-Admin-Api-Key";
    private const string ApiKeyQueryName = "api_key";

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;
    private readonly AdminSecurityOptions _options;
    private readonly IWebHostEnvironment _environment;

    public ApiKeyAuthMiddleware(
        RequestDelegate next,
        ILogger<ApiKeyAuthMiddleware> logger,
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

        // Tools endpoint'i sadece Development'ta gÃ¼venlik kontrolÃ¼nden muaf
        if (_environment.IsDevelopment() && context.Request.Path.StartsWithSegments("/api/gateway/admin/tools"))
        {
            await _next(context);
            return;
        }

        // GÃ¼venlik devre dÄ±ÅŸÄ± ise geÃ§
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // API Key yoksa sadece read izinli endpoint'lere eriÅŸime izin ver
        // (IP whitelist'ten geÃ§tiyse)
        if (_options.ApiKeys.Count == 0)
        {
            _logger.LogDebug("No API keys configured, allowing request without key authentication");
            await _next(context);
            return;
        }

        // API Key'i header veya query'den al
        var apiKey = GetApiKey(context);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning(
                "Admin access denied: Missing API key. Path: {Path}, IP: {Ip}",
                context.Request.Path,
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers["WWW-Authenticate"] = $"ApiKey realm=\"Gateway Admin\"";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "API key is required. Provide it via X-Admin-Api-Key header or api_key query parameter."
            });
            return;
        }

        // Key'i hash'le ve doÄŸrula
        var validKey = ValidateApiKey(apiKey, out var matchedKey);

        if (!validKey || matchedKey == null)
        {
            _logger.LogWarning(
                "Admin access denied: Invalid API key. Path: {Path}, IP: {Ip}",
                context.Request.Path,
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "Invalid API key"
            });
            return;
        }

        // Key sÃ¼re kontrolÃ¼
        if (matchedKey.ExpiresAt.HasValue && matchedKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning(
                "Admin access denied: Expired API key '{KeyName}'. Path: {Path}",
                matchedKey.Name,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "API key has expired"
            });
            return;
        }

        // Key aktif mi
        if (!matchedKey.IsActive)
        {
            _logger.LogWarning(
                "Admin access denied: Inactive API key '{KeyName}'. Path: {Path}",
                matchedKey.Name,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "API key is inactive"
            });
            return;
        }

        // Key bilgilerini context'e ekle (permission kontrolÃ¼ iÃ§in)
        context.Items["AdminApiKey"] = matchedKey;
        context.Items["AdminKeyName"] = matchedKey.Name;
        context.Items["AdminPermissions"] = matchedKey.Permissions;

        _logger.LogDebug(
            "Admin API key validated: {KeyName} with permissions [{Permissions}]",
            matchedKey.Name,
            string.Join(", ", matchedKey.Permissions));

        await _next(context);
    }

    /// <summary>
    /// Header veya query string'den API key'i alÄ±r
    /// </summary>
    private static string? GetApiKey(HttpContext context)
    {
        // Ã–nce header'dan dene
        if (context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var headerValue))
        {
            return headerValue.FirstOrDefault();
        }

        // Sonra query string'den dene
        if (context.Request.Query.TryGetValue(ApiKeyQueryName, out var queryValue))
        {
            return queryValue.FirstOrDefault();
        }

        return null;
    }

    /// <summary>
    /// API key'i SHA256 hash ile doÄŸrular
    /// </summary>
    private bool ValidateApiKey(string apiKey, out AdminApiKey? matchedKey)
    {
        matchedKey = null;

        // Gelen key'in hash'ini hesapla
        var keyHash = ComputeHash(apiKey);
        var keyHashWithPrefix = $"sha256:{keyHash}";

        foreach (var configuredKey in _options.ApiKeys)
        {
            // Hash karÅŸÄ±laÅŸtÄ±rmasÄ± (timing-safe)
            var storedHash = configuredKey.KeyHash;

            // "sha256:" prefix'i varsa kaldÄ±r karÅŸÄ±laÅŸtÄ±rma iÃ§in
            if (storedHash.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            {
                storedHash = storedHash[7..];
            }

            if (CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(keyHash.ToLowerInvariant()),
                Encoding.UTF8.GetBytes(storedHash.ToLowerInvariant())))
            {
                matchedKey = configuredKey;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// String'in SHA256 hash'ini hesaplar
    /// </summary>
    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// YardÄ±mcÄ± metod: Yeni bir API key iÃ§in hash Ã¼retir
    /// </summary>
    public static string GenerateKeyHash(string plainTextKey)
    {
        return $"sha256:{ComputeHash(plainTextKey)}";
    }
}

/// <summary>
/// Extension methods for ApiKeyAuthMiddleware
/// </summary>
public static class ApiKeyAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseAdminApiKeyAuth(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiKeyAuthMiddleware>();
    }

    /// <summary>
    /// Context'ten authenticated API key'i alÄ±r
    /// </summary>
    public static AdminApiKey? GetAdminApiKey(this HttpContext context)
    {
        return context.Items.TryGetValue("AdminApiKey", out var key) ? key as AdminApiKey : null;
    }

    /// <summary>
    /// Context'ten API key permissions listesini alÄ±r
    /// </summary>
    public static List<string> GetAdminPermissions(this HttpContext context)
    {
        if (context.Items.TryGetValue("AdminPermissions", out var perms) && perms is List<string> list)
            return list;

        return new List<string>();
    }
}


