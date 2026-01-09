using System.IO.Compression;
using System.Net;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.FeatureManagement;
using ApiGatewayKit.Gateway.Configuration;
using ApiGatewayKit.Gateway.Handlers;
using ApiGatewayKit.Gateway.Security;
using ApiGatewayKit.Gateway.Services;
using ApiGatewayKit.Gateway.Services.DownstreamHealth;
using ApiGatewayKit.Infrastructure.Logging.Extensions;
using ApiGatewayKit.Core.Application.Interfaces.Routing;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Serilog;

ThreadPool.SetMinThreads(100, 100);

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("gateway-targets.json", optional: false, reloadOnChange: true);

Log.Logger = new LoggerConfiguration()
    .ConfigureSerilog(builder.Configuration, "ApiGatewayKit.Gateway")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<GatewayOptions>(builder.Configuration.GetSection("Gateway"));
builder.Services.Configure<GatewayTargetsOptions>(builder.Configuration.GetSection(GatewayTargetsOptions.SectionName));
builder.Services.AddSingleton<IGatewayTargetsStore, GatewayTargetsStore>();
builder.Services.AddSingleton<IGatewayTargetsProvider>(sp => sp.GetRequiredService<IGatewayTargetsStore>());
builder.Services.AddSingleton<IRouteNodeOverrideProvider, OcelotRouteNodeOverrideProvider>();
builder.Services.AddSingleton<ITargetNodeSelector, TargetNodeSelector>();
builder.Services.AddSingleton<IGatewayNodeHealthChecker, GatewayNodeHealthChecker>();

// Downstream Health Monitoring
builder.Services.Configure<DownstreamWatcherOptions>(builder.Configuration.GetSection("DownstreamWatcher"));
builder.Services.AddSingleton<ISystemStatusRegistry, SystemStatusRegistry>();
builder.Services.AddSingleton<IDownstreamHealthChecker, DownstreamHealthChecker>();
builder.Services.AddHostedService<DownstreamWatcherService>();

// HttpClient for downstream health checks (tolerates SSL errors for legacy systems)
builder.Services.AddHttpClient("DownstreamHealthCheck")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });

builder.Services.Configure<AdminSecurityOptions>(builder.Configuration.GetSection(AdminSecurityOptions.SectionName));
builder.Services.AddScoped<AdminAuditActionFilter>();
builder.Services.AddScoped<ProductionRestrictionFilter>();
builder.Services.RegisterLogging(builder.Configuration);
builder.Services.AddFeatureManagement();
builder.Services.AddSingleton<IRouteConfigurationService, RouteConfigurationService>();

ConfigureHttpClients(builder.Services, builder.Configuration);

builder.Services.AddOcelot(builder.Configuration).AddDelegatingHandler<FeatureRoutingHandler>(global: true);
builder.Services.AddTransient<FeatureRoutingHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt => opt.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "ApiGatewayKit Gateway", Version = "v1" }));
builder.Services.AddCors(opt => opt.AddPolicy("AdminPanel", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddHealthChecks();
builder.Services.AddResponseCompression(opt => { opt.EnableForHttps = true; opt.Providers.Add<BrotliCompressionProvider>(); opt.Providers.Add<GzipCompressionProvider>(); });
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

var app = builder.Build();

app.UseResponseCompression();
app.UseIpRateLimiting();
app.UseLogging();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("AdminPanel");
app.UseAdminIpWhitelist();
app.UseAdminApiKeyAuth();
app.UseAdminRateLimit();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseEndpoints(endpoints => {
    endpoints.MapControllers();
    endpoints.MapHealthChecks("/health");
});

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api/gateway") &&
               !context.Request.Path.StartsWithSegments("/swagger") &&
               !context.Request.Path.StartsWithSegments("/admin") &&
               !context.Request.Path.StartsWithSegments("/health"),
    appBuilder => { appBuilder.UseOcelot().Wait(); });

try {
    app.Lifetime.ApplicationStarted.Register(() => PrintStartupBanner(app));
    app.Run();
} catch (Exception ex) { Log.Fatal(ex, "Terminated unexpectedly"); }
finally { Log.CloseAndFlush(); }

static void PrintStartupBanner(WebApplication app) {
    var server = app.Services.GetService<IServer>();
    var addresses = server?.Features.Get<IServerAddressesFeature>()?.Addresses ?? new List<string>();
    var urls = addresses.Count > 0 ? string.Join(", ", addresses) : "N/A";
    var appName = app.Configuration["Logging:ApplicationName"] ?? "ApiGatewayKit.Gateway";
    
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("");
    Console.WriteLine("                                                            ");
    Console.WriteLine($"  {appName,-56}  ");
    Console.WriteLine("  Standalone API Gateway Service                            ");
    Console.WriteLine("");
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("  [OK] Status      : Running                                ");
    Console.ResetColor();
    Console.WriteLine($"  * Environment    : {app.Environment.EnvironmentName,-38} ");
    Console.WriteLine($"  * Bound URLs     : {urls,-38} ");
    if (app.Environment.IsDevelopment()) {
        var swaggerUrl = (addresses.FirstOrDefault(u => u.StartsWith("http")) ?? "http://localhost:53000").TrimEnd('/');
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  * Swagger        : {swaggerUrl + "/swagger",-38} ");
        Console.ResetColor();
    }
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine("  * Admin Panel    : /admin                                 ");
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("                                                            ");
    Console.WriteLine("");
    Console.ResetColor();
    Console.WriteLine();
}

static void ConfigureHttpClients(IServiceCollection services, IConfiguration configuration) {
    var gatewaySection = configuration.GetSection("Gateway");
    var legacyUrl = gatewaySection["LegacyBaseUrl"] ?? "http://localhost:39414";
    var newUrl = gatewaySection["NewBaseUrl"] ?? "http://localhost:52200";
    
    services.AddHttpClient("LegacyTarget", c => { c.BaseAddress = new Uri(legacyUrl); c.Timeout = Timeout.InfiniteTimeSpan; })
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(120));
            
    services.AddHttpClient("NewTarget", c => { c.BaseAddress = new Uri(newUrl); c.Timeout = Timeout.InfiniteTimeSpan; })
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(120));
}

public partial class Program { }
