namespace ApiGatewayKit.Core.Application.Models.Routing;

public sealed class GatewayTargetsSnapshot
{
    public required IReadOnlyList<GatewayTargetNode> LegacyNodes { get; init; }
    public required IReadOnlyList<GatewayTargetNode> NewNodes { get; init; }
}


