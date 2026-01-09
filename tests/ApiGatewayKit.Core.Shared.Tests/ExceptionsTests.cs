using ApiGatewayKit.Core.Shared.Constants;
using ApiGatewayKit.Core.Shared.ErrorCodes;
using ApiGatewayKit.Core.Shared.Exceptions;
using FluentAssertions;
using Xunit;

namespace ApiGatewayKit.Core.Shared.Tests;

public class ExceptionsTests
{
    [Fact]
    public void BaseException_WithData_AddsAdditionalData()
    {
        var ex = new BusinessException("m");

        ex.WithData("k", 1);

        ex.AdditionalData.Should().ContainKey("k");
        ex.AdditionalData["k"].Should().Be(1);
    }

    [Fact]
    public void BusinessException_DefaultConstructor_SetsLayerAndErrorCode()
    {
        var ex = new BusinessException("msg");

        ex.Layer.Should().Be(LogConstants.Layers.Business);
        ex.ErrorCode.Should().Be("BUSINESS_ERROR");
        ex.Message.Should().Be("msg");
        ex.HttpStatusCode.Should().Be(400);
    }

    [Fact]
    public void BusinessException_WithExplicitErrorCode_SetsErrorCode()
    {
        var ex = new BusinessException("msg", "E1");

        ex.ErrorCode.Should().Be("E1");
    }

    [Fact]
    public void BusinessException_FromErrorCode_PopulatesFields()
    {
        ErrorCode errorCode = new("X-1", "user msg", "tech", 422, ErrorCategory.Business);

        var ex = new BusinessException(errorCode);

        ex.ErrorCodeInfo.Should().Be(errorCode);
        ex.ErrorCode.Should().Be("X-1");
        ex.UserFriendlyMessage.Should().Be("user msg");
        ex.HttpStatusCode.Should().Be(422);
    }

    [Fact]
    public void BusinessExceptionFactory_NotFound_AddsResourceIdWhenProvided()
    {
        BusinessException ex = BusinessExceptionFactory.NotFound("Thing", 99);

        ex.ErrorCodeInfo.Should().Be(CommonErrorCodes.EntityNotFound);
        ex.AdditionalData.Should().ContainKey("ResourceId");
        ex.AdditionalData["ResourceId"].Should().Be(99);
    }

    [Fact]
    public void BusinessExceptionFactory_Duplicate_AddsFieldNameAndValueWhenProvided()
    {
        BusinessException ex = BusinessExceptionFactory.Duplicate("Email", "a@b.com");

        ex.ErrorCodeInfo.Should().Be(CommonErrorCodes.DuplicateEntry);
        ex.AdditionalData.Should().ContainKey("FieldName");
        ex.AdditionalData.Should().ContainKey("Value");
        ex.AdditionalData["FieldName"].Should().Be("Email");
        ex.AdditionalData["Value"].Should().Be("a@b.com");
    }

    [Fact]
    public void NotFoundException_SetsProperties()
    {
        var ex = new NotFoundException("User", "1");

        ex.ResourceType.Should().Be("User");
        ex.ResourceId.Should().Be("1");
        ex.ErrorCode.Should().Be("NOT_FOUND");
        ex.Layer.Should().Be(LogConstants.Layers.Domain);
    }

    [Fact]
    public void ValidationException_FromDictionary_SetsErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Field"] = new[] { "Bad" }
        };

        var ex = new ValidationException(errors);

        ex.Errors.Should().ContainKey("Field");
        ex.Errors["Field"].Should().ContainSingle().Which.Should().Be("Bad");
    }

    [Fact]
    public void ValidationException_FromFieldAndError_SetsErrors()
    {
        var ex = new ValidationException("Field", "Bad");

        ex.Errors.Should().ContainKey("Field");
        ex.Errors["Field"].Should().ContainSingle().Which.Should().Be("Bad");
    }

    [Fact]
    public void ExternalServiceException_SetsServiceNameAndStatus()
    {
        var ex = new ExternalServiceException("Service", 500, "body");

        ex.ServiceName.Should().Be("Service");
        ex.StatusCode.Should().Be(500);
        ex.ResponseBody.Should().Be("body");
        ex.ErrorCode.Should().Be("EXTERNAL_SERVICE_ERROR");
        ex.Layer.Should().Be(LogConstants.Layers.Proxy);
    }

    [Fact]
    public void DatabaseException_SetsOptionalSqlFields()
    {
        var inner = new InvalidOperationException("x");
        var ex = new DatabaseException("m", "S", 7, inner);

        ex.SqlState.Should().Be("S");
        ex.SqlErrorNumber.Should().Be(7);
        ex.ErrorCode.Should().Be("DATABASE_ERROR");
        ex.Layer.Should().Be(LogConstants.Layers.Domain);
    }

    [Fact]
    public void UnauthorizedException_UsesDefaults()
    {
        var ex = new UnauthorizedException();

        ex.ErrorCode.Should().Be("UNAUTHORIZED");
        ex.Layer.Should().Be(LogConstants.Layers.ServerApi);
    }

    [Fact]
    public void ForbiddenException_UsesDefaults()
    {
        var ex = new ForbiddenException();

        ex.ErrorCode.Should().Be("FORBIDDEN");
        ex.Layer.Should().Be(LogConstants.Layers.ServerApi);
    }
}

