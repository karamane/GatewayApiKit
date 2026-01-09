using ApiGatewayKit.Core.Application.Models.Routing;

namespace ApiGatewayKit.Core.Application.Interfaces.Routing;

public interface ITargetNodeSelector
{
    Task<GatewayTargetNode> SelectNodeAsync(
        TargetSystem target,
        string? routeKey,
        string? upstreamPath,
        CancellationToken cancellationToken);
}


