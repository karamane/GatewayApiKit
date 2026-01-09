using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using ApiGatewayKit.Gateway.Configuration;
using ApiGatewayKit.Gateway.Services;
using ApiGatewayKit.Core.Application.Interfaces.Routing;
using ApiGatewayKit.Core.Application.Models.Routing;
using Ocelot.Middleware;
using Ocelot.Configuration;
using Microsoft.AspNetCore.Http;

namespace ApiGatewayKit.Gateway.Handlers;

/// <summary>
/// Ocelot DelegatingHandler - ocelot.json Metadata ve FeatureManagement Ã¼zerinden yÃ¶nlendirme
/// </summary>
public class FeatureRoutingHandler : DelegatingHandler
{
    private readonly IFeatureManager _featureManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GatewayOptions _options;
    private readonly ILogger<FeatureRoutingHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ITargetNodeSelector _nodeSelector;

    public FeatureRoutingHandler(
        IFeatureManager featureManager,
        IHttpClientFactory httpClientFactory,
        IOptions<GatewayOptions> options,
        ILogger<FeatureRoutingHandler> logger,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ITargetNodeSelector nodeSelector)
    {
        _featureManager = featureManager;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _nodeSelector = nodeSelector;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var originalUri = request.RequestUri;
        if (originalUri == null) return await base.SendAsync(request, cancellationToken);

        var path = originalUri.AbsolutePath;
        var moduleCode = ExtractModuleCode(path);
        if (string.IsNullOrEmpty(moduleCode)) return await base.SendAsync(request, cancellationToken);

        // Ocelot DownstreamRoute bilgilerini HttpContext'ten al
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext bulunamadÄ±, Ocelot'a devam: {Path}", path);
            return await base.SendAsync(request, cancellationToken);
        }

        var downstreamRoute = httpContext.Items.DownstreamRoute();

        if (downstreamRoute == null)
        {
            _logger.LogDebug("DownstreamRoute bulunamadÄ±, Ocelot'a devam: {Path}", path);
            return await base.SendAsync(request, cancellationToken);
        }

