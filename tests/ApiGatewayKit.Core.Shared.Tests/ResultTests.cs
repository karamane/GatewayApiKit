using ApiGatewayKit.Core.Shared.Results;
using FluentAssertions;
using Xunit;

namespace ApiGatewayKit.Core.Shared.Tests;

public class ResultTests
{
    [Fact]
    public void GenericSuccess_SetsSuccessAndData()
    {
        Result<int> result = Result<int>.Success(123, "ok");

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Data.Should().Be(123);
        result.Message.Should().Be("ok");
        result.ErrorCode.Should().BeNull();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void GenericFailure_SetsFailureAndMessageAndErrorCode()
    {
        Result<int> result = Result<int>.Failure("bad", "E");

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Data.Should().Be(default(int));
        result.Message.Should().Be("bad");
        result.ErrorCode.Should().Be("E");
    }

    [Fact]
    public void ValidationFailure_AddsErrorsAndUsesValidationDefaults()
    {
        var errors = new List<string> { "a", "b" };

        Result<int> result = Result<int>.ValidationFailure(errors);

        result.IsFailure.Should().BeTrue();
        result.Message.Should().Be("Validation failed");
        result.ErrorCode.Should().Be("VALIDATION_ERROR");
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void ImplicitBoolConversion_UsesIsSuccess()
    {
        Result<int> success = Result<int>.Success(1);
        Result<int> failure = Result<int>.Failure("x");

        bool successValue = success;
        bool failureValue = failure;

        successValue.Should().BeTrue();
        failureValue.Should().BeFalse();
    }

    [Fact]
    public void NonGenericSuccess_SetsSuccess()
    {
        Result result = Result.Success("ok");

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("ok");
    }

    [Fact]
    public void NonGenericFailure_SetsFailure()
    {
        Result result = Result.Failure("no", "ERR");

        result.IsFailure.Should().BeTrue();
        result.Message.Should().Be("no");
        result.ErrorCode.Should().Be("ERR");
    }
}

