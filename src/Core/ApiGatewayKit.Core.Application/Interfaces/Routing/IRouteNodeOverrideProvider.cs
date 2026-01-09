using ApiGatewayKit.Core.Application.Models.Routing;

namespace ApiGatewayKit.Core.Application.Interfaces.Routing;

public interface IRouteNodeOverrideProvider
{
    Task<RouteDisabledNodes> GetDisabledNodesAsync(string routeKey, CancellationToken cancellationToken);

    Task SetNodeEnabledAsync(
        string routeKey,
        TargetSystem target,
        string nodeId,
        bool enabled,
        CancellationToken cancellationToken);
}


