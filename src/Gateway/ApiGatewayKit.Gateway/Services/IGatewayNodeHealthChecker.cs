using System.Net;
using Microsoft.Extensions.Logging;

namespace ApiGatewayKit.Gateway.Services;

public interface IGatewayNodeHealthChecker
{
    Task<NodeHealthResult> CheckAsync(Uri baseUrl, CancellationToken cancellationToken);
}

public sealed record NodeHealthResult(
    bool Ready,
    int? StatusCode,
    string Hint);


