namespace ApiGatewayKit.Gateway.Configuration;

/// <summary>
/// Yonlendirme hedef tipleri
/// </summary>
public enum RoutingTarget
{
    /// <summary>
    /// Tum trafik eski sisteme yonlendirilir
    /// </summary>
    Legacy,

    /// <summary>
    /// Tum trafik yeni sisteme yonlendirilir
    /// </summary>
    New,

    /// <summary>
    /// Trafik yuzdelik olarak dagitilir (Canary deployment)
    /// </summary>
    Split,

    /// <summary>
    /// Her iki sisteme gonderilir, legacy'den cevap doner (test icin)
    /// </summary>
    Shadow
}




