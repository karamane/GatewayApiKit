namespace ApiGatewayKit.Core.Application.Interfaces.Caching;

public interface ICacheProvider
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task<IEnumerable<string>> GetAllKeysAsync(string? pattern = null, CancellationToken ct = default);
}








