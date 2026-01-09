namespace ApiGatewayKit.Gateway.Configuration;

public sealed class GatewayTargetsOptions
{
    public const string SectionName = "GatewayTargets";

    public GatewayTargetGroupOptions Legacy { get; set; } = new();
    public GatewayTargetGroupOptions New { get; set; } = new();
}

public sealed class GatewayTargetGroupOptions
{
    public List<GatewayTargetNodeOptions> Nodes { get; set; } = new();
}

public sealed class GatewayTargetNodeOptions
{
    public string Id { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int Weight { get; set; } = 100;
}


