using System.Net;
using System.Text.Json;
using ApiGatewayKit.Core.Application.Interfaces.Routing;
using ApiGatewayKit.Core.Application.Models.Routing;
using ApiGatewayKit.Gateway.Configuration;
using Microsoft.Extensions.Options;

namespace ApiGatewayKit.Gateway.Services;

public sealed class GatewayTargetsStore : IGatewayTargetsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly IOptionsMonitor<GatewayTargetsOptions> _optionsMonitor;
    private readonly IWebHostEnvironment _environment;
    private readonly object _fileLock = new();

    public GatewayTargetsStore(IOptionsMonitor<GatewayTargetsOptions> optionsMonitor, IWebHostEnvironment environment)
    {
        _optionsMonitor = optionsMonitor;
        _environment = environment;
    }

    public GatewayTargetsSnapshot GetSnapshot()
    {
        string filePath = GetTargetsFilePath();
        
        GatewayTargetsOptions options = new();
        try
        {
            string json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("GatewayTargets", out var gtEl))
            {
                // Better manual mapping to ensure case-insensitivity and defaults
                options.Legacy = MapGroup(gtEl, "Legacy");
                options.New = MapGroup(gtEl, "New");
            }
        }
        catch (Exception)
        {
            options = _optionsMonitor.CurrentValue;
        }

        bool allowDevelopmentHosts = _environment.IsDevelopment();

        return new GatewayTargetsSnapshot
        {
            LegacyNodes = (options.Legacy?.Nodes ?? Enumerable.Empty<GatewayTargetNodeOptions>())
                .Select(n => MapNodeOrThrow(n, allowDevelopmentHosts))
                .ToList(),
            NewNodes = (options.New?.Nodes ?? Enumerable.Empty<GatewayTargetNodeOptions>())
                .Select(n => MapNodeOrThrow(n, allowDevelopmentHosts))
                .ToList()
        };
    }

    private static GatewayTargetGroupOptions MapGroup(JsonElement parent, string name)
    {
        var group = new GatewayTargetGroupOptions();
        if (parent.TryGetProperty(name, out var groupEl) || parent.TryGetProperty(name.ToLowerInvariant(), out groupEl))
        {
            if (groupEl.TryGetProperty("Nodes", out var nodesEl) || groupEl.TryGetProperty("nodes", out nodesEl))
            {
                foreach (var n in nodesEl.EnumerateArray())
                {
                    var node = new GatewayTargetNodeOptions
                    {
                        Id = GetString(n, "Id"),
                        BaseUrl = GetString(n, "BaseUrl"),
                        Weight = GetInt(n, "Weight", 100),
                        Enabled = GetBool(n, "Enabled", true)
                    };
                    group.Nodes.Add(node);
                }
            }
        }
        return group;
    }

    private static string GetString(JsonElement el, string name) =>
        (el.TryGetProperty(name, out var p) || el.TryGetProperty(name.ToLowerInvariant(), out p)) ? p.GetString() ?? "" : "";

    private static int GetInt(JsonElement el, string name, int def) =>
        (el.TryGetProperty(name, out var p) || el.TryGetProperty(name.ToLowerInvariant(), out p)) ? p.GetInt32() : def;

    private static bool GetBool(JsonElement el, string name, bool def) =>
        (el.TryGetProperty(name, out var p) || el.TryGetProperty(name.ToLowerInvariant(), out p)) ? p.GetBoolean() : def;

    public Task SetNodeEnabledAsync(TargetSystem target, string nodeId, bool enabled, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("nodeId is required.", nameof(nodeId));
        }

        string filePath = GetTargetsFilePath();

        lock (_fileLock)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string json = File.ReadAllText(filePath);
            using JsonDocument document = JsonDocument.Parse(json);

            using var outputStream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(outputStream, new JsonWriterOptions { Indented = true }))
            {
                WriteUpdatedTargetsJson(document.RootElement, target, nodeId.Trim(), enabled, writer);
            }

            File.WriteAllBytes(filePath, outputStream.ToArray());
        }

        return Task.CompletedTask;
    }

    private string GetTargetsFilePath()
    {
        string filePath = Path.Combine(_environment.ContentRootPath, "gateway-targets.json");
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("gateway-targets.json not found in content root.", filePath);
        }

        return filePath;
    }

    private static GatewayTargetNode MapNodeOrThrow(GatewayTargetNodeOptions node, bool allowDevelopmentHosts)
    {
        if (string.IsNullOrWhiteSpace(node.Id))
        {
            throw new InvalidOperationException("GatewayTargets node Id is required.");
        }

        if (node.Weight < 1)
        {
            throw new InvalidOperationException($"GatewayTargets node '{node.Id}' Weight must be >= 1.");
        }

        if (!Uri.TryCreate(node.BaseUrl, UriKind.Absolute, out Uri? baseUri))
        {
            throw new InvalidOperationException($"GatewayTargets node '{node.Id}' BaseUrl is invalid.");
        }

        if (!string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            if (!allowDevelopmentHosts || !string.Equals(baseUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"GatewayTargets node '{node.Id}' BaseUrl must use https scheme.");
            }
        }

        if (!string.IsNullOrEmpty(baseUri.UserInfo))
        {
            throw new InvalidOperationException($"GatewayTargets node '{node.Id}' BaseUrl must not contain user info.");
        }

        // Reject IP-literals and localhost to reduce SSRF blast radius.
        // Allow in Development for local testing.
        if (!allowDevelopmentHosts)
        {
            if (string.Equals(baseUri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(baseUri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(baseUri.Host, "::1", StringComparison.OrdinalIgnoreCase) ||
                IPAddress.TryParse(baseUri.Host, out _))
            {
                throw new InvalidOperationException($"GatewayTargets node '{node.Id}' BaseUrl host is not allowed.");
            }
        }

        return new GatewayTargetNode
        {
            Id = node.Id.Trim(),
            BaseUrl = baseUri,
            Enabled = node.Enabled,
            Weight = node.Weight
        };
    }

    private static void WriteUpdatedTargetsJson(
        JsonElement root,
        TargetSystem target,
        string nodeId,
        bool enabled,
        Utf8JsonWriter writer)
    {
        // We preserve unknown fields by rewriting from JsonElement with a targeted change.
        // Expected shape:
        // { "GatewayTargets": { "Legacy": { "Nodes": [ ... ] }, "New": { "Nodes": [ ... ] } } }

        writer.WriteStartObject();

        foreach (JsonProperty prop in root.EnumerateObject())
        {
            if (!prop.NameEquals("GatewayTargets"))
            {
                prop.WriteTo(writer);
                continue;
            }

            writer.WritePropertyName("GatewayTargets");
            writer.WriteStartObject();

            foreach (JsonProperty tg in prop.Value.EnumerateObject())
            {
                bool isTargetGroup = (target == TargetSystem.Legacy && tg.NameEquals("Legacy")) ||
                                     (target == TargetSystem.New && tg.NameEquals("New"));

                if (!isTargetGroup)
                {
                    tg.WriteTo(writer);
                    continue;
                }

                writer.WritePropertyName(tg.Name);
                writer.WriteStartObject();

                foreach (JsonProperty groupProp in tg.Value.EnumerateObject())
                {
                    if (!groupProp.NameEquals("Nodes") || groupProp.Value.ValueKind != JsonValueKind.Array)
                    {
                        groupProp.WriteTo(writer);
                        continue;
                    }

                    writer.WritePropertyName("Nodes");
                    writer.WriteStartArray();

                    bool found = false;

                    foreach (JsonElement node in groupProp.Value.EnumerateArray())
                    {
                        if (node.ValueKind != JsonValueKind.Object)
                        {
                            node.WriteTo(writer);
                            continue;
                        }

                        string currentId = node.TryGetProperty("Id", out JsonElement idEl)
                            ? (idEl.GetString() ?? string.Empty)
                            : string.Empty;

                        if (!string.Equals(currentId, nodeId, StringComparison.OrdinalIgnoreCase))
                        {
                            node.WriteTo(writer);
                            continue;
                        }

                        found = true;

                        writer.WriteStartObject();
                        foreach (JsonProperty nodeProp in node.EnumerateObject())
                        {
                            if (nodeProp.NameEquals("Enabled"))
                            {
                                writer.WriteBoolean("Enabled", enabled);
                            }
                            else
                            {
                                nodeProp.WriteTo(writer);
                            }
                        }
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();

                    if (!found)
                    {
                        throw new InvalidOperationException($"Node '{nodeId}' was not found in target registry.");
                    }
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}


