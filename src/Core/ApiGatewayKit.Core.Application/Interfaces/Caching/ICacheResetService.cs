namespace ApiGatewayKit.Core.Application.Interfaces.Caching;

public interface ICacheResetService
{
    Task ResetAsync(CancellationToken ct = default);
    Task PreloadAsync(CancellationToken ct = default);
}








