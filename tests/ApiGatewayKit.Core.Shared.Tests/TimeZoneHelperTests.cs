using ApiGatewayKit.Core.Shared.Helpers;
using FluentAssertions;
using Xunit;

namespace ApiGatewayKit.Core.Shared.Tests;

public class TimeZoneHelperTests
{
    [Fact]
    public void TurkeyTimeZone_IsNotNull()
    {
        TimeZoneHelper.TurkeyTimeZone.Should().NotBeNull();
    }

    [Fact]
    public void NowTurkey_IsCloseToConvertedUtcNow()
    {
        DateTime utcNow = DateTime.UtcNow;
        DateTime expected = TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneHelper.TurkeyTimeZone);

        DateTime actual = TimeZoneHelper.NowTurkey;

        actual.Should().BeCloseTo(expected, precision: TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void NowTurkeyOffset_IsCloseToConvertedUtcNow()
    {
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        DateTimeOffset expected = TimeZoneInfo.ConvertTime(utcNow, TimeZoneHelper.TurkeyTimeZone);

        DateTimeOffset actual = TimeZoneHelper.NowTurkeyOffset;

        actual.Should().BeCloseTo(expected, precision: TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void ToTurkeyTime_ForUtcDateTime_MatchesTimeZoneInfoConvert()
    {
        DateTime utc = new(2026, 1, 8, 12, 0, 0, DateTimeKind.Utc);

        DateTime actual = utc.ToTurkeyTime();
        DateTime expected = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneHelper.TurkeyTimeZone);

        actual.Should().Be(expected);
    }

    [Fact]
    public void ToTurkeyTime_ForLocalDateTime_UsesToUniversalTimeThenConvert()
    {
        DateTime local = new(2026, 1, 8, 12, 0, 0, DateTimeKind.Local);
        DateTime utc = local.ToUniversalTime();

        DateTime actual = local.ToTurkeyTime();
        DateTime expected = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneHelper.TurkeyTimeZone);

        actual.Should().Be(expected);
    }

    [Fact]
    public void ToTurkeyTime_ForDateTimeOffset_MatchesTimeZoneInfoConvert()
    {
        DateTimeOffset utc = new(new DateTime(2026, 1, 8, 12, 0, 0, DateTimeKind.Utc));

        DateTimeOffset actual = utc.ToTurkeyTime();
        DateTimeOffset expected = TimeZoneInfo.ConvertTime(utc, TimeZoneHelper.TurkeyTimeZone);

        actual.Should().Be(expected);
    }

    [Fact]
    public void ToUtcFromTurkey_MatchesTimeZoneInfoConvert()
    {
        DateTime turkey = new(2026, 1, 8, 15, 0, 0, DateTimeKind.Unspecified);

        DateTime actual = turkey.ToUtcFromTurkey();
        DateTime expected = TimeZoneInfo.ConvertTimeToUtc(turkey, TimeZoneHelper.TurkeyTimeZone);

        actual.Should().Be(expected);
    }

    [Fact]
    public void ToTurkeyString_UsesTurkeyTimeAndFormat()
    {
        DateTime utc = new(2026, 1, 8, 12, 0, 0, DateTimeKind.Utc);
        string format = "yyyy-MM-dd HH:mm:ss";

        string actual = utc.ToTurkeyString(format);
        string expected = utc.ToTurkeyTime().ToString(format);

        actual.Should().Be(expected);
    }

    [Fact]
    public void ToTurkeyString_ForDateTimeOffset_UsesTurkeyTimeAndFormat()
    {
        DateTimeOffset utc = new(new DateTime(2026, 1, 8, 12, 0, 0, DateTimeKind.Utc));
        string format = "yyyy-MM-dd HH:mm:ss";

        string actual = utc.ToTurkeyString(format);
        string expected = utc.ToTurkeyTime().ToString(format);

        actual.Should().Be(expected);
    }
}

