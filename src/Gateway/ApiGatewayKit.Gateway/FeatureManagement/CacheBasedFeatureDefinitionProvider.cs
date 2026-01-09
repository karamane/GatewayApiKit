using System.Text.Json;
using Microsoft.FeatureManagement;
using ApiGatewayKit.Core.Application.Interfaces.Caching;

namespace ApiGatewayKit.Gateway.FeatureManagement;

/// <summary>
/// Cache tabanli feature definition provider
/// Redis cache'den feature flag tanimlarini okur
/// Runtime'da dinamik olarak feature flag'leri degistirmeyi destekler
/// </summary>
public class CacheBasedFeatureDefinitionProvider : IFeatureDefinitionProvider
{
    private readonly ICacheProvider _cache;
    private readonly ILogger<CacheBasedFeatureDefinitionProvider> _logger;

    private const string FeatureCachePrefix = "feature:";

    public CacheBasedFeatureDefinitionProvider(
        ICacheProvider cache,
        ILogger<CacheBasedFeatureDefinitionProvider> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<FeatureDefinition?> GetFeatureDefinitionAsync(string featureName)
    {
        var cacheKey = $"{FeatureCachePrefix}{featureName}";

        try
        {
            var cachedValue = await _cache.GetAsync<string>(cacheKey);

            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Feature definition not found in cache: {FeatureName}", featureName);
                return null;
            }

            var definition = JsonSerializer.Deserialize<CachedFeatureDefinition>(cachedValue, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (definition == null)
            {
                return null;
            }

            return ConvertToFeatureDefinition(featureName, definition);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get feature definition from cache: {FeatureName}", featureName);
            return null;
        }
    }

    public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
    {
        IEnumerable<string> keys;

        try
        {
            keys = await _cache.GetAllKeysAsync(FeatureCachePrefix);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get feature keys from cache");
            yield break;
        }

        foreach (var key in keys)
        {
            var featureName = key.Replace(FeatureCachePrefix, "");
            var definition = await GetFeatureDefinitionAsync(featureName);

            if (definition != null)
            {
                yield return definition;
            }
        }
    }

    private FeatureDefinition ConvertToFeatureDefinition(string featureName, CachedFeatureDefinition cached)
    {
        var enabledFor = new List<FeatureFilterConfiguration>();

        if (cached.EnabledFor != null)
        {
            foreach (var filter in cached.EnabledFor)
            {
                enabledFor.Add(new FeatureFilterConfiguration
                {
                    Name = filter.Name,
                    Parameters = CreateConfigurationFromDictionary(filter.Parameters)
                });
            }
        }

        return new FeatureDefinition
        {
            Name = featureName,
            EnabledFor = enabledFor
        };
    }

    private static IConfiguration CreateConfigurationFromDictionary(Dictionary<string, object>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            return new ConfigurationBuilder().Build();
        }

        var flattenedDict = new Dictionary<string, string?>();
        FlattenDictionary(parameters, "", flattenedDict);

        return new ConfigurationBuilder()
            .AddInMemoryCollection(flattenedDict)
            .Build();
    }

    private static void FlattenDictionary(
        Dictionary<string, object> source,
        string prefix,
        Dictionary<string, string?> destination)
    {
        foreach (var kvp in source)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}:{kvp.Key}";

            if (kvp.Value is JsonElement jsonElement)
            {
                FlattenJsonElement(jsonElement, key, destination);
            }
            else if (kvp.Value is Dictionary<string, object> nestedDict)
            {
                FlattenDictionary(nestedDict, key, destination);
            }
            else
            {
                destination[key] = kvp.Value?.ToString();
            }
        }
    }

    private static void FlattenJsonElement(JsonElement element, string key, Dictionary<string, string?> destination)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    FlattenJsonElement(property.Value, $"{key}:{property.Name}", destination);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    FlattenJsonElement(item, $"{key}:{index}", destination);
                    index++;
                }
                break;

            default:
                destination[key] = element.ToString();
                break;
        }
    }
}

/// <summary>
/// Cache'de saklanan feature taniminin modeli
/// </summary>
public class CachedFeatureDefinition
{
    public bool Enabled { get; set; } = true;
    public List<CachedFeatureFilter>? EnabledFor { get; set; }
}

/// <summary>
/// Cache'de saklanan feature filter modeli
/// </summary>
public class CachedFeatureFilter
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
}




