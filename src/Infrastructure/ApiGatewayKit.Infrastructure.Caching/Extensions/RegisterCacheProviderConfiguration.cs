using ApiGatewayKit.Core.Application.Interfaces.Caching;
using ApiGatewayKit.Infrastructure.Caching.Options;
using ApiGatewayKit.Infrastructure.Caching.Providers;
using ApiGatewayKit.Infrastructure.Caching.Services;
using ApiGatewayKit.Infrastructure.Caching.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ApiGatewayKit.Infrastructure.Caching.Extensions;

public static class RegisterCacheProviderConfiguration
{
    public static IServiceCollection RegisterCacheProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(CacheProviderOptions.SectionName)
            .Get<CacheProviderOptions>() ?? new CacheProviderOptions();

        services.Configure<CacheProviderOptions>(
            configuration.GetSection(CacheProviderOptions.SectionName));

        if (options.Provider == CacheProviderType.Redis)
        {
            ConfigureRedis(services, options);
        }
        else
        {
            ConfigureMemory(services, options);
        }

        services.AddScoped<ICacheResetService, CacheResetService>();

        return services;
    }

    private static void ConfigureRedis(IServiceCollection services, CacheProviderOptions options)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var config = ConfigurationOptions.Parse(options.ConnectionString);
            config.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(config);
        });

        services.AddScoped<ICacheSyncService, RedisCacheSyncService>();
        services.AddScoped<ICacheProvider, RedisCacheProvider>();
        services.AddScoped<RedisCacheProvider>();
        services.AddHostedService<CacheSyncBackgroundService>();
    }

    private static void ConfigureMemory(IServiceCollection services, CacheProviderOptions options)
    {
        services.AddMemoryCache(opt =>
        {
            opt.SizeLimit = options.MemorySizeLimitMb * 1024 * 1024;
        });

        // Named HttpClient for cache sync - PeerApiUrl ile yapÄ±landÄ±rÄ±lmÄ±ÅŸ
        if (!string.IsNullOrEmpty(options.PeerApiUrl))
        {
            services.AddHttpClient("CacheSync", client =>
            {
                client.BaseAddress = new Uri(options.PeerApiUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            });
        }
        else
        {
            services.AddHttpClient("CacheSync");
        }

        services.AddScoped<ICacheSyncService, HttpCacheSyncService>();
        services.AddScoped<ICacheProvider, MemoryCacheProvider>();
        services.AddScoped<MemoryCacheProvider>();
    }
}



