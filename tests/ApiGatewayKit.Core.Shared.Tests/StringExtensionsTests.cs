using ApiGatewayKit.Core.Shared.Extensions;
using FluentAssertions;
using Xunit;

namespace ApiGatewayKit.Core.Shared.Tests;

public class StringExtensionsTests
{
    [Fact]
    public void IsNullOrEmpty_Works()
    {
        ((string?)null).IsNullOrEmpty().Should().BeTrue();
        "".IsNullOrEmpty().Should().BeTrue();
        "x".IsNullOrEmpty().Should().BeFalse();
    }

    [Fact]
    public void IsNullOrWhiteSpace_Works()
    {
        ((string?)null).IsNullOrWhiteSpace().Should().BeTrue();
        "".IsNullOrWhiteSpace().Should().BeTrue();
        "   ".IsNullOrWhiteSpace().Should().BeTrue();
        "x".IsNullOrWhiteSpace().Should().BeFalse();
    }

    [Fact]
    public void Truncate_WhenNullOrEmpty_ReturnsEmpty()
    {
        ((string?)null).Truncate(10).Should().BeEmpty();
        "".Truncate(10).Should().BeEmpty();
    }

    [Fact]
    public void Truncate_WhenMaxLengthIsNonPositive_ReturnsEmpty()
    {
        "abcdef".Truncate(0).Should().BeEmpty();
        "abcdef".Truncate(-1).Should().BeEmpty();
    }

    [Fact]
    public void Truncate_WhenAlreadyShort_ReturnsSame()
    {
        "abc".Truncate(5).Should().Be("abc");
        "abc".Truncate(3).Should().Be("abc");
    }

    [Fact]
    public void Truncate_WhenSuffixLongerThanMaxLength_ReturnsTrimmedSuffix()
    {
        "abcdef".Truncate(2, "...").Should().Be("..");
    }

    [Fact]
    public void Truncate_WhenLong_ReturnsTruncatedWithSuffix()
    {
        "abcdef".Truncate(5, "...").Should().Be("ab...");
    }

    [Fact]
    public void ToSha256Hash_ProducesExpectedHash()
    {
        "abc".ToSha256Hash().Should().Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
    }

    [Fact]
    public void Base64_RoundTrip_Works()
    {
        const string input = "hello";

        string encoded = input.ToBase64();
        string decoded = encoded.FromBase64();

        decoded.Should().Be(input);
    }

    [Fact]
    public void SanitizeForLogging_RemovesNewlinesTabsAndEscapeSequences()
    {
        const string input = "a\r\nb\t\x1B[31mred\x1B[0m";

        string sanitized = input.SanitizeForLogging();

        sanitized.Should().NotContain("\r");
        sanitized.Should().NotContain("\n");
        sanitized.Should().NotContain("\t");
        sanitized.Should().NotContain("\x1B");
    }

    [Theory]
    [InlineData("a@b.com", true)]
    [InlineData("a@b", false)]
    [InlineData("a", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidEmail_Works(string? email, bool expected)
    {
        email.IsValidEmail().Should().Be(expected);
    }

    [Theory]
    [InlineData("5551234567", "(555) 123-4567")]
    [InlineData(" (555) 123-4567 ", "(555) 123-4567")]
    [InlineData("15551234567", "+1 (555) 123-4567")]
    public void FormatPhoneNumber_FormatsKnownPatterns(string input, string expected)
    {
        input.FormatPhoneNumber().Should().Be(expected);
    }

    [Fact]
    public void MaskCreditCard_MasksAllButLastFour()
    {
        "4111 1111 1111 1234".MaskCreditCard().Should().Be("****-****-****-1234");
        "123".MaskCreditCard().Should().Be("****");
        ((string?)null).MaskCreditCard().Should().BeEmpty();
    }

    [Theory]
    [InlineData("john.doe@example.com", "jo***@example.com")]
    [InlineData("a@example.com", "***@example.com")]
    [InlineData("not-an-email", "***@***")]
    public void MaskEmail_MasksLocalPart(string input, string expected)
    {
        input.MaskEmail().Should().Be(expected);
    }
}

