using System.Text.Json;
using ApiGatewayKit.Gateway.Configuration;

namespace ApiGatewayKit.Gateway.Services;

/// <summary>
/// ocelot.json'dan route konfigÃ¼rasyonunu okur
/// Admin panel iÃ§in modÃ¼l ve route bilgilerini saÄŸlar
/// </summary>
public interface IRouteConfigurationService
{
    /// <summary>
    /// TÃ¼m modÃ¼lleri ve route'larÄ±nÄ± dÃ¶ndÃ¼rÃ¼r
    /// </summary>
    Task<List<ModuleSummary>> GetModulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Feature flag durumlarÄ±nÄ± dÃ¶ndÃ¼rÃ¼r
    /// </summary>
    Task<Dictionary<string, int>> GetFeatureFlagStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Route konfigÃ¼rasyon servisi implementasyonu
/// </summary>
public class RouteConfigurationService : IRouteConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RouteConfigurationService> _logger;
    private readonly IWebHostEnvironment _environment;

    public RouteConfigurationService(
        IConfiguration configuration,
        ILogger<RouteConfigurationService> logger,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
    }

    public async Task<List<ModuleSummary>> GetModulesAsync(CancellationToken cancellationToken = default)
    {
        var (routes, modulePercentages) = await LoadConfigurationFromOcelotAsync(cancellationToken);

        // Route'larÄ± modÃ¼llere gÃ¶re grupla
        var moduleGroups = routes
            .GroupBy(r => r.Module)
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .OrderBy(g => g.Key);

        var modules = new List<ModuleSummary>();

        foreach (var group in moduleGroups)
        {
            var moduleCode = group.Key;
            var percentage = modulePercentages.TryGetValue(moduleCode, out var pct) ? pct : 0;

            modules.Add(new ModuleSummary
            {
                Code = moduleCode,
                Name = ModuleDefinitions.GetModuleName(moduleCode),
                Description = ModuleDefinitions.GetModuleDescription(moduleCode),
                EndpointCount = group.Count(),
                NewPercentage = percentage,
                Routes = group.OrderBy(r => r.Priority).ThenBy(r => r.LegacyPath).ToList()
            });
        }

        return modules;
    }

    public async Task<Dictionary<string, int>> GetFeatureFlagStatusAsync(CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, int>();

        try
        {
            var ocelotPath = Path.Combine(_environment.ContentRootPath, "ocelot.json");
            if (!File.Exists(ocelotPath)) return result;

            var json = await File.ReadAllTextAsync(ocelotPath, cancellationToken);
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.TryGetProperty("FeatureManagement", out var fmElement))
            {
                foreach (var flag in fmElement.EnumerateObject())
                {
                    // flag.Value -> EnabledFor[0] -> Parameters -> Value
                    if (flag.Value.TryGetProperty("EnabledFor", out var enabledFor) &&
                        enabledFor.GetArrayLength() > 0)
                    {
                        var firstFilter = enabledFor[0];
                        if (firstFilter.TryGetProperty("Name", out var name) && 
                            name.GetString() == "Percentage" &&
                            firstFilter.TryGetProperty("Parameters", out var parameters) &&
                            parameters.TryGetProperty("Value", out var val))
                        {
                            result[flag.Name] = val.GetInt32();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ocelot.json FeatureManagement okuma hatasÄ±");
        }

        return result;
    }

    private async Task<(List<RouteDefinition> routes, Dictionary<string, int> modulePercentages)> LoadConfigurationFromOcelotAsync(CancellationToken cancellationToken)
    {
        var routes = new List<RouteDefinition>();
        var moduleFlags = await GetFeatureFlagStatusAsync(cancellationToken);
        
        // flag name'den modÃ¼l kodunu Ã§Ä±kar (Auth_UseNew -> Auth)
        var modulePercentages = moduleFlags.ToDictionary(
            kvp => kvp.Key.Replace("_UseNew", ""), 
            kvp => kvp.Value);

        try
        {
            var ocelotPath = Path.Combine(_environment.ContentRootPath, "ocelot.json");
            if (!File.Exists(ocelotPath)) return (routes, modulePercentages);

            var json = await File.ReadAllTextAsync(ocelotPath, cancellationToken);
            using var document = JsonDocument.Parse(json);

            // 2. Route tanÄ±mlarÄ±nÄ± oku
            if (document.RootElement.TryGetProperty("Routes", out var routesElement))
            {
                foreach (var route in routesElement.EnumerateArray())
                // ... (rest of the method stays same)
                {
                    var key = route.TryGetProperty("Key", out var keyProp) ? keyProp.GetString() ?? "" : "";
                    var upstreamPath = route.GetProperty("UpstreamPathTemplate").GetString() ?? "";
                    var downstreamPath = route.GetProperty("DownstreamPathTemplate").GetString() ?? "";
                    var priority = route.TryGetProperty("Priority", out var priorityProp) ? priorityProp.GetInt32() : 10;

                    // Metadata'dan override yÃ¼zdesini oku
                    int? overridePct = null;
                    if (route.TryGetProperty("Metadata", out var routeMetadata) &&
                        routeMetadata.TryGetProperty("NewSystemPercentage", out var pctProp))
                    {
                        if (pctProp.ValueKind == JsonValueKind.Number)
                            overridePct = pctProp.GetInt32();
                        else if (pctProp.ValueKind == JsonValueKind.String && int.TryParse(pctProp.GetString(), out var p))
                            overridePct = p;
                    }

                    var methods = new List<string>();
                    if (route.TryGetProperty("UpstreamHttpMethod", out var methodsElement))
                    {
                        foreach (var method in methodsElement.EnumerateArray())
                        {
                            methods.Add(method.GetString() ?? "");
                        }
                    }

                    var moduleCode = ExtractModuleFromPath(upstreamPath);
                    if (key.StartsWith("catchall", StringComparison.OrdinalIgnoreCase)) continue;

                    routes.Add(new RouteDefinition
                    {
                        Key = key,
                        Module = moduleCode,
                        LegacyPath = upstreamPath,
                        NewPath = downstreamPath,
                        HttpMethods = methods,
                        Priority = priority,
                        OverridePercentage = overridePct
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ocelot.json okuma hatasÄ±");
        }

        return (routes, modulePercentages);
    }

    private static string ExtractModuleFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // /moim/api/v1/internet/{module}/...
        if (segments.Length >= 5 &&
            segments[0].Equals("moim", StringComparison.OrdinalIgnoreCase))
        {
            return ToPascalCase(segments[4]);
        }

        // /api/{module}/...
        if (segments.Length >= 2 &&
            segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            return ToPascalCase(segments[1]);
        }

        return string.Empty;
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Ã–zel eÅŸleÅŸtirmeler
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["billlimit"] = "Bill",
            ["genericmessages"] = "GenericMessages",
            ["linesuspension"] = "LineSuspension",
            ["lineSuspension"] = "LineSuspension",
            ["autopayment"] = "AutoPayment",
            ["endtoend"] = "EndToEnd",
            ["pratiknet"] = "PratikNet",
            ["banaozel"] = "BanaOzel",
            ["unicaoffer"] = "Product"
        };

        if (mappings.TryGetValue(input, out var mapped))
        {
            return mapped;
        }

        return char.ToUpperInvariant(input[0]) + input[1..].ToLowerInvariant();
    }
}

