using ApiGatewayKit.Core.Application.Models.Routing;

namespace ApiGatewayKit.Core.Application.Interfaces.Routing;

public interface IGatewayTargetsProvider
{
    GatewayTargetsSnapshot GetSnapshot();
}


