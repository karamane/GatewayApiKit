namespace ApiGatewayKit.Core.Shared.Options;

/// <summary>
/// Hosting yapÄ±landÄ±rma seÃ§enekleri
/// Konsol veya Windows Service olarak Ã§alÄ±ÅŸtÄ±rma
/// </summary>
public class HostingOptions
{
    public const string SectionName = "Hosting";

    /// <summary>
    /// Windows Service olarak Ã§alÄ±ÅŸtÄ±r
    /// </summary>
    public bool RunAsWindowsService { get; set; } = false;

    /// <summary>
    /// Windows Service adÄ±
    /// </summary>
    public string ServiceName { get; set; } = "ApiGatewayKit";

    /// <summary>
    /// Windows Service aÃ§Ä±klamasÄ±
    /// </summary>
    public string ServiceDisplayName { get; set; } = "ApiGatewayKit Service";

    /// <summary>
    /// Windows Service aÃ§Ä±klamasÄ±
    /// </summary>
    public string ServiceDescription { get; set; } = "ApiGatewayKit .NET Service";

    /// <summary>
    /// Graceful shutdown timeout (saniye)
    /// </summary>
    public int ShutdownTimeoutSeconds { get; set; } = 30;
}


