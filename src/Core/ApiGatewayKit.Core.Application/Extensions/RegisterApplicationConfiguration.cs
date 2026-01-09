using AutoMapper;
using ApiGatewayKit.Core.Application.Behaviors;
using ApiGatewayKit.Core.Application.Mapping;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ApiGatewayKit.Core.Application.Extensions;

/// <summary>
/// Application katmanÄ± yapÄ±landÄ±rma sÄ±nÄ±fÄ±
/// Plugin gibi tek satÄ±rda uygulamaya dahil edilebilir
/// </summary>
public static class RegisterApplicationConfiguration
{
    /// <summary>
    /// Application katmanÄ± servislerini register eder
    /// </summary>
    /// <example>
    /// services.RegisterApplication();
    /// </example>
    public static IServiceCollection RegisterApplication(this IServiceCollection services)
    {
        var assembly = typeof(RegisterApplicationConfiguration).Assembly;

        // AutoMapper - Application katmanÄ± mapping profili
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<ApplicationMappingProfile>();
        }, assembly);

        // FluentValidation - Application katmanÄ± validator'larÄ±
        services.AddValidatorsFromAssembly(assembly);

        // MediatR Pipeline Behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));

        return services;
    }
}


