using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using ApiGatewayKit.Gateway.Configuration;
using ApiGatewayKit.Gateway.FeatureManagement;
using ApiGatewayKit.Gateway.Handlers;
using ApiGatewayKit.Gateway.Services;
using ApiGatewayKit.Infrastructure.Logging.Extensions;
using Ocelot.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace ApiGatewayKit.Gateway.Extensions;

/// <summary>
/// Gateway servislerinin DI kayÄ±tlarÄ±
/// Ocelot + FeatureManagement tabanlÄ± yeni yapÄ±
/// </summary>
public static class RegisterGatewayConfiguration
{
    /// <summary>
    /// Gateway altyapÄ±sÄ±nÄ± kaydeder
    /// </summary>
    public static IServiceCollection RegisterGateway(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Gateway options
        services.Configure<GatewayOptions>(configuration.GetSection(GatewayOptions.SectionName));
        var options = configuration.GetSection(GatewayOptions.SectionName).Get<GatewayOptions>() 
            ?? new GatewayOptions();

        // Logging
        services.RegisterLogging(configuration);

        // Route Configuration Service (ocelot.json'dan okur)
        services.AddSingleton<IRouteConfigurationService, RouteConfigurationService>();

        // HttpClient'larÄ± Polly ile yapÄ±landÄ±r
        ConfigureHttpClients(services, options);

        // Feature Management
        services.AddHttpContextAccessor();
        services.AddFeatureManagement()
            .AddFeatureFilter<PercentageFilter>()
            .AddFeatureFilter<TargetingFilter>()
            .AddFeatureFilter<TimeWindowFilter>();

        // Targeting context accessor (user-based feature flags iÃ§in)
        services.AddSingleton<ITargetingContextAccessor, HttpContextTargetingContextAccessor>();

        // FeatureRoutingHandler (Ocelot DelegatingHandler)
        services.AddTransient<FeatureRoutingHandler>();

        // Ocelot with DelegatingHandler
        services.AddOcelot(configuration)
            .AddDelegatingHandler<FeatureRoutingHandler>(global: true);

        return services;
    }

    private static void ConfigureHttpClients(IServiceCollection services, GatewayOptions options)
    {
        // Legacy system client
        services.AddHttpClient("LegacyTarget", client =>
        {
            if (!string.IsNullOrEmpty(options.LegacyBaseUrl))
            {
                client.BaseAddress = new Uri(options.LegacyBaseUrl);
            }
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        })
        .ConfigurePrimaryHttpMessageHandler(() => CreateHttpHandler())
        .AddPolicyHandler(GetRetryPolicy(options))
        .AddPolicyHandler(GetCircuitBreakerPolicy(options));

        // New system client
        services.AddHttpClient("NewTarget", client =>
        {
            if (!string.IsNullOrEmpty(options.NewBaseUrl))
            {
                client.BaseAddress = new Uri(options.NewBaseUrl);
            }
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        })
        .ConfigurePrimaryHttpMessageHandler(() => CreateHttpHandler())
        .AddPolicyHandler(GetRetryPolicy(options))
        .AddPolicyHandler(GetCircuitBreakerPolicy(options));
    }

    private static HttpClientHandler CreateHttpHandler()
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(GatewayOptions options)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                options.RetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(GatewayOptions options)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                options.CircuitBreakerThreshold,
                TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds));
    }
}

