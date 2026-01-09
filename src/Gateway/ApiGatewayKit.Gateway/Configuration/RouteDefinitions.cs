using Microsoft.Extensions.Configuration;

namespace ApiGatewayKit.Gateway.Configuration;

/// <summary>
/// Route tanımları - ocelot.json'dan parse edilir
/// Admin panelde gösterim için kullanılır
/// </summary>
public class RouteDefinition
{
    /// <summary>
    /// Benzersiz route anahtarı
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Modül kodu (Auth, Bill, Usage vb.)
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Legacy path (Upstream)
    /// </summary>
    public string LegacyPath { get; set; } = string.Empty;

    /// <summary>
    /// Yeni path (Downstream)
    /// </summary>
    public string NewPath { get; set; } = string.Empty;

    /// <summary>
    /// HTTP metotları
    /// </summary>
    public List<string> HttpMethods { get; set; } = new();

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Öncelik
    /// </summary>
    public int Priority { get; set; } = 10;

    /// <summary>
    /// Endpoint bazlı yüzde override (null ise modül oranını kullanır)
    /// </summary>
    public int? OverridePercentage { get; set; }
}

/// <summary>
/// Modül özeti - Admin panelde gösterim için
/// </summary>
public class ModuleSummary
{
    /// <summary>
    /// Modül kodu
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Modül adı (Türkçe)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Endpoint sayısı
    /// </summary>
    public int EndpointCount { get; set; }

    /// <summary>
    /// Yeni sisteme yönlendirme yüzdesi
    /// </summary>
    public int NewPercentage { get; set; }

    /// <summary>
    /// Bu modüle ait route'lar
    /// </summary>
    public List<RouteDefinition> Routes { get; set; } = new();
}

/// <summary>
/// Modül bilgisi
/// </summary>
public class ModuleInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Modül tanımları provider - Config'den okur
/// </summary>
public interface IModuleDefinitionsProvider
{
    string GetModuleName(string moduleCode);
    string GetModuleDescription(string moduleCode);
    IReadOnlyDictionary<string, ModuleInfo> GetAllModules();
}

/// <summary>
/// Modül tanımları provider implementasyonu - appsettings.json'dan okur
/// </summary>
public sealed class ModuleDefinitionsProvider : IModuleDefinitionsProvider
{
    private readonly IReadOnlyDictionary<string, ModuleInfo> _modules;

    public ModuleDefinitionsProvider(IConfiguration configuration)
    {
        var moduleSection = configuration.GetSection("ModuleDefinitions");
        var modules = new Dictionary<string, ModuleInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var child in moduleSection.GetChildren())
        {
            var moduleCode = child.Key;
            var name = child.GetValue<string>("Name") ?? moduleCode;
            var description = child.GetValue<string>("Description") ?? string.Empty;

            modules[moduleCode] = new ModuleInfo
            {
                Name = name,
                Description = description
            };
        }

        _modules = modules;
    }

    public string GetModuleName(string moduleCode)
    {
        return _modules.TryGetValue(moduleCode, out var info) ? info.Name : moduleCode;
    }

    public string GetModuleDescription(string moduleCode)
    {
        return _modules.TryGetValue(moduleCode, out var info) ? info.Description : string.Empty;
    }

    public IReadOnlyDictionary<string, ModuleInfo> GetAllModules()
    {
        return _modules;
    }
}

/// <summary>
/// Modül tanımları - Geriye uyumluluk için static accessor
/// NOT: Yeni kodda IModuleDefinitionsProvider kullanın
/// </summary>
public static class ModuleDefinitions
{
    private static IModuleDefinitionsProvider? _provider;

    /// <summary>
    /// Provider'ı DI'dan set eder - Uygulama başlangıcında çağrılmalı
    /// </summary>
    public static void Initialize(IModuleDefinitionsProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Modül kodundan Türkçe isim döndürür
    /// </summary>
    public static string GetModuleName(string moduleCode)
    {
        return _provider?.GetModuleName(moduleCode) ?? moduleCode;
    }

    /// <summary>
    /// Modül kodundan açıklama döndürür
    /// </summary>
    public static string GetModuleDescription(string moduleCode)
    {
        return _provider?.GetModuleDescription(moduleCode) ?? string.Empty;
    }
}