        try
        {
            bool useNew = false;
            string decisionSource = "Module_FeatureFlag";

            // 1. Ã–nce Route bazlÄ± override kontrolÃ¼ (ocelot.json -> Routes -> Metadata)
            // Ocelot nesnesindeki Key Ã¼zerinden ocelot.json'daki Metadata'yÄ± bulalÄ±m (En saÄŸlam yÃ¶ntem)
            var routeKey = downstreamRoute.Key;
            int? routePct = null;

            if (!string.IsNullOrEmpty(routeKey))
            {
                var routeMetadata = _configuration.GetSection("Routes")
                    .GetChildren()
                    .FirstOrDefault(r => r["Key"] == routeKey)?
                    .GetSection("Metadata");

                var pctVal = routeMetadata?["NewSystemPercentage"];
                if (!string.IsNullOrEmpty(pctVal) && int.TryParse(pctVal, out var pct))
                {
                    routePct = pct;
                }
            }

            if (routePct.HasValue)
            {
                useNew = ShouldUseNewSystem(routePct.Value);
                decisionSource = $"Route_Override_%{routePct.Value}";
                
                _logger.LogDebug("[ROUTE-MATCH] Key: {Key}, Path: {Path}, Pct: {Pct}", 
                    routeKey, path, routePct.Value);
            }
            else
            {
                // Fallback: Key Ã¼zerinden bulunamazsa path bazlÄ± deneme (Sadece gÃ¼venlik iÃ§in)
                var rawUpstreamPath = downstreamRoute.UpstreamPathTemplate?.OriginalValue ?? "";
                var normalizedUpstreamPath = rawUpstreamPath.Trim('/');
                
                if (!string.IsNullOrEmpty(normalizedUpstreamPath))
                {
                    var routeMetadata = _configuration.GetSection("Routes")
                        .GetChildren()
                        .FirstOrDefault(r => (r["UpstreamPathTemplate"] ?? "").Trim('/') == normalizedUpstreamPath)?
                        .GetSection("Metadata");

                    var pctVal = routeMetadata?["NewSystemPercentage"];
                    if (!string.IsNullOrEmpty(pctVal) && int.TryParse(pctVal, out var pct))
                    {
                        routePct = pct;
                        useNew = ShouldUseNewSystem(routePct.Value);
                        decisionSource = $"Route_Override_Path_%{routePct.Value}";
                    }
                }
            }

            if (!routePct.HasValue)
            {
                // 2. ModÃ¼l bazlÄ± FeatureManager ile karar ver (ocelot.json -> FeatureManagement)
                var featureFlag = $"{moduleCode}_UseNew";
                useNew = await _featureManager.IsEnabledAsync(featureFlag);
                
                _logger.LogDebug("[MODULE-MATCH] Module: {Module}, Flag: {Flag}, UseNew: {UseNew}", 
                    moduleCode, featureFlag, useNew);
            }
            
            _logger.LogInformation("[ROUTING] Module: {Module}, Path: {Path}, Source: {Source} -> Target: {Target}",
                moduleCode, path, decisionSource, useNew ? "NEW" : "LEGACY");

            if (useNew)
            {
                GatewayTargetNode node = await _nodeSelector.SelectNodeAsync(
                    TargetSystem.New,
                    routeKey,
                    downstreamRoute.UpstreamPathTemplate?.OriginalValue,
                    cancellationToken);

                request.RequestUri = BuildTargetUri(node.BaseUrl, request.RequestUri!);

                request.Headers.TryAddWithoutValidation("X-Target-System", "NEW");
                request.Headers.TryAddWithoutValidation("X-Module-Code", moduleCode);
                request.Headers.TryAddWithoutValidation("X-Target-Node", node.Id);
                
                _logger.LogDebug("[NEW-ROUTING] Target: {TargetUrl}", request.RequestUri);
                
                return await base.SendAsync(request, cancellationToken);
            }
            else
            {
                GatewayTargetNode node = await _nodeSelector.SelectNodeAsync(
                    TargetSystem.Legacy,
                    routeKey,
                    downstreamRoute.UpstreamPathTemplate?.OriginalValue,
                    cancellationToken);

                return await ForwardToLegacyAsync(request, originalUri.PathAndQuery, moduleCode, node, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YÃ¶nlendirme kararÄ± alÄ±nÄ±rken hata oluÅŸtu. Legacy'ye yÃ¶nlendiriliyor.");
            return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent(
                    "{\"error\":\"Service Unavailable\",\"message\":\"No available upstream nodes\"}",
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        }
    }

    /// <summary>
    /// YÃ¼zdeye gÃ¶re yeni sisteme gidip gitmeyeceÄŸine karar verir
    /// </summary>
    private static bool ShouldUseNewSystem(int percentage)
    {
        if (percentage >= 100) return true;
        if (percentage <= 0) return false;
        
        // Rastgele karar (yÃ¼zdelik daÄŸÄ±lÄ±m)
        return Random.Shared.Next(100) < percentage;
    }

    /// <summary>
    /// Legacy sisteme direkt yÃ¶nlendirme (Ocelot bypass)
    /// </summary>
    private async Task<HttpResponseMessage> ForwardToLegacyAsync(
        HttpRequestMessage originalRequest,
        string path,
        string moduleCode,
        GatewayTargetNode node,
        CancellationToken cancellationToken)
    {
        var legacyPath = _options.TransformToLegacyPath(path);
        var legacyUrl = new Uri(node.BaseUrl, legacyPath).ToString();

        _logger.LogInformation("[{Module}] -> LEGACY({NodeId}): {Path} -> {LegacyUrl}", moduleCode, node.Id, path, legacyUrl);

        try
        {
            var client = _httpClientFactory.CreateClient("LegacyTarget");

            // Yeni request oluÅŸtur
            var legacyRequest = new HttpRequestMessage(originalRequest.Method, legacyUrl);

            // Headers kopyala
            var hasAuth = false;
            foreach (var header in originalRequest.Headers)
            {
                // Host header'Ä±nÄ± kopyalama
                if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                {
                    legacyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    
                    if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                    {
                        hasAuth = true;
                    }
                }
            }

            // Custom headers ekle
            legacyRequest.Headers.TryAddWithoutValidation("X-Target-System", "LEGACY");
            legacyRequest.Headers.TryAddWithoutValidation("X-Module-Code", moduleCode);
            legacyRequest.Headers.TryAddWithoutValidation("X-Forwarded-By", "ApiGatewayKit-Gateway");
            legacyRequest.Headers.TryAddWithoutValidation("X-Target-Node", node.Id);

            // Body kopyala
            string? bodyPreview = null;
            if (originalRequest.Content != null)
            {
                var content = await originalRequest.Content.ReadAsByteArrayAsync(cancellationToken);
                legacyRequest.Content = new ByteArrayContent(content);

                // Content headers kopyala
                foreach (var header in originalRequest.Content.Headers)
                {
                    legacyRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                
                // Debug: Body preview (ilk 200 karakter)
                if (content.Length > 0)
                {
                    bodyPreview = System.Text.Encoding.UTF8.GetString(content, 0, Math.Min(content.Length, 200));
                }
            }

            _logger.LogDebug("[LEGACY REQUEST] URL: {Url}, Method: {Method}, HasAuth: {HasAuth}, BodyPreview: {Body}",
                legacyUrl, originalRequest.Method, hasAuth, bodyPreview ?? "(empty)");

            // Legacy'ye istek at
            var response = await client.SendAsync(legacyRequest, cancellationToken);

            _logger.LogDebug("[LEGACY RESPONSE] URL: {Url}, Status: {Status}", legacyUrl, response.StatusCode);

            // Response header ekle
            response.Headers.TryAddWithoutValidation("X-Routed-To", "LEGACY");
            response.Headers.TryAddWithoutValidation("X-Module-Code", moduleCode);
            response.Headers.TryAddWithoutValidation("X-Target-Node", node.Id);

            return response;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Legacy sistem timeout: {LegacyUrl} - 30 saniye iÃ§inde yanÄ±t vermedi", legacyUrl);

            return new HttpResponseMessage(System.Net.HttpStatusCode.GatewayTimeout)
            {
                Content = new StringContent(
                    $"{{\"error\":\"Legacy sistem yanÄ±t vermiyor\",\"url\":\"{legacyUrl}\",\"details\":\"30 saniye timeout aÅŸÄ±ldÄ±. Legacy sistem ({_options.LegacyBaseUrl}) Ã§alÄ±ÅŸÄ±yor mu?\"}}", 
                    System.Text.Encoding.UTF8, 
                    "application/json")
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Legacy sistem baÄŸlantÄ± hatasÄ±: {LegacyUrl}", legacyUrl);

            return new HttpResponseMessage(System.Net.HttpStatusCode.BadGateway)
            {
                Content = new StringContent(
                    $"{{\"error\":\"Legacy sistem baÄŸlantÄ± hatasÄ±\",\"url\":\"{legacyUrl}\",\"details\":\"{ex.Message}\"}}", 
                    System.Text.Encoding.UTF8, 
                    "application/json")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Legacy sistem isteÄŸi baÅŸarÄ±sÄ±z: {LegacyUrl}", legacyUrl);

            return new HttpResponseMessage(System.Net.HttpStatusCode.BadGateway)
            {
                Content = new StringContent(
                    $"{{\"error\":\"Legacy sistem hatasÄ±\",\"url\":\"{legacyUrl}\",\"details\":\"{ex.Message}\"}}", 
                    System.Text.Encoding.UTF8, 
                    "application/json")
            };
        }
    }

    private static Uri BuildTargetUri(Uri baseUrl, Uri requestUri)
    {
        // Preserve the request path+query, replace scheme/host/port with the selected node's BaseUrl.
        var builder = new UriBuilder(requestUri)
        {
            Scheme = baseUrl.Scheme,
            Host = baseUrl.Host,
            Port = baseUrl.IsDefaultPort ? -1 : baseUrl.Port
        };

        return builder.Uri;
    }

    /// <summary>
    /// Path'ten modÃ¼l kodunu Ã§Ä±karÄ±r
    /// Gateway'in kendi endpoint'leri (admin, swagger, health) iÃ§in null dÃ¶ner
    /// </summary>
    private string? ExtractModuleCode(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var legacyPrefix = _options.LegacyPathPrefix.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var newPrefix = _options.NewPathPrefix.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Legacy format check
        if (segments.Length > legacyPrefix.Length)
        {
            bool match = true;
            for (int i = 0; i < legacyPrefix.Length; i++)
            {
                if (!segments[i].Equals(legacyPrefix[i], StringComparison.OrdinalIgnoreCase))
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return MapToModuleCode(segments[legacyPrefix.Length]);
            }
        }

        // New format check
        if (segments.Length > newPrefix.Length)
        {
            bool match = true;
            for (int i = 0; i < newPrefix.Length; i++)
            {
                if (!segments[i].Equals(newPrefix[i], StringComparison.OrdinalIgnoreCase))
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                var moduleName = segments[newPrefix.Length].ToLowerInvariant();
                
                // Gateway'in kendi endpoint'leri - yÃ¶nlendirme yapma
                if (moduleName == "gateway")
                {
                    return null;
                }
                
                return MapToModuleCode(segments[newPrefix.Length]);
            }
        }

        return null;
    }

    /// <summary>
    /// Bypass edilmesi gereken path'ler (Gateway'in kendi endpoint'leri)
    /// </summary>
    private static readonly HashSet<string> BypassSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "gateway", "swagger", "health", "admin", "favicon.ico"
    };

    /// <summary>
    /// Path segment'Ä±nÄ± standart modÃ¼l koduna Ã§evirir
    /// Bypass edilmesi gereken segment'ler iÃ§in null dÃ¶ner
    /// </summary>
    private static string? MapToModuleCode(string segment)
    {
        var lower = segment.ToLowerInvariant();
        
        // Bypass segment'leri
        if (BypassSegments.Contains(lower))
        {
            return null;
        }
        
        return lower switch
        {
            "auth" => "Auth",
            "login" => "Auth",
            "usage" => "Usage",
            "bill" => "Bill",
            "billlimit" => "Bill",
            "product" => "Product",
            "features" => "Features",
            "subscriber" => "Subscriber",
            "lead" => "Lead",
            "guest" => "Guest",
            "linesuspension" => "LineSuspension",
            "otp" => "Otp",
            "campaigns" => "Campaigns",
            "packages" => "Packages",
            "thk" => "Thk",
            "adid" => "Adid",
            "document" => "Document",
            "pratiknet" => "PratikNet",
            "settings" => "Settings",
            "endtoend" => "EndToEnd",
            "profile" => "Profile",
            "autopayment" => "AutoPayment",
            "flow" => "Flow",
            "address" => "Address",
            "image" => "Image",
            "banaozel" => "BanaOzel",
            "dashboard" => "Dashboard",
            "digitt" => "Digitt",
            "genericmessages" => "GenericMessages",
            _ => ToPascalCase(segment)
        };
    }

    /// <summary>
    /// String'i PascalCase'e Ã§evirir
    /// </summary>
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpperInvariant(input[0]) + input[1..].ToLowerInvariant();
    }

    /// <summary>
    /// ModÃ¼l kodunu normalleÅŸtirir (KonfigÃ¼rasyon anahtarÄ± uyumu iÃ§in)
    /// </summary>
    private static string NormalizeModuleCode(string moduleCode)
    {
        // ocelot.json metadata'daki key'ler ToPascalCase ile oluÅŸturuluyor
        return ToPascalCase(moduleCode);
    }
}

