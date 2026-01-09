using ApiGatewayKit.Core.Shared.Exceptions;
using ApiGatewayKit.Core.Shared.Guards;
using FluentAssertions;
using Xunit;

namespace ApiGatewayKit.Core.Shared.Tests;

public class GuardTests
{
    [Fact]
    public void AgainstNull_WhenValueIsNull_ThrowsArgumentNullException()
    {
        Action act = () => Guard.AgainstNull<object>(null, "value");

        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("value");
    }

    [Fact]
    public void AgainstNull_WhenValueIsNotNull_ReturnsSameInstance()
    {
        object instance = new();

        object result = Guard.AgainstNull(instance, "instance");

        result.Should().BeSameAs(instance);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AgainstNullOrEmpty_WhenNullOrEmpty_ThrowsArgumentException(string? input)
    {
        Action act = () => Guard.AgainstNullOrEmpty(input, "input");

        act.Should()
            .Throw<ArgumentException>()
            .Where(ex => ex.ParamName == "input");
    }

    [Fact]
    public void AgainstBusinessRule_WhenConditionIsTrue_ThrowsBusinessExceptionWithErrorCode()
    {
        Action act = () => Guard.AgainstBusinessRule(true, "rule broken", "RULE");

        BusinessException exception = act.Should().Throw<BusinessException>().Which;
        exception.ErrorCode.Should().Be("RULE");
    }
}

