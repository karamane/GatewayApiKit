namespace ApiGatewayKit.Gateway.FeatureManagement;

/// <summary>
/// Feature flag sabitleri
/// </summary>
public static class FeatureFlags
{
    /// <summary>
    /// Auth servisi icin yeni sistemi kullan
    /// </summary>
    public const string UseNewAuthService = "UseNewAuthService";

    /// <summary>
    /// Musteri servisi icin yeni sistemi kullan
    /// </summary>
    public const string UseNewCustomerService = "UseNewCustomerService";

    /// <summary>
    /// Fatura servisi icin yeni sistemi kullan
    /// </summary>
    public const string UseNewBillingService = "UseNewBillingService";

    /// <summary>
    /// Siparis servisi icin yeni sistemi kullan
    /// </summary>
    public const string UseNewOrderService = "UseNewOrderService";

    /// <summary>
    /// Urun servisi icin yeni sistemi kullan
    /// </summary>
    public const string UseNewProductService = "UseNewProductService";

    /// <summary>
    /// Route path'ine gore feature flag adini dondurur
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>Feature flag adi veya null</returns>
    public static string? GetFeatureFlagForPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        var lowerPath = path.ToLowerInvariant();

        if (lowerPath.StartsWith("/api/auth"))
            return UseNewAuthService;

        if (lowerPath.StartsWith("/api/customers") || lowerPath.StartsWith("/api/customer"))
            return UseNewCustomerService;

        if (lowerPath.StartsWith("/api/billing") || lowerPath.StartsWith("/api/bills") || lowerPath.StartsWith("/api/invoices"))
            return UseNewBillingService;

        if (lowerPath.StartsWith("/api/orders") || lowerPath.StartsWith("/api/order") || lowerPath.StartsWith("/api/subscriptions"))
            return UseNewOrderService;

        if (lowerPath.StartsWith("/api/products") || lowerPath.StartsWith("/api/product") || lowerPath.StartsWith("/api/campaigns"))
            return UseNewProductService;

        return null;
    }
}




