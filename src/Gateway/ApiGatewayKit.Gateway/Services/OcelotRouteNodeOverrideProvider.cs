using ApiGatewayKit.Core.Application.Interfaces.Routing;
using ApiGatewayKit.Core.Application.Models.Routing;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApiGatewayKit.Gateway.Services;

public sealed class OcelotRouteNodeOverrideProvider : IRouteNodeOverrideProvider
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<OcelotRouteNodeOverrideProvider> _logger;
    private readonly object _fileLock = new();

    public OcelotRouteNodeOverrideProvider(IWebHostEnvironment environment, ILogger<OcelotRouteNodeOverrideProvider> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public Task<RouteDisabledNodes> GetDisabledNodesAsync(string routeKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(routeKey))
        {
            throw new ArgumentException("routeKey is required.", nameof(routeKey));
        }

        cancellationToken.ThrowIfCancellationRequested();

        JObject ocelot = LoadOcelotJsonOrThrow();
        JObject? route = FindRouteByKey(ocelot, routeKey.Trim());

        if (route == null)
        {
            _logger.LogDebug("GetDisabledNodesAsync: route {RouteKey} not found", routeKey);
            return Task.FromResult(new RouteDisabledNodes());
        }

        JObject? metadata = route["Metadata"] as JObject;
        JObject? disabledNodes = metadata?["DisabledNodes"] as JObject;

        var result = new RouteDisabledNodes
        {
            Legacy = ReadStringArrayCaseInsensitive(disabledNodes, "Legacy"),
            New = ReadStringArrayCaseInsensitive(disabledNodes, "New")
        };

        _logger.LogDebug(
            "GetDisabledNodesAsync: routeKey={RouteKey}, Legacy=[{Legacy}], New=[{New}]",
            routeKey, string.Join(", ", result.Legacy), string.Join(", ", result.New));

        return Task.FromResult(result);
    }

    private static IReadOnlyCollection<string> ReadStringArrayCaseInsensitive(JObject? parent, string propertyName)
    {
        if (parent == null)
        {
            return Array.Empty<string>();
        }

        // Case-insensitive property lookup
        JProperty? prop = parent.Properties()
            .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

        return ReadStringArray(prop?.Value);
    }

    public Task SetNodeEnabledAsync(
        string routeKey,
        TargetSystem target,
        string nodeId,
        bool enabled,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(routeKey))
        {
            throw new ArgumentException("routeKey is required.", nameof(routeKey));
        }

        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("nodeId is required.", nameof(nodeId));
        }

        string normalizedRouteKey = routeKey.Trim();
        string normalizedNodeId = nodeId.Trim();

        Console.WriteLine($"[SetNodeEnabledAsync] ENTRY: routeKey={normalizedRouteKey}, target={target}, nodeId={normalizedNodeId}, enabled={enabled}");
        _logger.LogInformation(
            "SetNodeEnabledAsync called: routeKey={RouteKey}, target={Target}, nodeId={NodeId}, enabled={Enabled}",
            normalizedRouteKey, target, normalizedNodeId, enabled);

        lock (_fileLock)
        {
            cancellationToken.ThrowIfCancellationRequested();

            JObject ocelot = LoadOcelotJsonOrThrow();
            JObject route = FindRouteByKey(ocelot, normalizedRouteKey)
                ?? throw new InvalidOperationException($"Route '{normalizedRouteKey}' was not found in ocelot.json.");

            _logger.LogDebug("Found route: {RouteKey}", normalizedRouteKey);

            string targetKey = target == TargetSystem.Legacy ? "Legacy" : "New";
            string otherKey = target == TargetSystem.Legacy ? "New" : "Legacy";

            // Read current disabled lists
            JObject? existingMetadata = route["Metadata"] as JObject;
            JObject? existingDisabledNodes = existingMetadata?["DisabledNodes"] as JObject;
            
            List<string> currentTargetList = new();
            List<string> currentOtherList = new();
            
            if (existingDisabledNodes != null)
            {
                // Case-insensitive read for Legacy
                var legacyProp = existingDisabledNodes.Properties()
                    .FirstOrDefault(p => string.Equals(p.Name, "Legacy", StringComparison.OrdinalIgnoreCase));
                if (legacyProp?.Value is JArray legacyArr)
                {
                    currentTargetList = target == TargetSystem.Legacy 
                        ? legacyArr.Values<string>().Where(s => s != null).ToList()!
                        : currentTargetList;
                    currentOtherList = target == TargetSystem.New 
                        ? legacyArr.Values<string>().Where(s => s != null).ToList()!
                        : currentOtherList;
                }
                
                // Case-insensitive read for New
                var newProp = existingDisabledNodes.Properties()
                    .FirstOrDefault(p => string.Equals(p.Name, "New", StringComparison.OrdinalIgnoreCase));
                if (newProp?.Value is JArray newArr)
                {
                    currentTargetList = target == TargetSystem.New 
                        ? newArr.Values<string>().Where(s => s != null).ToList()!
                        : currentTargetList;
                    currentOtherList = target == TargetSystem.Legacy 
                        ? newArr.Values<string>().Where(s => s != null).ToList()!
                        : currentOtherList;
                }
            }

            Console.WriteLine($"[SetNodeEnabledAsync] Before: {targetKey}=[{string.Join(",", currentTargetList)}]");

            bool contains = currentTargetList.Any(v => string.Equals(v, normalizedNodeId, StringComparison.OrdinalIgnoreCase));

            if (enabled && contains)
            {
                // Remove from list
                currentTargetList = currentTargetList
                    .Where(v => !string.Equals(v, normalizedNodeId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                Console.WriteLine($"[SetNodeEnabledAsync] REMOVED {normalizedNodeId}");
                _logger.LogInformation("Removed {NodeId} from DisabledNodes.{Target} for route {RouteKey}", normalizedNodeId, targetKey, normalizedRouteKey);
            }
            else if (!enabled && !contains)
            {
                // Add to list
                currentTargetList.Add(normalizedNodeId);
                Console.WriteLine($"[SetNodeEnabledAsync] ADDED {normalizedNodeId}");
                _logger.LogInformation("Added {NodeId} to DisabledNodes.{Target} for route {RouteKey}", normalizedNodeId, targetKey, normalizedRouteKey);
            }

            Console.WriteLine($"[SetNodeEnabledAsync] After: {targetKey}=[{string.Join(",", currentTargetList)}]");

            // Rebuild the entire Metadata.DisabledNodes structure
            JObject newDisabledNodes = new JObject
            {
                [targetKey] = new JArray(currentTargetList),
                [otherKey] = new JArray(currentOtherList)
            };

            // Preserve other metadata properties
            JObject newMetadata = new JObject();
            if (existingMetadata != null)
            {
                foreach (var prop in existingMetadata.Properties())
                {
                    if (!string.Equals(prop.Name, "DisabledNodes", StringComparison.OrdinalIgnoreCase))
                    {
                        newMetadata[prop.Name] = prop.Value.DeepClone();
                    }
                }
            }
            newMetadata["DisabledNodes"] = newDisabledNodes;

            // Replace entire Metadata in route
            route["Metadata"] = newMetadata;

            Console.WriteLine($"[SetNodeEnabledAsync] Rebuilt Metadata, saving...");
            SaveOcelotJson(ocelot);
            Console.WriteLine("[SetNodeEnabledAsync] SaveOcelotJson completed");
        }

        return Task.CompletedTask;
    }

    private JObject LoadOcelotJsonOrThrow()
    {
        string path = Path.Combine(_environment.ContentRootPath, "ocelot.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("ocelot.json not found in content root.", path);
        }

        string json = File.ReadAllText(path);
        return JObject.Parse(json);
    }

    private JObject? FindRouteByKey(JObject ocelot, string routeKey)
    {
        JArray? routes = ocelot["Routes"] as JArray;
        if (routes == null)
        {
            return null;
        }

        foreach (JToken token in routes)
        {
            if (token is not JObject route)
            {
                continue;
            }

            string? key = route["Key"]?.ToString();
            if (string.Equals(key, routeKey, StringComparison.OrdinalIgnoreCase))
            {
                return route;
            }
        }

        return null;
    }

    private static IReadOnlyCollection<string> ReadStringArray(JToken? token)
    {
        if (token is not JArray arr)
        {
            return Array.Empty<string>();
        }

        return arr.Values<string>()
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToArray();
    }

    private void SaveOcelotJson(JObject ocelot)
    {
        string path = Path.Combine(_environment.ContentRootPath, "ocelot.json");
        
        _logger.LogWarning("[SaveOcelotJson] Starting save to: {Path}", path);
        _logger.LogWarning("[SaveOcelotJson] File exists before write: {Exists}", File.Exists(path));
        
        // Get the routes section to log what we're about to write
        JArray? routes = ocelot["Routes"] as JArray;
        JObject? firstRoute = routes?.FirstOrDefault() as JObject;
        string firstRouteKey = firstRoute?["Key"]?.ToString() ?? "unknown";
        JObject? firstMetadata = firstRoute?["Metadata"] as JObject;
        JObject? firstDisabledNodes = firstMetadata?["DisabledNodes"] as JObject;
        string disabledLegacy = firstDisabledNodes?["Legacy"]?.ToString() ?? "null";
        
        _logger.LogWarning("[SaveOcelotJson] First route key={Key}, DisabledNodes.Legacy={Legacy}", 
            firstRouteKey, disabledLegacy);
        
        try
        {
            string json = ocelot.ToString(Formatting.Indented);
            
            _logger.LogWarning("[SaveOcelotJson] JSON length: {Length} chars", json.Length);
            
            File.WriteAllText(path, json);
            
            // Verify by re-reading
            string verifyJson = File.ReadAllText(path);
            bool matches = json == verifyJson;
            
            _logger.LogWarning("[SaveOcelotJson] File written. Size: {Size} bytes. Verification: {Match}", 
                new FileInfo(path).Length, matches ? "OK" : "MISMATCH!");
            
            if (!matches)
            {
                _logger.LogError("[SaveOcelotJson] CRITICAL: Written content doesn't match read-back content!");
            }
            
            _logger.LogInformation("ocelot.json updated (route node overrides) at {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SaveOcelotJson] FAILED to save ocelot.json at {Path}", path);
            throw;
        }
    }
}


