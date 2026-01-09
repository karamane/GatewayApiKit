using ApiGatewayKit.Core.Application.Interfaces.Caching;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiGatewayKit.Infrastructure.Caching.Extensions;

public static class UseCachePreloadExtension
{
    public static IApplicationBuilder UseCachePreload(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var resetService = scope.ServiceProvider.GetRequiredService<ICacheResetService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ICacheResetService>>();

        try
        {
            resetService.PreloadAsync().GetAwaiter().GetResult();
            logger.LogInformation("Cache preload completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cache preload failed");
        }

        return app;
    }
}








