using System.Text.Json;
using ApiGatewayKit.Infrastructure.Caching.Options;
using ApiGatewayKit.Infrastructure.Caching.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ApiGatewayKit.Infrastructure.Caching.Sync;

public class CacheSyncBackgroundService : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceProvider _services;
    private readonly CacheProviderOptions _options;
    private readonly ILogger<CacheSyncBackgroundService> _logger;

    public CacheSyncBackgroundService(
        IConnectionMultiplexer redis,
        IServiceProvider services,
        IOptions<CacheProviderOptions> options,
        ILogger<CacheSyncBackgroundService> logger)
    {
        _redis = redis;
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ISubscriber? subscriber = null;
        
        try
        {
            subscriber = _redis.GetSubscriber();
            
            await subscriber.SubscribeAsync(RedisChannel.Literal(_options.SyncChannel), async (channel, message) =>
            {
                try
                {
                    var msg = JsonSerializer.Deserialize<CacheSyncMessage>(message.ToString());
                    if (msg == null || msg.Source == _options.AppId) return;

                    using var scope = _services.CreateScope();
                    var provider = scope.ServiceProvider.GetRequiredService<RedisCacheProvider>();

                    if (msg.Action == CacheSyncAction.Set && msg.Value != null)
                    {
                        await provider.SetLocalAsync(msg.Key, msg.Value);
                        _logger.LogDebug("Cache synced SET: {Key}", msg.Key);
                    }
                    else if (msg.Action == CacheSyncAction.Remove)
                    {
                        await provider.RemoveLocalAsync(msg.Key);
                        _logger.LogDebug("Cache synced REMOVE: {Key}", msg.Key);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cache sync message handling failed");
                }
            });

            _logger.LogInformation("Cache sync subscriber started on channel: {Channel}", _options.SyncChannel);
            
            // Uygulama kapanana kadar bekle
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Uygulama kapaniyor - bu beklenen bir durum
            _logger.LogInformation("Cache sync subscriber stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache sync subscriber encountered an error");
        }
        finally
        {
            // Subscriber'i temizle
            if (subscriber != null)
            {
                try
                {
                    await subscriber.UnsubscribeAsync(RedisChannel.Literal(_options.SyncChannel));
                    _logger.LogInformation("Cache sync subscriber unsubscribed from channel: {Channel}", _options.SyncChannel);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to unsubscribe from cache sync channel");
                }
            }
        }
    }
}


