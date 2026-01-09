namespace ApiGatewayKit.Core.Application.Interfaces.Caching;

/// <summary>
/// Parametre cache servisi
/// Uygulama parametrelerinin yÃ¶netimi iÃ§in
/// </summary>
public interface IParameterCacheService
{
    /// <summary>
    /// Parametre deÄŸerini getirir
    /// </summary>
    Task<T?> GetParameterAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parametre deÄŸerini getirir, yoksa default deÄŸer dÃ¶ner
    /// </summary>
    Task<T> GetParameterAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parametre deÄŸerini gÃ¼nceller
    /// </summary>
    Task SetParameterAsync<T>(string key, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// TÃ¼m parametreleri yeniden yÃ¼kler
    /// </summary>
    Task RefreshAllParametersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen parametreyi yeniden yÃ¼kler
    /// </summary>
    Task RefreshParameterAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parametre deÄŸiÅŸikliÄŸi bildirir (Pub/Sub)
    /// TÃ¼m sunuculara cache refresh sinyali gÃ¶nderir
    /// </summary>
    Task NotifyParameterChangedAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// TÃ¼m parametreleri dictionary olarak getirir
    /// </summary>
    Task<Dictionary<string, object>> GetAllParametersAsync(CancellationToken cancellationToken = default);
}


