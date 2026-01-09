using ApiGatewayKit.Core.Application.Interfaces.Caching;
using ApiGatewayKit.Infrastructure.Caching.Options;
using ApiGatewayKit.Infrastructure.Caching.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ApiGatewayKit.Infrastructure.Caching.Extensions;

/// <summary>
/// Caching servisleri yapÄ±landÄ±rma sÄ±nÄ±fÄ±
/// Redis ve MemoryCache arasÄ±nda switch yapÄ±labilir
/// Plugin gibi tek satÄ±rda uygulamaya dahil edilebilir
/// </summary>
public static class RegisterCachingConfiguration
{
    /// <summary>
    /// Caching servislerini DI container'a register eder
    /// Provider tipine gÃ¶re Redis veya MemoryCache yapÄ±landÄ±rÄ±r
    /// </summary>
    /// <example>
    /// services.RegisterCaching(configuration);
    /// </example>
    public static IServiceCollection RegisterCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>()
            ?? new CacheOptions();

        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        // Provider'a gÃ¶re yapÄ±landÄ±r
        switch (cacheOptions.Provider)
        {
            case CacheProvider.Redis:
                ConfigureRedis(services, cacheOptions);
                break;

            case CacheProvider.Memory:
                ConfigureMemory(services, cacheOptions);
                break;

            case CacheProvider.Hybrid:
                ConfigureHybrid(services, cacheOptions);
                break;

            default:
                ConfigureMemory(services, cacheOptions);
                break;
        }

        // Parameter cache service (her provider iÃ§in ortak)
        services.AddScoped<IParameterCacheService, ParameterCacheService>();

        return services;
    }

    /// <summary>
    /// Redis cache yapÄ±landÄ±rmasÄ± (distributed)
    /// </summary>
    private static void ConfigureRedis(IServiceCollection services, CacheOptions options)
    {
        // Redis connection multiplexer
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
            configOptions.AbortOnConnectFail = false;
            configOptions.ConnectRetry = options.RetryCount;
            configOptions.ConnectTimeout = options.ConnectTimeoutMs;
            configOptions.SyncTimeout = options.SyncTimeoutMs;
            configOptions.AsyncTimeout = options.SyncTimeoutMs;

            return ConnectionMultiplexer.Connect(configOptions);
        });

        // Distributed cache (Redis)
        services.AddStackExchangeRedisCache(redisOptions =>
        {
            redisOptions.Configuration = options.ConnectionString;
            redisOptions.InstanceName = options.InstanceName;
        });

        // Redis cache service
        services.AddScoped<ICacheService, RedisCacheService>();
    }

    /// <summary>
    /// Memory cache yapÄ±landÄ±rmasÄ± (in-process)
    /// </summary>
    private static void ConfigureMemory(IServiceCollection services, CacheOptions options)
    {
        // Memory cache
        services.AddMemoryCache(memoryOptions =>
        {
            memoryOptions.SizeLimit = options.MemoryCacheSizeLimitMb * 1024 * 1024; // MB -> bytes
            memoryOptions.CompactionPercentage = options.CompactionPercentage;
        });

        // Distributed cache (Memory-based)
        services.AddDistributedMemoryCache();

        // Memory cache service
        services.AddScoped<ICacheService, MemoryCacheService>();
    }

    /// <summary>
    /// Hybrid cache yapÄ±landÄ±rmasÄ± (L1: Memory, L2: Redis)
    /// </summary>
    private static void ConfigureHybrid(IServiceCollection services, CacheOptions options)
    {
        // L1: Memory cache
        services.AddMemoryCache(memoryOptions =>
        {
            memoryOptions.SizeLimit = options.MemoryCacheSizeLimitMb * 1024 * 1024;
            memoryOptions.CompactionPercentage = options.CompactionPercentage;
        });

        // L2: Redis connection
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
            configOptions.AbortOnConnectFail = false;
            configOptions.ConnectRetry = options.RetryCount;
            configOptions.ConnectTimeout = options.ConnectTimeoutMs;
            configOptions.SyncTimeout = options.SyncTimeoutMs;

            return ConnectionMultiplexer.Connect(configOptions);
        });

        // L2: Distributed cache (Redis)
        services.AddStackExchangeRedisCache(redisOptions =>
        {
            redisOptions.Configuration = options.ConnectionString;
            redisOptions.InstanceName = options.InstanceName;
        });

        // Hybrid cache service
        services.AddScoped<ICacheService, HybridCacheService>();
    }
}


