using ApiGatewayKit.Core.Application.Interfaces.Routing;
using ApiGatewayKit.Gateway.Configuration;
using ApiGatewayKit.Gateway.Handlers;
using ApiGatewayKit.Gateway.Security;
using ApiGatewayKit.Gateway.Services;
using ApiGatewayKit.Gateway.Services.DownstreamHealth;
using ApiGatewayKit.Infrastructure.Logging.Extensions;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.FeatureManagement;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Serilog;
using System.IO.Compression;
using System.Net;
using System.Threading.RateLimiting; 
using ApiGatewayKit.Gateway.Middleware;
using ApiGatewayKit.Gateway.Security.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

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
builder.Services.AddSingleton<IModuleDefinitionsProvider, ModuleDefinitionsProvider>();

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

builder.Services
    .AddOcelot(builder.Configuration)
    .AddPolly()
    .AddDelegatingHandler<FeatureRoutingHandler>(global: true);

builder.Services.AddTransient<FeatureRoutingHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt => opt.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "ApiGatewayKit Gateway", Version = "v1" }));

// CORS for React Admin Panel - URL'ler config'den alınıyor
var adminUiDevUrls = builder.Configuration.GetSection("AdminUI:DevServerUrls").Get<string[]>() ?? Array.Empty<string>();
var adminUiProdUrl = builder.Configuration.GetValue<string>("AdminUI:ProductionUrl") ?? string.Empty;
var allCorsOrigins = adminUiDevUrls.Concat(new[] { adminUiProdUrl }).Where(u => !string.IsNullOrWhiteSpace(u)).ToArray();

builder.Services.AddCors(opt => opt.AddPolicy("AdminPanel", p => p
    .WithOrigins(allCorsOrigins)
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()));
builder.Services.AddHealthChecks();
builder.Services.AddResponseCompression(opt => { opt.EnableForHttps = true; opt.Providers.Add<BrotliCompressionProvider>(); opt.Providers.Add<GzipCompressionProvider>(); });
builder.Services.AddMemoryCache();
// 1. Concurrency Limiter (Global Fail-Fast)
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetConcurrencyLimiter(
            partitionKey: "GlobalConcurrency",
            factory: partition => new ConcurrencyLimiterOptions
            {
                PermitLimit = 1000, 
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
    
    options.OnRejected = async (context, token) => {
        context.HttpContext.Response.StatusCode = 503;
        await context.HttpContext.Response.WriteAsync("Service Unavailable (Concurrency Limit Reached)", token);
    };
});

// 2. Token/Quota Rate Limiting (AspNetCoreRateLimit)
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));

// Decide Store: Redis vs Memory
var useRedis = builder.Configuration.GetValue<bool>("RateLimitOptions:UseRedis");
if (useRedis)
{
    var redisConn = builder.Configuration.GetConnectionString("Redis");
    if (string.IsNullOrEmpty(redisConn)) throw new Exception("Redis connection string is missing.");
    
    // Register Redis
    var multiplexer = ConnectionMultiplexer.Connect(redisConn);
    builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
    // Use Distributed Stores (provided by AspNetCoreRateLimit.Redis or standard IDistributedCache adapters)
    // Note: AspNetCoreRateLimit recently supports Redis directly via extensions or via IDistributedCache
    // For this context, assuming we use the IDistributedCache implementation or specific Redis stores.
    // We will register DistributedCacheIpPolicyStore which uses IDistributedCache.
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConn);
    builder.Services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
    builder.Services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();
    builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>(); 
}
else
{
    builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
    builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
    builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>(); 
}

builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
// Register the Custom Token Resolver
builder.Services.AddSingleton<IClientResolveContributor, TokenClientIdResolver>();

// 3. Authentication (Required for Token Parsing)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Minimal config to allow parsing without strict signature check (assuming F5 handles it or keys provided later)
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = false, // Check expiration if needed
        SignatureValidator = (token, parameters) => new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token)
    };
});

var app = builder.Build();

// Modül tanımları provider'ını static accessor için initialize et
ModuleDefinitions.Initialize(app.Services.GetRequiredService<IModuleDefinitionsProvider>());

app.UseResponseCompression();
app.UseMiddleware<GatewayLoggingMiddleware>(); // Custom Structured Logging
app.UseRateLimiter(); // Concurrency
app.UseAuthentication();
app.UseIpRateLimiting(); // Token/Quota Throttling
app.UseLogging();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("AdminPanel");
app.UseAdminIpWhitelist();
app.UseAdminApiKeyAuth();
app.UseAdminRateLimit();

// Development: /admin -> React UI'a yönlendir, Production: statik dosyaları sun
var adminRedirectUrl = app.Configuration.GetValue<string>("AdminUI:RedirectUrl") ?? "http://localhost:3000/admin";
if (app.Environment.IsDevelopment())
{
    // /admin isteklerini React dev server'a yönlendir
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/admin"))
        {
            context.Response.Redirect(adminRedirectUrl);
            return;
        }
        await next();
    });
}
else
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

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
    var adminPanelUrl = app.Configuration.GetValue<string>("AdminUI:RedirectUrl") ?? "/admin";
    if (app.Environment.IsDevelopment())
    {
        Console.WriteLine($"  * Admin Panel    : {adminPanelUrl,-38} ");
    }
    else
    {
        Console.WriteLine("  * Admin Panel    : /admin                                 ");
    }
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
    
    services.AddHttpClient("NewTarget", c => { c.BaseAddress = new Uri(legacyUrl); c.Timeout = Timeout.InfiniteTimeSpan; })
           .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(120));

    services.AddHttpClient("NewTarget", c => { c.BaseAddress = new Uri(newUrl); c.Timeout = Timeout.InfiniteTimeSpan; })
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(120));
}

public partial class Program { }
