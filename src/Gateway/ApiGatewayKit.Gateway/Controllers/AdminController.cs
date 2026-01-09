using Microsoft.AspNetCore.Mvc;
using ApiGatewayKit.Gateway.Configuration;
using ApiGatewayKit.Gateway.Security;
using ApiGatewayKit.Gateway.Services;

namespace ApiGatewayKit.Gateway.Controllers;

/// <summary>
/// Gateway Admin API - Route ve Feature Flag yönetimi (ocelot.json tabanlı)
/// Güvenlik: IP Whitelist + API Key + Permission-based Authorization
/// </summary>
[ApiController]
[Route("api/gateway/admin")]
[ServiceFilter(typeof(AdminAuditActionFilter))]
[ServiceFilter(typeof(ProductionRestrictionFilter))]
public class AdminController : ControllerBase
{
    private readonly IRouteConfigurationService _routeService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminController> _logger;
    private readonly IWebHostEnvironment _environment;

    public AdminController(
        IRouteConfigurationService routeService,
        IConfiguration configuration,
        ILogger<AdminController> logger,
        IWebHostEnvironment environment)
    {
        _routeService = routeService;
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Tüm modülleri ve route'larını döndürür
    /// </summary>
    [HttpGet("modules")]
    [AdminPermission(AdminPermissions.Read)]
    public async Task<IActionResult> GetModules(CancellationToken cancellationToken)
    {
        var modules = await _routeService.GetModulesAsync(cancellationToken);

        return Ok(new
        {
            Zaman = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
            ToplamModul = modules.Count,
            ToplamEndpoint = modules.Sum(m => m.EndpointCount),
            Moduller = modules
        });
    }

    /// <summary>
    /// Belirli bir modülün detaylarını döndürür
    /// </summary>
    [HttpGet("modules/{moduleCode}")]
    [AdminPermission(AdminPermissions.Read)]
    public async Task<IActionResult> GetModule(string moduleCode, CancellationToken cancellationToken)
    {
        var modules = await _routeService.GetModulesAsync(cancellationToken);
        var module = modules.FirstOrDefault(m =>
            m.Code.Equals(moduleCode, StringComparison.OrdinalIgnoreCase));

        if (module == null)
        {
            return NotFound(new { Hata = $"Modül bulunamadı: {moduleCode}" });
        }

        return Ok(module);
    }

    /// <summary>
    /// Feature flag durumlarını döndürür
    /// </summary>
    [HttpGet("feature-flags")]
    [AdminPermission(AdminPermissions.Read)]
    public async Task<IActionResult> GetFeatureFlags(CancellationToken cancellationToken)
    {
        var flags = await _routeService.GetFeatureFlagStatusAsync(cancellationToken);

        return Ok(new
        {
            Zaman = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
            ToplamFlag = flags.Count,
            Flags = flags.Select(f => new
            {
                Anahtar = f.Key,
                Modul = f.Key.Replace("_UseNew", ""),
                YeniYuzde = f.Value,
                Durum = f.Value == 0 ? "Legacy" : f.Value == 100 ? "Yeni" : $"Bölünmüş (%{f.Value})"
            }).OrderBy(f => f.Modul)
        });
    }

    /// <summary>
    /// Sistem sağlık durumu
    /// </summary>
    [HttpGet("health")]
    [AdminPermission(AdminPermissions.Read)]
    public IActionResult GetHealth()
    {
        var gatewayConfig = _configuration.GetSection("Gateway");

        return Ok(new
        {
            Durum = "Sağlıklı",
            Zaman = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
            Ortam = _environment.EnvironmentName,
            Versiyon = _configuration["Logging:ApplicationVersion"] ?? "1.0.0",
            VarsayilanHedef = gatewayConfig["DefaultTarget"] ?? "Legacy",
            LegacyUrl = gatewayConfig["LegacyBaseUrl"],
            YeniUrl = gatewayConfig["NewBaseUrl"],
            OcelotDosyasi = System.IO.File.Exists(
                Path.Combine(_environment.ContentRootPath, "ocelot.json")) ? "Mevcut" : "Bulunamadı"
        });
    }

    /// <summary>
    /// Konfigürasyon bilgilerini döndürür
    /// </summary>
    [HttpGet("config")]
    [AdminPermission(AdminPermissions.Read)]
    public IActionResult GetConfig()
    {
        var gatewayConfig = _configuration.GetSection("Gateway");

        return Ok(new
        {
            Gateway = new
            {
                VarsayilanHedef = gatewayConfig["DefaultTarget"],
                LegacyBaseUrl = gatewayConfig["LegacyBaseUrl"],
                YeniBaseUrl = gatewayConfig["NewBaseUrl"],
                LegacyPathPrefix = gatewayConfig["LegacyPathPrefix"],
                YeniPathPrefix = gatewayConfig["NewPathPrefix"],
                ZamanAsimi = gatewayConfig["RequestTimeoutSeconds"],
                YenidenDenemeSayisi = gatewayConfig["RetryCount"]
            },
            HotReload = "Aktif - ocelot.json ve appsettings.json değişiklikleri otomatik yüklenir"
        });
    }

    /// <summary>
    /// Tüm modülleri legacy'ye döndürür (Acil durum)
    /// ocelot.json'daki tüm yüzdeleri 0 yapar
    /// </summary>
    [HttpPost("emergency/rollback")]
    [AdminPermission(AdminPermissions.Emergency)]
    public async Task<IActionResult> EmergencyRollback(CancellationToken cancellationToken)
    {
        _logger.LogWarning("ACİL DURUM: Tüm modüller Legacy'ye yönlendiriliyor!");

        try
        {
            var ocelotPath = Path.Combine(_environment.ContentRootPath, "ocelot.json");
            var json = await System.IO.File.ReadAllTextAsync(ocelotPath, cancellationToken);
            var ocelot = Newtonsoft.Json.Linq.JObject.Parse(json);

            var featureManagement = ocelot["FeatureManagement"] as Newtonsoft.Json.Linq.JObject;
            if (featureManagement != null)
            {
                foreach (var flag in featureManagement.Properties())
                {
                    var flagSection = flag.Value as Newtonsoft.Json.Linq.JObject;
                    var enabledFor = flagSection?["EnabledFor"] as Newtonsoft.Json.Linq.JArray;
                    if (enabledFor != null && enabledFor.Count > 0)
                    {
                        var parameters = enabledFor[0]["Parameters"] as Newtonsoft.Json.Linq.JObject;
                        if (parameters != null)
                        {
                            parameters["Value"] = 0;
                        }
                    }
                }
            }

            // Route bazlı override'ları da temizle
            var routes = ocelot["Routes"] as Newtonsoft.Json.Linq.JArray;
            if (routes != null)
            {
                foreach (var route in routes)
                {
                    var routeMetadata = route["Metadata"] as Newtonsoft.Json.Linq.JObject;
                    routeMetadata?.Remove("NewSystemPercentage");
                }
            }

            await System.IO.File.WriteAllTextAsync(ocelotPath, ocelot.ToString(Newtonsoft.Json.Formatting.Indented), cancellationToken);

            return Ok(new
            {
                Mesaj = "Tüm trafik Legacy sisteme yönlendirildi (%0)",
                Zaman = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Rollback hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Modül tanımlarını döndürür (Türkçe isim ve açıklamalar)
    /// </summary>
    [HttpGet("module-definitions")]
    [AdminPermission(AdminPermissions.Read)]
    public IActionResult GetModuleDefinitions()
    {
        return Ok(new
        {
            Moduller = ModuleDefinitions.Modules.Select(m => new
            {
                Kod = m.Key,
                Ad = m.Value.Name,
                Aciklama = m.Value.Description
            }).OrderBy(m => m.Ad)
        });
    }

    /// <summary>
    /// Modül yüzdesini günceller (ocelot.json içindeki FeatureManagement'ı günceller)
    /// </summary>
    [HttpPut("modules/{moduleCode}/percentage")]
    [AdminPermission(AdminPermissions.Write)]
    public async Task<IActionResult> UpdateModulePercentage(
        string moduleCode, 
        [FromBody] UpdatePercentageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Percentage < 0 || request.Percentage > 100)
        {
            return BadRequest("Yüzde 0-100 arasında olmalıdır");
        }
        
        _logger.LogInformation("Modül yüzdesi güncelleniyor: {Module} -> %{Percentage}", 
            moduleCode, request.Percentage);

        try
        {
            var ocelotPath = Path.Combine(_environment.ContentRootPath, "ocelot.json");
            if (!System.IO.File.Exists(ocelotPath)) return NotFound("ocelot.json bulunamadı");

            var json = await System.IO.File.ReadAllTextAsync(ocelotPath, cancellationToken);
            var ocelot = Newtonsoft.Json.Linq.JObject.Parse(json);

            var featureManagement = ocelot["FeatureManagement"] as Newtonsoft.Json.Linq.JObject;
            if (featureManagement == null)
            {
                featureManagement = new Newtonsoft.Json.Linq.JObject();
                ocelot["FeatureManagement"] = featureManagement;
            }

            var flagKey = $"{moduleCode}_UseNew";
            var flagSection = new Newtonsoft.Json.Linq.JObject
            {
                ["EnabledFor"] = new Newtonsoft.Json.Linq.JArray
                {
                    new Newtonsoft.Json.Linq.JObject
                    {
                        ["Name"] = "Percentage",
                        ["Parameters"] = new Newtonsoft.Json.Linq.JObject { ["Value"] = request.Percentage }
                    }
                }
            };

            featureManagement[flagKey] = flagSection;

            await System.IO.File.WriteAllTextAsync(ocelotPath, ocelot.ToString(Newtonsoft.Json.Formatting.Indented), cancellationToken);

            return Ok(new
            {
                Mesaj = $"{moduleCode} modülü %{request.Percentage} yeni sisteme yönlendirildi",
                Modul = moduleCode,
                Yuzde = request.Percentage,
                Zaman = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Modül yüzdesi güncellenirken hata: {Module}", moduleCode);
            return StatusCode(500, $"Güncelleme hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Endpoint override ayarlar (ocelot.json'a kaydedilir)
    /// </summary>
    [HttpPut("routes/override")]
    [AdminPermission(AdminPermissions.Write)]
    public async Task<IActionResult> SetRouteOverride([FromBody] RouteOverrideRequest? request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrEmpty(request.Path)) return BadRequest("Path gerekli");

        try
        {
            var ocelotPath = Path.Combine(_environment.ContentRootPath, "ocelot.json");
            if (!System.IO.File.Exists(ocelotPath)) return NotFound("ocelot.json bulunamadı");

            var json = await System.IO.File.ReadAllTextAsync(ocelotPath, cancellationToken);
            var ocelot = Newtonsoft.Json.Linq.JObject.Parse(json);

            var routes = ocelot["Routes"] as Newtonsoft.Json.Linq.JArray;
            if (routes == null) return BadRequest("Routes bölümü bulunamadı");

            bool found = false;
            foreach (var route in routes)
            {
                var upstreamPath = route["UpstreamPathTemplate"]?.ToString();
                if (string.Equals(upstreamPath, request.Path, StringComparison.OrdinalIgnoreCase))
                {
                    var metadata = route["Metadata"] as Newtonsoft.Json.Linq.JObject;
                    if (metadata == null)
                    {
                        metadata = new Newtonsoft.Json.Linq.JObject();
                        route["Metadata"] = metadata;
                    }

                    if (request.Percentage.HasValue)
                        metadata["NewSystemPercentage"] = request.Percentage.Value;
                    else
                        metadata.Remove("NewSystemPercentage");

                    found = true;
                    break;
                }
            }

            if (!found) return NotFound($"Route bulunamadı: {request.Path}");

            await System.IO.File.WriteAllTextAsync(ocelotPath, ocelot.ToString(Newtonsoft.Json.Formatting.Indented), cancellationToken);

            return Ok(new
            {
                Mesaj = request.Percentage == null ? "Override kaldırıldı" : "Override kaydedildi",
                Path = request.Path,
                Yuzde = request.Percentage,
                Zaman = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route override kaydedilirken hata: {Path}", request.Path);
            return StatusCode(500, $"Güncelleme hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Tüm aktif endpoint override'ları döndürür
    /// </summary>
    [HttpGet("routes/overrides")]
    [AdminPermission(AdminPermissions.Read)]
    public async Task<IActionResult> GetRouteOverrides(CancellationToken cancellationToken)
    {
        var modules = await _routeService.GetModulesAsync(cancellationToken);
        var overrides = modules.SelectMany(m => m.Routes)
            .Where(r => r.OverridePercentage.HasValue)
            .Select(r => new
            {
                Path = r.LegacyPath,
                Yuzde = r.OverridePercentage.Value,
                Hedef = r.OverridePercentage.Value == 0 ? "Legacy" : r.OverridePercentage.Value == 100 ? "Yeni" : $"Bölünmüş (%{r.OverridePercentage.Value})"
            }).OrderBy(o => o.Path).ToList();

        return Ok(new
        {
            Toplam = overrides.Count,
            Overrides = overrides,
            Zaman = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
        });
    }

    /// <summary>
    /// Belirli bir endpoint override'ını kaldırır
    /// </summary>
    [HttpDelete("routes/override")]
    [AdminPermission(AdminPermissions.Write)]
    public async Task<IActionResult> RemoveRouteOverride([FromQuery] string path, CancellationToken cancellationToken)
    {
        return await SetRouteOverride(new RouteOverrideRequest { Path = path, Percentage = null }, cancellationToken);
    }

    /// <summary>
    /// Tüm endpoint override'larını temizler
    /// </summary>
    [HttpDelete("routes/overrides/clear")]
    [AdminPermission(AdminPermissions.Write)]
    public async Task<IActionResult> ClearAllOverrides(CancellationToken cancellationToken)
    {
        try
        {
            var ocelotPath = Path.Combine(_environment.ContentRootPath, "ocelot.json");
            var json = await System.IO.File.ReadAllTextAsync(ocelotPath, cancellationToken);
            var ocelot = Newtonsoft.Json.Linq.JObject.Parse(json);

            var routes = ocelot["Routes"] as Newtonsoft.Json.Linq.JArray;
            if (routes != null)
            {
                foreach (var route in routes)
                {
                    var metadata = route["Metadata"] as Newtonsoft.Json.Linq.JObject;
                    metadata?.Remove("NewSystemPercentage");
                }
            }

            await System.IO.File.WriteAllTextAsync(ocelotPath, ocelot.ToString(Newtonsoft.Json.Formatting.Indented), cancellationToken);
            return Ok(new { Mesaj = "Tüm override'lar temizlendi" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}

public class UpdatePercentageRequest
{
    public int Percentage { get; set; }
}

public class RouteOverrideRequest
{
    public string ModuleCode { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int? Percentage { get; set; }
}

