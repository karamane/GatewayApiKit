using ApiGatewayKit.Core.Application.Interfaces.Routing;
using ApiGatewayKit.Core.Application.Models.Routing;

namespace ApiGatewayKit.Gateway.Services;

public sealed class TargetNodeSelector : ITargetNodeSelector
{
    private readonly IGatewayTargetsProvider _targetsProvider;
    private readonly IRouteNodeOverrideProvider _overrideProvider;

    public TargetNodeSelector(IGatewayTargetsProvider targetsProvider, IRouteNodeOverrideProvider overrideProvider)
    {
        _targetsProvider = targetsProvider;
        _overrideProvider = overrideProvider;
    }

    public async Task<GatewayTargetNode> SelectNodeAsync(
        TargetSystem target,
        string? routeKey,
        string? upstreamPath,
        CancellationToken cancellationToken)
    {
        GatewayTargetsSnapshot snapshot = _targetsProvider.GetSnapshot();

        IReadOnlyList<GatewayTargetNode> nodes = target == TargetSystem.Legacy
            ? snapshot.LegacyNodes
            : snapshot.NewNodes;

        if (nodes.Count == 0)
        {
            throw new InvalidOperationException($"No nodes configured for target '{target}'.");
        }

        HashSet<string> disabled = await GetDisabledSetAsync(target, routeKey, cancellationToken);

        List<GatewayTargetNode> effectiveEnabled = nodes
            .Where(n => n.Enabled && !disabled.Contains(n.Id))
            .ToList();

        if (effectiveEnabled.Count == 0)
        {
            string routeInfo = !string.IsNullOrWhiteSpace(routeKey) ? routeKey : upstreamPath ?? "unknown";
            throw new InvalidOperationException($"No effective enabled nodes for target '{target}' (route: {routeInfo}).");
        }

        return SelectWeightedRandom(effectiveEnabled);
    }

    private async Task<HashSet<string>> GetDisabledSetAsync(TargetSystem target, string? routeKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(routeKey))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        RouteDisabledNodes disabled = await _overrideProvider.GetDisabledNodesAsync(routeKey.Trim(), cancellationToken);

        return target == TargetSystem.Legacy
            ? new HashSet<string>(disabled.Legacy, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(disabled.New, StringComparer.OrdinalIgnoreCase);
    }

    private static GatewayTargetNode SelectWeightedRandom(IReadOnlyList<GatewayTargetNode> nodes)
    {
        int totalWeight = 0;
        foreach (GatewayTargetNode node in nodes)
        {
            checked
            {
                totalWeight += node.Weight;
            }
        }

        int pick = Random.Shared.Next(1, totalWeight + 1);
        int cumulative = 0;

        foreach (GatewayTargetNode node in nodes)
        {
            cumulative += node.Weight;
            if (pick <= cumulative)
            {
                return node;
            }
        }

        return nodes[^1];
    }
}


