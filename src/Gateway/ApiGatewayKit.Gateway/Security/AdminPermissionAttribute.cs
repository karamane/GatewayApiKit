using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace ApiGatewayKit.Gateway.Security;

/// <summary>
/// Admin endpoint'leri iÃ§in izin kontrolÃ¼ yapan attribute.
/// API Key'in sahip olduÄŸu izinlerle endpoint'in gerektirdiÄŸi izni karÅŸÄ±laÅŸtÄ±rÄ±r.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AdminPermissionAttribute : TypeFilterAttribute
{
    /// <summary>
    /// Gerekli izin (read, write, emergency)
    /// </summary>
    public string RequiredPermission { get; }

    public AdminPermissionAttribute(string requiredPermission) : base(typeof(AdminPermissionFilter))
    {
        RequiredPermission = requiredPermission;
        Arguments = new object[] { requiredPermission };
    }
}

/// <summary>
/// AdminPermissionAttribute iÃ§in filter implementasyonu
/// </summary>
public class AdminPermissionFilter : IAsyncAuthorizationFilter
{
    private readonly string _requiredPermission;
    private readonly ILogger<AdminPermissionFilter> _logger;
    private readonly AdminSecurityOptions _options;

    public AdminPermissionFilter(
        string requiredPermission,
        ILogger<AdminPermissionFilter> logger,
        IOptions<AdminSecurityOptions> options)
    {
        _requiredPermission = requiredPermission;
        _logger = logger;
        _options = options.Value;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // GÃ¼venlik devre dÄ±ÅŸÄ± ise geÃ§
        if (!_options.Enabled)
        {
            return Task.CompletedTask;
        }

        // API Key konfigÃ¼re edilmemiÅŸse geÃ§ (IP whitelist yeterli)
        if (_options.ApiKeys.Count == 0)
        {
            return Task.CompletedTask;
        }

        // Context'ten permissions listesini al
        var permissions = context.HttpContext.GetAdminPermissions();

        // HiÃ§ permission yoksa (authenticated deÄŸil) reddet
        if (permissions.Count == 0)
        {
            _logger.LogWarning(
                "Admin permission denied: No permissions found. Required: {Required}, Path: {Path}",
                _requiredPermission,
                context.HttpContext.Request.Path);

            context.Result = new JsonResult(new
            {
                error = "Forbidden",
                message = $"Authentication required to access this resource"
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return Task.CompletedTask;
        }

        // Ä°zin kontrolÃ¼
        if (!AdminPermissions.HasPermission(permissions, _requiredPermission))
        {
            var keyName = context.HttpContext.Items.TryGetValue("AdminKeyName", out var name)
                ? name?.ToString() ?? "unknown"
                : "unknown";

            _logger.LogWarning(
                "Admin permission denied: Key '{KeyName}' has [{Permissions}] but requires '{Required}'. Path: {Path}",
                keyName,
                string.Join(", ", permissions),
                _requiredPermission,
                context.HttpContext.Request.Path);

            context.Result = new JsonResult(new
            {
                error = "Forbidden",
                message = $"Insufficient permissions. Required: {_requiredPermission}",
                yourPermissions = permissions
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return Task.CompletedTask;
        }

        _logger.LogDebug(
            "Admin permission granted: {Permission} for {Path}",
            _requiredPermission,
            context.HttpContext.Request.Path);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Production ortamÄ± kÄ±sÄ±tlamalarÄ± iÃ§in filter
/// </summary>
public class ProductionRestrictionFilter : IAsyncAuthorizationFilter
{
    private readonly ILogger<ProductionRestrictionFilter> _logger;
    private readonly AdminSecurityOptions _options;
    private readonly IWebHostEnvironment _environment;

    public ProductionRestrictionFilter(
        ILogger<ProductionRestrictionFilter> logger,
        IOptions<AdminSecurityOptions> options,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _options = options.Value;
        _environment = environment;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Sadece production'da Ã§alÄ±ÅŸ
        if (!_environment.IsProduction())
        {
            return Task.CompletedTask;
        }

        var path = context.HttpContext.Request.Path.ToString();
        var method = context.HttpContext.Request.Method;

        // Write endpoint kÄ±sÄ±tlamasÄ±
        if (_options.Production.DisableWriteEndpoints &&
            method is "POST" or "PUT" or "PATCH" or "DELETE")
        {
            _logger.LogWarning(
                "Production restriction: Write operations disabled. Method: {Method}, Path: {Path}",
                method,
                path);

            context.Result = new JsonResult(new
            {
                error = "Forbidden",
                message = "Write operations are disabled in production environment"
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return Task.CompletedTask;
        }

        // Allowed endpoints kontrolÃ¼
        if (_options.Production.AllowedEndpoints.Count > 0)
        {
            var isAllowed = _options.Production.AllowedEndpoints
                .Any(e => path.Contains(e, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                _logger.LogWarning(
                    "Production restriction: Endpoint not in allowed list. Path: {Path}",
                    path);

                context.Result = new JsonResult(new
                {
                    error = "Forbidden",
                    message = "This endpoint is not available in production environment"
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
}


