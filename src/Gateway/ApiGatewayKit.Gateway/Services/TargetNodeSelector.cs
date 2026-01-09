using ApiGatewayKit.Core.Application.Interfaces.Routing;
using ApiGatewayKit.Core.Application.Models.Routing;
using ApiGatewayKit.Gateway.Services.DownstreamHealth;
using Microsoft.Extensions.Logging;

namespace ApiGatewayKit.Gateway.Services;

public sealed class TargetNodeSelector : ITargetNodeSelector
{
    private readonly IGatewayTargetsProvider _targetsProvider;
    private readonly IRouteNodeOverrideProvider _overrideProvider;
    private readonly ISystemStatusRegistry _statusRegistry;
    private readonly ILogger<TargetNodeSelector> _logger;

    public TargetNodeSelector(
        IGatewayTargetsProvider targetsProvider, 
        IRouteNodeOverrideProvider overrideProvider,
        ISystemStatusRegistry statusRegistry,
        ILogger<TargetNodeSelector> logger)
    {
        _targetsProvider = targetsProvider;
        _overrideProvider = overrideProvider;
        _statusRegistry = statusRegistry;
        _logger = logger;
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

        // Önce enabled ve disabled olmayan node'ları filtrele
        List<GatewayTargetNode> enabledNodes = nodes
            .Where(n => n.Enabled && !disabled.Contains(n.Id))
            .ToList();

        if (enabledNodes.Count == 0)
        {
            string routeInfo = !string.IsNullOrWhiteSpace(routeKey) ? routeKey : upstreamPath ?? "unknown";
            throw new InvalidOperationException($"No effective enabled nodes for target '{target}' (route: {routeInfo}).");
        }

        // Sağlıklı node'ları filtrele
        List<GatewayTargetNode> healthyNodes = enabledNodes
            .Where(n => IsNodeHealthy(n.Id))
            .ToList();

        // Sağlıklı node varsa onlardan seç, yoksa enabled node'lardan seç (graceful degradation)
        if (healthyNodes.Count > 0)
        {
            return SelectWeightedRandom(healthyNodes);
        }
        
        // Sağlıklı node yoksa, enabled node'lardan seç ve uyarı logla
        _logger.LogWarning(
            "Sağlıklı node bulunamadı, enabled node'lardan seçiliyor. Target: {Target}, Route: {Route}, EnabledNodes: {EnabledNodes}",
            target,
            routeKey ?? upstreamPath ?? "unknown",
            string.Join(", ", enabledNodes.Select(n => n.Id)));

        return SelectWeightedRandom(enabledNodes);
    }

    /// <summary>
    /// Node'un sağlık durumunu kontrol eder.
    /// Eğer henüz kontrol yapılmadıysa (uygulama yeni başladıysa) sağlıklı kabul eder.
    /// </summary>
    private bool IsNodeHealthy(string nodeId)
    {
        var status = _statusRegistry.GetStatus(nodeId);
        
        // Henüz sağlık kontrolü yapılmadıysa (uygulama yeni başladı), sağlıklı kabul et
        if (status == null)
        {
            return true;
        }

        return status.IsOnline;
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


