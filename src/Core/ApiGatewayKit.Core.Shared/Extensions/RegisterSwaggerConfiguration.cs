using ApiGatewayKit.Core.Shared.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiGatewayKit.Core.Shared.Extensions;

/// <summary>
/// Swagger yapÄ±landÄ±rma sÄ±nÄ±fÄ±
/// Parametrik olarak kontrol edilebilir
/// </summary>
public static class RegisterSwaggerConfiguration
{
    /// <summary>
    /// Swagger servislerini register eder
    /// </summary>
    public static IServiceCollection RegisterSwagger(
        this IServiceCollection services,
        IConfiguration configuration,
        string? defaultTitle = null,
        string? defaultDescription = null)
    {
        var options = configuration.GetSection(SwaggerOptions.SectionName).Get<SwaggerOptions>()
            ?? new SwaggerOptions();

        if (!options.Enabled)
            return services;

        services.Configure<SwaggerOptions>(configuration.GetSection(SwaggerOptions.SectionName));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(options.Version, new OpenApiInfo
            {
                Title = defaultTitle ?? options.Title,
                Version = options.Version,
                Description = defaultDescription ?? options.Description,
                Contact = options.Contact != null ? new OpenApiContact
                {
                    Name = options.Contact.Name,
                    Email = options.Contact.Email,
                    Url = !string.IsNullOrEmpty(options.Contact.Url) ? new Uri(options.Contact.Url) : null
                } : null,
                License = options.License != null ? new OpenApiLicense
                {
                    Name = options.License.Name,
                    Url = !string.IsNullOrEmpty(options.License.Url) ? new Uri(options.License.Url) : null
                } : null
            });

            // Bearer Token Authentication
            if (options.EnableBearerAuth)
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                // OperationFilter ile [AllowAnonymous] endpoint'leri hariÃ§ tutar
                c.OperationFilter<AuthorizeCheckOperationFilter>();
            }

            // API Key Authentication
            if (options.EnableApiKeyAuth)
            {
                c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Description = "API Key Authentication. Example: \"X-API-Key: {key}\"",
                    Name = "X-API-Key",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                // Note: API Key iÃ§in de OperationFilter kullanÄ±labilir
            }

            // XML Comments
            if (options.IncludeXmlComments)
            {
                var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
                foreach (var xmlFile in xmlFiles)
                {
                    try
                    {
                        c.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
                    }
                    catch
                    {
                        // XML dosyasÄ± okunamazsa devam et
                    }
                }
            }

            // Custom schema IDs
            c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
        });

        return services;
    }

    /// <summary>
    /// Swagger middleware'ini pipeline'a ekler
    /// </summary>
    public static IApplicationBuilder UseSwaggerWithConfig(
        this IApplicationBuilder app,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = configuration.GetSection(SwaggerOptions.SectionName).Get<SwaggerOptions>()
            ?? new SwaggerOptions();

        if (!options.Enabled)
            return app;

        // Ortam kontrolÃ¼
        if (options.AllowedEnvironments.Length > 0)
        {
            var isAllowed = options.AllowedEnvironments
                .Any(e => e.Equals(environment.EnvironmentName, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
                return app;
        }

        // Swagger JSON
        app.UseSwagger(c =>
        {
            c.RouteTemplate = $"{options.RoutePrefix}/{{documentName}}/swagger.json";
        });

        // Swagger UI
        if (options.EnableUI)
        {
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/{options.RoutePrefix}/{options.Version}/swagger.json", $"{options.Title} {options.Version}");
                c.RoutePrefix = options.RoutePrefix;

                // DocExpansion
                c.DocExpansion(options.DocExpansion switch
                {
                    "none" => Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None,
                    "full" => Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.Full,
                    _ => Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List
                });

                // Dark theme
                if (options.Theme.Equals("dark", StringComparison.OrdinalIgnoreCase))
                {
                    c.InjectStylesheet("/swagger-ui/dark-theme.css");
                }

                c.DisplayRequestDuration();
                c.EnableDeepLinking();
                c.EnableFilter();
                c.ShowExtensions();
            });
        }

        return app;
    }
}

/// <summary>
/// Swagger iÃ§in [Authorize] ve [AllowAnonymous] attribute'larÄ±nÄ± kontrol eden filter
/// [AllowAnonymous] olan endpoint'lerde kilit ikonu gÃ¶stermez
/// </summary>
public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Controller ve action Ã¼zerindeki attribute'larÄ± al
        var hasAllowAnonymous = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>()
            .Any();

        // Controller sÄ±nÄ±fÄ±nda da kontrol et
        if (!hasAllowAnonymous && context.MethodInfo.DeclaringType != null)
        {
            hasAllowAnonymous = context.MethodInfo.DeclaringType
                .GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>()
                .Any();
        }

        // [AllowAnonymous] varsa security requirement ekleme
        if (hasAllowAnonymous)
        {
            return;
        }

        // [Authorize] attribute'u var mÄ± kontrol et
        var hasAuthorize = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .Any();

        // Controller sÄ±nÄ±fÄ±nda da kontrol et
        if (!hasAuthorize && context.MethodInfo.DeclaringType != null)
        {
            hasAuthorize = context.MethodInfo.DeclaringType
                .GetCustomAttributes(true)
                .OfType<AuthorizeAttribute>()
                .Any();
        }

        // [Authorize] varsa security requirement ekle
        if (hasAuthorize)
        {
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                }
            };
        }
    }
}


