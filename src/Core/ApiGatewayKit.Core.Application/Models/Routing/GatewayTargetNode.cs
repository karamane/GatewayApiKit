namespace ApiGatewayKit.Core.Application.Models.Routing;

public sealed class GatewayTargetNode
{
    public required string Id { get; init; }
    public required Uri BaseUrl { get; init; }
    public required bool Enabled { get; init; }
    public required int Weight { get; init; }
}


