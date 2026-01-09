using ApiGatewayKit.Core.Application.Models.Routing;

namespace ApiGatewayKit.Core.Application.Interfaces.Routing;

public interface IGatewayTargetsStore : IGatewayTargetsProvider
{
    Task SetNodeEnabledAsync(TargetSystem target, string nodeId, bool enabled, CancellationToken cancellationToken);
}


