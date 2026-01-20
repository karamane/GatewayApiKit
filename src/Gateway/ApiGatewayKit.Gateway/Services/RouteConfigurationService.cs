using System.Text.Json;
using ApiGatewayKit.Gateway.Configuration;

namespace ApiGatewayKit.Gateway.Services;

/// <summary>
/// ocelot.json'dan route konfigürasyonunu okur
/// Admin panel için modül ve route bilgilerini sağlar
/// </summary>
public interface IRouteConfigurationService
{
    /// <summary>
    /// Tüm modülleri ve route'larını döndürür
    /// </summary>
    Task<List<ModuleSummary>> GetModulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Feature flag durumlarını döndürür
    /// </summary>
    Task<Dictionary<string, int>> GetFeatureFlagStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Route konfigürasyon servisi implementasyonu
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

        // Route'ları modüllere göre grupla
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
            _logger.LogError(ex, "ocelot.json FeatureManagement okuma hatası");
        }

        return result;
    }

    private async Task<(List<RouteDefinition> routes, Dictionary<string, int> modulePercentages)> LoadConfigurationFromOcelotAsync(CancellationToken cancellationToken)
    {
        var routes = new List<RouteDefinition>();
        var moduleFlags = await GetFeatureFlagStatusAsync(cancellationToken);
        
        // flag name'den modül kodunu çıkar (Auth_UseNew -> Auth)
        var modulePercentages = moduleFlags.ToDictionary(
            kvp => kvp.Key.Replace("_UseNew", ""), 
            kvp => kvp.Value);

        try
        {
            var ocelotPath = Path.Combine(_environment.ContentRootPath, "ocelot.json");
            if (!File.Exists(ocelotPath)) return (routes, modulePercentages);

            var json = await File.ReadAllTextAsync(ocelotPath, cancellationToken);
            using var document = JsonDocument.Parse(json);

            // 2. Route tanımlarını oku
            if (document.RootElement.TryGetProperty("Routes", out var routesElement))
            {
                foreach (var route in routesElement.EnumerateArray())
                {
                    var key = route.TryGetProperty("Key", out var keyProp) ? keyProp.GetString() ?? "" : "";
                    var upstreamPath = route.GetProperty("UpstreamPathTemplate").GetString() ?? "";
                    var downstreamPath = route.GetProperty("DownstreamPathTemplate").GetString() ?? "";
                    var priority = route.TryGetProperty("Priority", out var priorityProp) ? priorityProp.GetInt32() : 10;

                    // Metadata'dan Module ve override yüzdesini oku
                    string? metadataModule = null;
                    int? overridePct = null;
                    
                    if (route.TryGetProperty("Metadata", out var routeMetadata))
                    {
                        // Öncelik 1: Metadata.Module (açıkça tanımlı)
                        if (routeMetadata.TryGetProperty("Module", out var moduleProp))
                        {
                            metadataModule = moduleProp.GetString();
                        }
                        
                        // Override yüzdesi
                        if (routeMetadata.TryGetProperty("NewSystemPercentage", out var pctProp))
                        {
                            if (pctProp.ValueKind == JsonValueKind.Number)
                                overridePct = pctProp.GetInt32();
                            else if (pctProp.ValueKind == JsonValueKind.String && int.TryParse(pctProp.GetString(), out var p))
                                overridePct = p;
                        }
                    }

                    // Modül belirleme: Metadata > Path Parsing (fallback)
                    var moduleCode = !string.IsNullOrWhiteSpace(metadataModule) 
                        ? metadataModule 
                        : ExtractModuleFromPath(upstreamPath);

                    var methods = new List<string>();
                    if (route.TryGetProperty("UpstreamHttpMethod", out var methodsElement))
                    {
                        foreach (var method in methodsElement.EnumerateArray())
                        {
                            methods.Add(method.GetString() ?? "");
                        }
                    }

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
            _logger.LogError(ex, "ocelot.json okuma hatası");
        }

        return (routes, modulePercentages);
    }

    /// <summary>
    /// Path'ten modül kodunu çıkarır - Config'deki pattern'leri kullanır
    /// </summary>
    private string ExtractModuleFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return string.Empty;

        // Config'den path pattern'lerini oku
        var pathPatterns = _configuration.GetSection("ModuleParsing:PathPatterns").Get<List<PathPatternConfig>>() 
            ?? new List<PathPatternConfig>();

        foreach (var pattern in pathPatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern.Prefix)) continue;
            
            var prefixSegments = pattern.Prefix.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            // Prefix match kontrolü
            if (segments.Length > pattern.ModuleSegmentIndex && 
                segments.Length >= prefixSegments.Length &&
                MatchesPrefix(segments, prefixSegments))
            {
                var rawModule = segments[pattern.ModuleSegmentIndex];
                return NormalizeModuleName(rawModule);
            }
        }

        // Hiçbir pattern eşleşmedi - boş döndür
        return string.Empty;
    }

    /// <summary>
    /// Segment dizisinin prefix ile başlayıp başlamadığını kontrol eder
    /// </summary>
    private static bool MatchesPrefix(string[] segments, string[] prefixSegments)
    {
        for (int i = 0; i < prefixSegments.Length; i++)
        {
            if (!segments[i].Equals(prefixSegments[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Modül adını normalize eder - Config'deki alias'ları kullanır
    /// </summary>
    private string NormalizeModuleName(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Config'den alias'ları oku
        var aliases = _configuration.GetSection("ModuleParsing:ModuleAliases")
            .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();

        // Alias varsa kullan
        foreach (var alias in aliases)
        {
            if (alias.Key.Equals(input, StringComparison.OrdinalIgnoreCase))
            {
                return alias.Value;
            }
        }

        // PascalCase'e çevir
        return char.ToUpperInvariant(input[0]) + input[1..].ToLowerInvariant();
    }
}

/// <summary>
/// Path pattern konfigürasyonu
/// </summary>
public class PathPatternConfig
{
    /// <summary>
    /// Path prefix'i (örn: "moim/api/v1/internet")
    /// </summary>
    public string Prefix { get; set; } = string.Empty;
    
    /// <summary>
    /// Modül segment index'i (0-based)
    /// </summary>
    public int ModuleSegmentIndex { get; set; }
}