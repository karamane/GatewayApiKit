using Microsoft.FeatureManagement.FeatureFilters;

namespace ApiGatewayKit.Gateway.FeatureManagement;

/// <summary>
/// HttpContext'ten targeting context bilgisini cikarir
/// User-based ve group-based feature flag'ler icin kullanilir
/// </summary>
public class HttpContextTargetingContextAccessor : ITargetingContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<HttpContextTargetingContextAccessor> _logger;

    private const string UserIdHeader = "X-User-Id";
    private const string UserGroupsHeader = "X-User-Groups";

    public HttpContextTargetingContextAccessor(
        IHttpContextAccessor httpContextAccessor,
        ILogger<HttpContextTargetingContextAccessor> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public ValueTask<TargetingContext> GetContextAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return new ValueTask<TargetingContext>(new TargetingContext
            {
                UserId = Guid.NewGuid().ToString(),
                Groups = Enumerable.Empty<string>()
            });
        }

        var userId = GetUserId(httpContext);
        var groups = GetUserGroups(httpContext);

        _logger.LogDebug(
            "Targeting context created: UserId={UserId}, Groups={Groups}",
            userId,
            string.Join(",", groups));

        return new ValueTask<TargetingContext>(new TargetingContext
        {
            UserId = userId,
            Groups = groups
        });
    }

    private static string GetUserId(HttpContext context)
    {
        // 1. X-User-Id header'indan al
        if (context.Request.Headers.TryGetValue(UserIdHeader, out var userIdHeader) &&
            !string.IsNullOrEmpty(userIdHeader.FirstOrDefault()))
        {
            return userIdHeader.First()!;
        }

        // 2. Authentication claim'den al
        var userClaim = context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userClaim))
        {
            return userClaim;
        }

        // 3. JWT token'dan sub claim
        var subClaim = context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(subClaim))
        {
            return subClaim;
        }

        // 4. Client IP'yi kullan (anonim kullanicilar icin tutarli yuzde hesabi icin)
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(clientIp))
        {
            return $"anon-{clientIp}";
        }

        // 5. Rastgele ID (her request farkli olur, yuzde hesabi icin ideal degil)
        return Guid.NewGuid().ToString();
    }

    private static IEnumerable<string> GetUserGroups(HttpContext context)
    {
        var groups = new List<string>();

        // 1. X-User-Groups header'indan al (virgul ile ayrilmis)
        if (context.Request.Headers.TryGetValue(UserGroupsHeader, out var groupsHeader) &&
            !string.IsNullOrEmpty(groupsHeader.FirstOrDefault()))
        {
            var headerGroups = groupsHeader.First()!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            groups.AddRange(headerGroups);
        }

        // 2. Role claim'lerinden al
        var roleClaims = context.User?.FindAll("role")
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrEmpty(v));

        if (roleClaims?.Any() == true)
        {
            groups.AddRange(roleClaims!);
        }

        // 3. Ozel header'lardan grup belirle
        if (context.Request.Headers.ContainsKey("X-Beta-Tester"))
        {
            groups.Add("BetaTesters");
        }

        if (context.Request.Headers.ContainsKey("X-Internal-User"))
        {
            groups.Add("InternalUsers");
        }

        if (context.Request.Headers.ContainsKey("X-Premium-User"))
        {
            groups.Add("PremiumUsers");
        }

        return groups.Distinct();
    }
}




