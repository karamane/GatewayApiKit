using System.Text.Json;
using ApiGatewayKit.Core.Shared.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace ApiGatewayKit.Core.Shared.Tests;

public class SwaggerConfigurationTests
{
    [Fact]
    public void RegisterSwagger_WhenDisabled_DoesNotRegisterSwaggerProvider()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Swagger:Enabled"] = "false"
            })
            .Build();

        services.RegisterSwagger(configuration);

        services.Should().NotContain(d => d.ServiceType == typeof(ISwaggerProvider));
    }

    [Fact]
    public void RegisterSwagger_WhenEnabled_RegistersSwaggerProvider()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Swagger:Enabled"] = "true",
                ["Swagger:EnableBearerAuth"] = "true",
                ["Swagger:IncludeXmlComments"] = "false"
            })
            .Build();

        services.RegisterSwagger(configuration);

        services.Should().Contain(d => d.ServiceType == typeof(ISwaggerProvider));
    }

    [Fact]
    public void UseSwaggerWithConfig_WhenDisabled_ReturnsSameApplicationBuilder()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        IApplicationBuilder app = new ApplicationBuilder(services);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Swagger:Enabled"] = "false"
            })
            .Build();

        IHostEnvironment environment = new FakeHostEnvironment("Development");

        IApplicationBuilder result = app.UseSwaggerWithConfig(configuration, environment);

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void AuthorizeCheckOperationFilter_WhenAllowAnonymous_DoesNotAddSecurity()
    {
        var filter = new AuthorizeCheckOperationFilter();
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
        OperationFilterContext context = CreateContextForMethod(typeof(TestController).GetMethod(nameof(TestController.Anonymous))!);

        filter.Apply(operation, context);

        operation.Security.Should().BeEmpty();
        operation.Responses.Should().NotContainKey("401");
        operation.Responses.Should().NotContainKey("403");
    }

    [Fact]
    public void AuthorizeCheckOperationFilter_WhenAuthorize_AddsSecurityAndResponses()
    {
        var filter = new AuthorizeCheckOperationFilter();
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
        OperationFilterContext context = CreateContextForMethod(typeof(TestController).GetMethod(nameof(TestController.Secured))!);

        filter.Apply(operation, context);

        operation.Responses.Should().ContainKey("401");
        operation.Responses.Should().ContainKey("403");
        operation.Security.Should().NotBeNull();
        operation.Security!.Should().NotBeEmpty();
    }

    private static OperationFilterContext CreateContextForMethod(System.Reflection.MethodInfo methodInfo)
    {
        var apiDescription = new ApiDescription();
        var schemaRepository = new SchemaRepository();
        var schemaGenerator = new SchemaGenerator(
            new SchemaGeneratorOptions(),
            new JsonSerializerDataContractResolver(new JsonSerializerOptions()));

        return new OperationFilterContext(apiDescription, schemaGenerator, schemaRepository, methodInfo);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
            ApplicationName = "Test";
            ContentRootPath = AppContext.BaseDirectory;
            ContentRootFileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(ContentRootPath);
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
    }

    private sealed class TestController
    {
        [AllowAnonymous]
        public void Anonymous()
        {
        }

        [Authorize]
        public void Secured()
        {
        }
    }
}

