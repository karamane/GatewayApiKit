using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApiGatewayKit.Gateway.Services.DownstreamHealth;

/// <summary>
/// Downstream servislerin sağlık kontrolünü yapan servis interface'i.
/// </summary>
public interface IDownstreamHealthChecker
{
    /// <summary>
    /// Belirli bir downstream servisin sağlık durumunu kontrol eder.
    /// Kök dizinine (/) GET isteği atarak JSON yanıtını parse eder.
    /// </summary>
    /// <param name="serviceId">Servis tanımlayıcısı</param>
    /// <param name="targetSystem">Hedef sistem (Legacy/New)</param>
    /// <param name="baseUrl">Servisin base URL'i</param>
    /// <param name="cancellationToken">İptal tokeni</param>
    /// <returns>Sağlık durumu</returns>
    Task<DownstreamHealthStatus> CheckHealthAsync(
        string serviceId,
        string targetSystem,
        Uri baseUrl,
        CancellationToken cancellationToken);
}
