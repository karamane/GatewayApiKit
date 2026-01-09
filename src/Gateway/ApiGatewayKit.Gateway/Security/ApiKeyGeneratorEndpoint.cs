using Microsoft.AspNetCore.Mvc;

namespace ApiGatewayKit.Gateway.Security;

/// <summary>
/// API Key Ã¼retmek iÃ§in yardÄ±mcÄ± endpoint (Sadece Development ortamÄ±nda aktif)
/// Production'da bu endpoint eriÅŸilemez.
/// </summary>
[ApiController]
[Route("api/gateway/admin/tools")]
public class ApiKeyGeneratorController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ApiKeyGeneratorController> _logger;

    public ApiKeyGeneratorController(
        IWebHostEnvironment environment,
        ILogger<ApiKeyGeneratorController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Verilen plain text API key iÃ§in SHA256 hash Ã¼retir.
    /// Bu hash'i appsettings.json'a ekleyebilirsiniz.
    /// 
    /// Ã–rnek kullanÄ±m:
    /// GET /api/gateway/admin/tools/generate-key-hash?key=my-secret-api-key
    /// 
    /// Response:
    /// {
    ///   "plainTextKey": "my-secret-api-key",
    ///   "hash": "sha256:abc123...",
    ///   "configExample": { ... }
    /// }
    /// </summary>
    [HttpGet("generate-key-hash")]
    public IActionResult GenerateKeyHash([FromQuery] string? key = null)
    {
        // Sadece Development ortamÄ±nda Ã§alÄ±ÅŸÄ±r
        if (!_environment.IsDevelopment())
        {
            return NotFound(new { error = "This endpoint is only available in Development environment" });
        }

        // Key verilmezse rastgele bir key Ã¼ret
        var plainTextKey = key;
        if (string.IsNullOrWhiteSpace(plainTextKey))
        {
            plainTextKey = GenerateRandomKey();
        }

        var hash = ApiKeyAuthMiddleware.GenerateKeyHash(plainTextKey);

        _logger.LogInformation("API Key hash generated for development purposes");

        return Ok(new
        {
            PlainTextKey = plainTextKey,
            Hash = hash,
            ConfigExample = new
            {
                Name = "Your-Team-Name",
                KeyHash = hash,
                Permissions = new[] { "read", "write" },
                IsActive = true
            },
            Usage = new
            {
                Header = "X-Admin-Api-Key: " + plainTextKey,
                QueryString = "?api_key=" + plainTextKey
            },
            Warning = "Bu endpoint sadece Development ortamÄ±nda aktiftir. Key'i gÃ¼venli bir yerde saklayÄ±n!"
        });
    }

    /// <summary>
    /// Rastgele gÃ¼venli bir API key Ã¼retir
    /// </summary>
    private static string GenerateRandomKey()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var key = new char[32];

        for (var i = 0; i < key.Length; i++)
        {
            key[i] = chars[random.Next(chars.Length)];
        }

        return new string(key);
    }

    /// <summary>
    /// Mevcut gÃ¼venlik konfigÃ¼rasyonunu gÃ¶sterir (hassas veriler gizlenir)
    /// </summary>
    [HttpGet("security-status")]
    public IActionResult GetSecurityStatus(
        [FromServices] Microsoft.Extensions.Options.IOptions<AdminSecurityOptions> options)
    {
        // Sadece Development ortamÄ±nda Ã§alÄ±ÅŸÄ±r
        if (!_environment.IsDevelopment())
        {
            return NotFound(new { error = "This endpoint is only available in Development environment" });
        }

        var config = options.Value;

        return Ok(new
        {
            SecurityEnabled = config.Enabled,
            IpWhitelistCount = config.IpWhitelist.Count,
            IpWhitelist = config.IpWhitelist,
            ApiKeyCount = config.ApiKeys.Count,
            ApiKeys = config.ApiKeys.Select(k => new
            {
                k.Name,
                k.Permissions,
                k.IsActive,
                k.ExpiresAt,
                KeyHashPreview = k.KeyHash.Length > 20 ? k.KeyHash[..20] + "..." : k.KeyHash
            }),
            RateLimit = new
            {
                config.RateLimit.Enabled,
                config.RateLimit.RequestsPerMinute,
                config.RateLimit.BlockDurationMinutes
            },
            Audit = new
            {
                config.Audit.Enabled,
                config.Audit.FilePath,
                config.Audit.LogRequestBody,
                config.Audit.LogResponseBody
            },
            Production = config.Production
        });
    }
}


