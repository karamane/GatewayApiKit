using System.Net;
using System.Net.Sockets;
using ApiGatewayKit.Core.Application.Interfaces.Logging;

namespace ApiGatewayKit.Infrastructure.Logging.Context;

/// <summary>
/// Correlation context implementasyonu
/// Request boyunca taÅŸÄ±nan context bilgileri
/// </summary>
public class CorrelationContext : ICorrelationContext
{
    private string _correlationId;

    public string CorrelationId
    {
        get => _correlationId;
        set => _correlationId = value;
    }

    public string? ParentCorrelationId { get; private set; }
    public string? UserId { get; set; }
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestPath { get; set; }
    public string ServerName { get; }
    public string? ServerIp { get; }
    public DateTime RequestStartTime { get; }
    public string? SessionId { get; set; }
    public Dictionary<string, object> CustomProperties { get; } = new();

    public CorrelationContext()
    {
        _correlationId = GenerateCorrelationId();
        RequestStartTime = DateTime.UtcNow;
        ServerName = Environment.MachineName;
        ServerIp = GetLocalIpAddress();
    }

    public CorrelationContext(string existingCorrelationId, string? parentCorrelationId = null)
    {
        _correlationId = existingCorrelationId;
        ParentCorrelationId = parentCorrelationId;
        RequestStartTime = DateTime.UtcNow;
        ServerName = Environment.MachineName;
        ServerIp = GetLocalIpAddress();
    }

    /// <summary>
    /// Ã–zel Ã¶zellik ekler
    /// </summary>
    public void SetProperty(string key, object value)
    {
        CustomProperties[key] = value;
    }

    /// <summary>
    /// Ã–zel Ã¶zellik getirir
    /// </summary>
    public T? GetProperty<T>(string key)
    {
        if (CustomProperties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Benzersiz correlation ID oluÅŸturur
    /// Format: timestamp-guid-serverhash
    /// </summary>
    private static string GenerateCorrelationId()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N")[..8];
        var serverHash = Math.Abs(Environment.MachineName.GetHashCode()).ToString("X")[..4];
        return $"{timestamp}-{guid}-{serverHash}";
    }

    /// <summary>
    /// Local IP adresini alÄ±r
    /// </summary>
    private static string? GetLocalIpAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            if (socket.LocalEndPoint is IPEndPoint endPoint)
            {
                return endPoint.Address.ToString();
            }
        }
        catch
        {
            // Ignore - network olmayabilir
        }
        return null;
    }
}


