namespace ApiGatewayKit.Core.Application.Models.Routing;

public sealed class RouteDisabledNodes
{
    public IReadOnlyCollection<string> Legacy { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> New { get; init; } = Array.Empty<string>();
}


