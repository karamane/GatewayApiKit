using System.Text.Json;
using ApiGatewayKit.Core.Application.Interfaces.Routing;
using ApiGatewayKit.Core.Application.Models.Routing;
using ApiGatewayKit.Gateway.Services;
using ApiGatewayKit.Gateway.Security;
using Microsoft.AspNetCore.Mvc;

namespace ApiGatewayKit.Gateway.Controllers;

/// <summary>
/// Gateway Admin API - Load Balancing yönetimi (node listesi + node enable/disable + route bazlı node enable/disable)
/// </summary>
[ApiController]
[Route("api/gateway/admin")]
[ServiceFilter(typeof(AdminAuditActionFilter))]
[ServiceFilter(typeof(ProductionRestrictionFilter))]
public sealed class LoadBalancingController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IGatewayTargetsProvider _targetsProvider;
    private readonly IGatewayTargetsStore _targetsStore;
    private readonly IRouteNodeOverrideProvider _routeOverrideProvider;
    private readonly IGatewayNodeHealthChecker _nodeHealthChecker;
    private readonly IWebHostEnvironment _environment;

    public LoadBalancingController(
        IGatewayTargetsProvider targetsProvider,
        IGatewayTargetsStore targetsStore,
        IRouteNodeOverrideProvider routeOverrideProvider,
        IGatewayNodeHealthChecker nodeHealthChecker,
        IWebHostEnvironment environment)
    {
        _targetsProvider = targetsProvider;
        _targetsStore = targetsStore;
        _routeOverrideProvider = routeOverrideProvider;
        _nodeHealthChecker = nodeHealthChecker;
        _environment = environment;
    }

    [HttpGet("nodes")]
    [AdminPermission(AdminPermissions.Read)]
    public IActionResult GetNodes()
    {
        GatewayTargetsSnapshot snapshot = _targetsProvider.GetSnapshot();

        return Ok(new
        {
            Legacy = snapshot.LegacyNodes.Select(n => new
            {
                n.Id,
                BaseUrl = n.BaseUrl.ToString(),
                n.Enabled,
                n.Weight,
                EffectiveEnabled = n.Enabled
            }),
            New = snapshot.NewNodes.Select(n => new
            {
                n.Id,
                BaseUrl = n.BaseUrl.ToString(),
                n.Enabled,
                n.Weight,
                EffectiveEnabled = n.Enabled
            })
        });
    }

    [HttpGet("nodes/status")]
    [AdminPermission(AdminPermissions.Read)]
    public async Task<IActionResult> GetNodeStatus(CancellationToken cancellationToken)
    {
        GatewayTargetsSnapshot snapshot = _targetsProvider.GetSnapshot();

        object[] legacy = await BuildStatusesAsync(TargetSystem.Legacy, snapshot.LegacyNodes, cancellationToken);
        object[] @new = await BuildStatusesAsync(TargetSystem.New, snapshot.NewNodes, cancellationToken);

        return Ok(new
        {
            Legacy = legacy,
            New = @new
        });
    }

    [HttpPut("nodes/{nodeId}/enabled")]
    [AdminPermission(AdminPermissions.Write)]
    public async Task<IActionResult> SetNodeEnabled(string nodeId, [FromBody] SetNodeEnabledRequest? request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new { error = "BadRequest", message = "Request body is required." });
        }

        string normalizedNodeId = NormalizeIdOrNull(nodeId);
        if (normalizedNodeId == string.Empty)
        {
            return BadRequest(new { error = "BadRequest", message = "nodeId is invalid." });
        }

        GatewayTargetsSnapshot snapshot = _targetsProvider.GetSnapshot();

        bool existsInLegacy = snapshot.LegacyNodes.Any(n => string.Equals(n.Id, normalizedNodeId, StringComparison.OrdinalIgnoreCase));
        bool existsInNew = snapshot.NewNodes.Any(n => string.Equals(n.Id, normalizedNodeId, StringComparison.OrdinalIgnoreCase));

        if (!existsInLegacy && !existsInNew)
        {
            return NotFound(new { error = "NotFound", message = $"Node '{normalizedNodeId}' was not found." });
        }

        if (existsInLegacy && existsInNew)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { error = "Conflict", message = $"Node '{normalizedNodeId}' is ambiguous across targets." });
        }

        TargetSystem target = existsInLegacy ? TargetSystem.Legacy : TargetSystem.New;
        await _targetsStore.SetNodeEnabledAsync(target, normalizedNodeId, request.Enabled, cancellationToken);

        return Ok(new { NodeId = normalizedNodeId, Target = target.ToString(), Enabled = request.Enabled });
    }

    [HttpGet("routes")]
    [AdminPermission(AdminPermissions.Read)]
    public async Task<IActionResult> GetRoutes(CancellationToken cancellationToken)
    {
        string path = Path.Combine(_environment.ContentRootPath, "ocelot.json");
        Console.WriteLine($"[GetRoutes] Reading from: {path}");
        
        if (!System.IO.File.Exists(path))
        {
            return NotFound(new { error = "NotFound", message = "ocelot.json not found." });
        }

        string json = await System.IO.File.ReadAllTextAsync(path, cancellationToken);
        
        // Log first route's DisabledNodes for debugging
        Console.WriteLine($"[GetRoutes] File size: {json.Length} chars, LastWrite: {System.IO.File.GetLastWriteTime(path)}");
        using JsonDocument document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("Routes", out JsonElement routesElement) ||
            routesElement.ValueKind != JsonValueKind.Array)
        {
            return Ok(new { Routes = Array.Empty<object>() });
        }

        var result = new List<object>();

        int routeIndex = 0;
        foreach (JsonElement route in routesElement.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();

            string routeKey = route.TryGetProperty("Key", out JsonElement keyEl) ? keyEl.GetString() ?? string.Empty : string.Empty;
            string upstreamPath = route.TryGetProperty("UpstreamPathTemplate", out JsonElement upEl) ? upEl.GetString() ?? string.Empty : string.Empty;

            var methods = new List<string>();
            if (route.TryGetProperty("UpstreamHttpMethod", out JsonElement methodsEl) && methodsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement m in methodsEl.EnumerateArray())
                {
                    string? method = m.GetString();
                    if (!string.IsNullOrWhiteSpace(method))
                    {
                        methods.Add(method);
                    }
                }
            }

            RouteDisabledNodes disabled = ReadDisabledNodesFromRouteMetadata(route);

            // Log first few routes for debugging
            if (routeIndex < 3)
            {
                Console.WriteLine($"[GetRoutes] Route[{routeIndex}]: Key={routeKey}, DisabledNodes.Legacy=[{string.Join(",", disabled.Legacy)}], DisabledNodes.New=[{string.Join(",", disabled.New)}]");
            }
            routeIndex++;

            result.Add(new
            {
                Key = routeKey,
                UpstreamPathTemplate = upstreamPath,
                UpstreamHttpMethod = methods,
                DisabledNodes = new
                {
                    Legacy = disabled.Legacy,
                    New = disabled.New
                }
            });
        }

        return Ok(new { Routes = result });
    }

    private static RouteDisabledNodes ReadDisabledNodesFromRouteMetadata(JsonElement route)
    {
        if (!route.TryGetProperty("Metadata", out JsonElement metadata) || metadata.ValueKind != JsonValueKind.Object)
        {
            return new RouteDisabledNodes();
        }

        if (!metadata.TryGetProperty("DisabledNodes", out JsonElement disabledNodes) || disabledNodes.ValueKind != JsonValueKind.Object)
        {
            return new RouteDisabledNodes();
        }

        // Case-insensitive lookup for Legacy and New
        return new RouteDisabledNodes
        {
            Legacy = ReadStringArrayCaseInsensitive(disabledNodes, "Legacy"),
            New = ReadStringArrayCaseInsensitive(disabledNodes, "New")
        };
    }

    private static IReadOnlyCollection<string> ReadStringArrayCaseInsensitive(JsonElement parent, string propertyName)
    {
        // Try exact match first
        if (parent.TryGetProperty(propertyName, out JsonElement element))
        {
            return ReadStringArray(parent, propertyName);
        }

        // Try lowercase match
        if (parent.TryGetProperty(propertyName.ToLowerInvariant(), out JsonElement elementLower))
        {
            return ReadStringArray(parent, propertyName.ToLowerInvariant());
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyCollection<string> ReadStringArray(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement element) || element.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var list = new List<string>();
        foreach (JsonElement item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string? value = item.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            list.Add(value.Trim());
        }

        return list;
    }

    [HttpPut("routes/nodes")]
    [AdminPermission(AdminPermissions.Write)]
    public async Task<IActionResult> SetRouteNodeEnabled(
        [FromBody] SetRouteNodeEnabledRequest? request,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"[SetRouteNodeEnabled] Called with request={System.Text.Json.JsonSerializer.Serialize(request)}");
        
        if (request == null)
        {
            Console.WriteLine("[SetRouteNodeEnabled] Request body is null");
            return BadRequest(new { error = "BadRequest", message = "Request body is required." });
        }

        string normalizedRouteKey = NormalizeRouteKeyOrNull(request.RouteKey);
        if (normalizedRouteKey == string.Empty)
        {
            Console.WriteLine($"[SetRouteNodeEnabled] Invalid routeKey: {request.RouteKey}");
            return BadRequest(new { error = "BadRequest", message = "routeKey is invalid." });
        }

        string normalizedNodeId = NormalizeIdOrNull(request.NodeId);
        if (normalizedNodeId == string.Empty)
        {
            Console.WriteLine($"[SetRouteNodeEnabled] Invalid nodeId: {request.NodeId}");
            return BadRequest(new { error = "BadRequest", message = "nodeId is invalid." });
        }

        if (!Enum.TryParse<TargetSystem>(request.Target, ignoreCase: true, out TargetSystem target))
        {
            Console.WriteLine($"[SetRouteNodeEnabled] Invalid target: {request.Target}");
            return BadRequest(new { error = "BadRequest", message = "target must be 'Legacy' or 'New'." });
        }

        if (!RouteExists(normalizedRouteKey))
        {
            Console.WriteLine($"[SetRouteNodeEnabled] Route not found: {normalizedRouteKey}");
            return NotFound(new { error = "NotFound", message = $"Route '{normalizedRouteKey}' was not found." });
        }

        Console.WriteLine($"[SetRouteNodeEnabled] Calling SetNodeEnabledAsync: routeKey={normalizedRouteKey}, target={target}, nodeId={normalizedNodeId}, enabled={request.Enabled}");
        
        await _routeOverrideProvider.SetNodeEnabledAsync(normalizedRouteKey, target, normalizedNodeId, request.Enabled, cancellationToken);

        Console.WriteLine($"[SetRouteNodeEnabled] Success");
        
        return Ok(new { RouteKey = normalizedRouteKey, Target = target.ToString(), NodeId = normalizedNodeId, Enabled = request.Enabled });
    }

    private bool RouteExists(string routeKey)
    {
        string path = Path.Combine(_environment.ContentRootPath, "ocelot.json");
        if (!System.IO.File.Exists(path))
        {
            return false;
        }

        string json = System.IO.File.ReadAllText(path);
        using JsonDocument document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("Routes", out JsonElement routesElement) ||
            routesElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (JsonElement route in routesElement.EnumerateArray())
        {
            // Key veya UpstreamPathTemplate ile eşleşme yap
            string key = route.TryGetProperty("Key", out JsonElement keyEl) ? keyEl.GetString() ?? string.Empty : string.Empty;
            string upstreamPath = route.TryGetProperty("UpstreamPathTemplate", out JsonElement pathEl) ? pathEl.GetString() ?? string.Empty : string.Empty;
            
            if (string.Equals(key, routeKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(upstreamPath, routeKey, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeIdOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string trimmed = value.Trim();

        foreach (char c in trimmed)
        {
            bool ok = char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.';
            if (!ok)
            {
                return string.Empty;
            }
        }

        return trimmed;
    }

    /// <summary>
    /// Route key'leri normalize eder. Route key'ler path içerebilir.
    /// </summary>
    private static string NormalizeRouteKeyOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string trimmed = value.Trim();

        // URL decode (örn: %2F -> /)
        string decoded = Uri.UnescapeDataString(trimmed);

        foreach (char c in decoded)
        {
            // Route key'ler path içerebilir, bu yüzden / karakterine izin ver
            bool ok = char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' || c == '/';
            if (!ok)
            {
                return string.Empty;
            }
        }

        return decoded;
    }

    private async Task<object[]> BuildStatusesAsync(
        TargetSystem target,
        IReadOnlyCollection<GatewayTargetNode> nodes,
        CancellationToken cancellationToken)
    {
        const int maxParallelism = 8;
        using var gate = new SemaphoreSlim(maxParallelism);

        var tasks = nodes.Select(async node =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                NodeHealthResult result = await _nodeHealthChecker.CheckAsync(node.BaseUrl, cancellationToken);

                return new
                {
                    Target = target.ToString(),
                    node.Id,
                    Ready = result.Ready,
                    result.StatusCode,
                    result.Hint
                };
            }
            finally
            {
                gate.Release();
            }
        });

        return await Task.WhenAll(tasks);
    }

    public sealed class SetNodeEnabledRequest
    {
        public bool Enabled { get; set; }
    }

    public sealed class SetRouteNodeEnabledRequest
    {
        public string RouteKey { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public bool Enabled { get; set; }
    }
}


