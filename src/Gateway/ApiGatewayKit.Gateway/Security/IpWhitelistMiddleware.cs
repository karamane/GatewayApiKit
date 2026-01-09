using System.Net;
using Microsoft.Extensions.Options;

namespace ApiGatewayKit.Gateway.Security;

/// <summary>
/// Admin endpoint'leri iÃ§in IP whitelist kontrolÃ¼ yapan middleware.
/// CIDR notation destekler (Ã¶rn: 10.0.0.0/8, 192.168.0.0/16)
/// </summary>
public class IpWhitelistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpWhitelistMiddleware> _logger;
    private readonly AdminSecurityOptions _options;
    private readonly List<(IPAddress Network, int PrefixLength)> _parsedNetworks;
    private readonly IWebHostEnvironment _environment;

    public IpWhitelistMiddleware(
        RequestDelegate next,
        ILogger<IpWhitelistMiddleware> logger,
        IOptions<AdminSecurityOptions> options,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _environment = environment;
        _parsedNetworks = ParseNetworks(_options.IpWhitelist);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Sadece admin endpoint'lerini kontrol et
        if (!context.Request.Path.StartsWithSegments("/api/gateway/admin"))
        {
            await _next(context);
            return;
        }

        // Tools endpoint'i sadece Development'ta gÃ¼venlik kontrolÃ¼nden muaf
        if (_environment.IsDevelopment() && context.Request.Path.StartsWithSegments("/api/gateway/admin/tools"))
        {
            await _next(context);
            return;
        }

        // GÃ¼venlik devre dÄ±ÅŸÄ± ise geÃ§
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var remoteIp = context.Connection.RemoteIpAddress;

        // IP alÄ±namadÄ±ysa reddet
        if (remoteIp == null)
        {
            _logger.LogWarning("Admin access denied: Could not determine remote IP address");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = "IP address could not be determined"
            });
            return;
        }

        // IPv4-mapped IPv6 adresini IPv4'e Ã§evir
        if (remoteIp.IsIPv4MappedToIPv6)
        {
            remoteIp = remoteIp.MapToIPv4();
        }

        // Whitelist kontrolÃ¼
        if (!IsIpAllowed(remoteIp))
        {
            _logger.LogWarning(
                "Admin access denied for IP {RemoteIp} - not in whitelist. Path: {Path}",
                remoteIp,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = "Your IP address is not authorized to access this resource"
            });
            return;
        }

        _logger.LogDebug("Admin access allowed for IP {RemoteIp}", remoteIp);
        await _next(context);
    }

    /// <summary>
    /// IP adresinin whitelist'te olup olmadÄ±ÄŸÄ±nÄ± kontrol eder
    /// </summary>
    private bool IsIpAllowed(IPAddress remoteIp)
    {
        // HiÃ§ whitelist yoksa localhost'a izin ver
        if (_parsedNetworks.Count == 0)
        {
            return IPAddress.IsLoopback(remoteIp);
        }

        foreach (var (network, prefixLength) in _parsedNetworks)
        {
            // Exact match
            if (prefixLength == -1)
            {
                if (remoteIp.Equals(network))
                    return true;
                continue;
            }

            // CIDR match
            if (IsInNetwork(remoteIp, network, prefixLength))
                return true;
        }

        return false;
    }

    /// <summary>
    /// IP adresinin verilen network/prefix iÃ§inde olup olmadÄ±ÄŸÄ±nÄ± kontrol eder
    /// </summary>
    private static bool IsInNetwork(IPAddress address, IPAddress network, int prefixLength)
    {
        // AynÄ± address family olmalÄ±
        if (address.AddressFamily != network.AddressFamily)
        {
            // IPv4 mapped IPv6 kontrolÃ¼
            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();
            if (network.IsIPv4MappedToIPv6)
                network = network.MapToIPv4();

            if (address.AddressFamily != network.AddressFamily)
                return false;
        }

        var addressBytes = address.GetAddressBytes();
        var networkBytes = network.GetAddressBytes();

        if (addressBytes.Length != networkBytes.Length)
            return false;

        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        // Full byte'larÄ± karÅŸÄ±laÅŸtÄ±r
        for (var i = 0; i < fullBytes; i++)
        {
            if (addressBytes[i] != networkBytes[i])
                return false;
        }

        // Kalan bit'leri karÅŸÄ±laÅŸtÄ±r
        if (remainingBits > 0 && fullBytes < addressBytes.Length)
        {
            var mask = (byte)(0xFF << (8 - remainingBits));
            if ((addressBytes[fullBytes] & mask) != (networkBytes[fullBytes] & mask))
                return false;
        }

        return true;
    }

    /// <summary>
    /// IP whitelist string'lerini parse eder
    /// </summary>
    private List<(IPAddress Network, int PrefixLength)> ParseNetworks(List<string> ipList)
    {
        var result = new List<(IPAddress, int)>();

        foreach (var entry in ipList)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entry))
                    continue;

                var trimmed = entry.Trim();

                // CIDR notation: 10.0.0.0/8
                if (trimmed.Contains('/'))
                {
                    var parts = trimmed.Split('/');
                    if (parts.Length == 2 &&
                        IPAddress.TryParse(parts[0], out var network) &&
                        int.TryParse(parts[1], out var prefix))
                    {
                        result.Add((network, prefix));
                        _logger.LogDebug("Parsed CIDR: {Network}/{Prefix}", network, prefix);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid CIDR notation in IP whitelist: {Entry}", entry);
                    }
                }
                // Single IP
                else if (IPAddress.TryParse(trimmed, out var ip))
                {
                    result.Add((ip, -1)); // -1 = exact match
                    _logger.LogDebug("Parsed IP: {Ip}", ip);
                }
                else
                {
                    _logger.LogWarning("Invalid IP address in whitelist: {Entry}", entry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing IP whitelist entry: {Entry}", entry);
            }
        }

        _logger.LogInformation("IP whitelist configured with {Count} entries", result.Count);
        return result;
    }
}

/// <summary>
/// Extension methods for IpWhitelistMiddleware
/// </summary>
public static class IpWhitelistMiddlewareExtensions
{
    public static IApplicationBuilder UseAdminIpWhitelist(this IApplicationBuilder app)
    {
        return app.UseMiddleware<IpWhitelistMiddleware>();
    }
}


