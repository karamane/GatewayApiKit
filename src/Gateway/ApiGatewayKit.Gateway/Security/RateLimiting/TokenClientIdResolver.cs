using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace ApiGatewayKit.Gateway.Security.RateLimiting;

public class TokenClientIdResolver : IClientResolveContributor
{
    private readonly ILogger<TokenClientIdResolver> _logger;
    private readonly string _claintIdHeader = "This-Is-Not-Used-But-Required";

    public TokenClientIdResolver(ILogger<TokenClientIdResolver> logger)
    {
        _logger = logger;
    }

    public Task<string> ResolveClientAsync(HttpContext httpContext)
    {
        // 1. Check for Authorization Header
        var authHeader = httpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            // Fallback to IP or Anonymous if no token provided
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogDebug("No Bearer token found. Falling back to IP-based rate limiting: {Ip}", ip);
            return Task.FromResult($"anon_{ip}");
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        string keyToHash = string.Empty;

        // 2. Try to get Claim from HttpContext.User (if Authentication Middleware is active and successful)
        // Optimization: Use parsed principal if available to avoid re-parsing
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            // Priority: sub -> uid -> propert_id (gsm) -> name
            var claim = httpContext.User.FindFirst("sub") 
                     ?? httpContext.User.FindFirst("uid")
                     ?? httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub);

            if (claim != null)
            {
                keyToHash = claim.Value;
            }
        }

        // 3. If HttpContext.User is not populated (Auth middleware missing or failed, but token exists),
        // we parse manually to extract the "sub" or just use the token signature as key?
        // User requested: "JWT içinden (örn: usr, gsm, sub, jti claim’leri) token bazlı bir rate limit key üret"
        // So we must try to parse it.
        if (string.IsNullOrEmpty(keyToHash))
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwtToken = handler.ReadJwtToken(token);
                    // Configurable priority could be injected, strictly using sub/jti here
                    var sub = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                    var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;
                    
                    keyToHash = sub ?? jti ?? "unknown_jwt_claim";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse JWT for Rate Limiting. Using token hash as fallback.");
                // Fallback: Use the token itself (or signature) as the key source
                // This ensures "Same token = Same limit" even if we can't read claims
                keyToHash = token; 
            }
        }

        if (string.IsNullOrEmpty(keyToHash))
        {
             keyToHash = token; // Ultimate fallback
        }

        // 4. SHA256 Hash
        var hash = ComputeSha256Hash(keyToHash);
        
        // Log debug (careful not to log full PII in prod, hash is safe)
        // _logger.LogDebug("RateLimit ClientID Resolved: {ClientId}", hash);

        return Task.FromResult(hash);
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
