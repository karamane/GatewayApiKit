namespace ApiGatewayKit.Core.Application.Interfaces.Caching;

public interface ICachePreloadProvider
{
    Task<Dictionary<string, object>> GetPreloadDataAsync(CancellationToken ct = default);
}








