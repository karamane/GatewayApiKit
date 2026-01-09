namespace ApiGatewayKit.Gateway.Configuration;

/// <summary>
/// Routing stratejisinin dondurdugu karar
/// </summary>
public record RoutingDecision
{
    /// <summary>
    /// Yonlendirilecek hedef URL
    /// </summary>
    public string TargetUrl { get; init; } = string.Empty;

    /// <summary>
    /// Shadow mode aktif mi?
    /// </summary>
    public bool IsShadowMode { get; init; }

    /// <summary>
    /// Shadow modda kullanilacak ikincil URL
    /// </summary>
    public string? ShadowUrl { get; init; }

    /// <summary>
    /// Hangi sisteme yonlendirildi?
    /// </summary>
    public RoutingTarget ResolvedTarget { get; init; }

    /// <summary>
    /// Uygulanan kural (null ise varsayilan kullanildi)
    /// </summary>
    public RoutingRule? AppliedRule { get; init; }
}




