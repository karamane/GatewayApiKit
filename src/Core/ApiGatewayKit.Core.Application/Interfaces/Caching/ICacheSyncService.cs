namespace ApiGatewayKit.Core.Application.Interfaces.Caching;

public interface ICacheSyncService
{
    Task PublishAsync(string key, string? value, CancellationToken ct = default);
    Task PublishRemoveAsync(string key, CancellationToken ct = default);
}








