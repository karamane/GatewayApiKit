namespace ApiGatewayKit.Core.Shared.Helpers;

/// <summary>
/// TÃ¼rkiye saat dilimi iÃ§in helper sÄ±nÄ±f
/// </summary>
public static class TimeZoneHelper
{
    /// <summary>
    /// TÃ¼rkiye saat dilimi ID
    /// </summary>
    public const string TurkeyTimeZoneId = "Turkey Standard Time";

    /// <summary>
    /// TÃ¼rkiye saat dilimi (IANA formatÄ± - Linux iÃ§in)
    /// </summary>
    public const string TurkeyTimeZoneIdIana = "Europe/Istanbul";

    private static readonly Lazy<TimeZoneInfo> _turkeyTimeZone = new(() =>
    {
        try
        {
            // Windows iÃ§in
            return TimeZoneInfo.FindSystemTimeZoneById(TurkeyTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Linux/macOS iÃ§in IANA formatÄ±
                return TimeZoneInfo.FindSystemTimeZoneById(TurkeyTimeZoneIdIana);
            }
            catch
            {
                // Fallback: UTC+3 olarak manuel oluÅŸtur
                return TimeZoneInfo.CreateCustomTimeZone(
                    "Turkey",
                    TimeSpan.FromHours(3),
                    "Turkey Standard Time",
                    "Turkey Standard Time");
            }
        }
    });

    /// <summary>
    /// TÃ¼rkiye TimeZoneInfo
    /// </summary>
    public static TimeZoneInfo TurkeyTimeZone => _turkeyTimeZone.Value;

    /// <summary>
    /// Åu anki TÃ¼rkiye saati
    /// </summary>
    public static DateTime NowTurkey => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTimeZone);

    /// <summary>
    /// Åu anki TÃ¼rkiye saati (DateTimeOffset)
    /// </summary>
    public static DateTimeOffset NowTurkeyOffset => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TurkeyTimeZone);

    /// <summary>
    /// UTC'yi TÃ¼rkiye saatine Ã§evirir
    /// </summary>
    public static DateTime ToTurkeyTime(this DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Local)
        {
            utcDateTime = utcDateTime.ToUniversalTime();
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TurkeyTimeZone);
    }

    /// <summary>
    /// UTC'yi TÃ¼rkiye saatine Ã§evirir (DateTimeOffset)
    /// </summary>
    public static DateTimeOffset ToTurkeyTime(this DateTimeOffset utcDateTime)
    {
        return TimeZoneInfo.ConvertTime(utcDateTime, TurkeyTimeZone);
    }

    /// <summary>
    /// TÃ¼rkiye saatini UTC'ye Ã§evirir
    /// </summary>
    public static DateTime ToUtcFromTurkey(this DateTime turkeyDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(turkeyDateTime, TurkeyTimeZone);
    }

    /// <summary>
    /// Tarih formatÄ± (TÃ¼rkiye standardÄ±)
    /// </summary>
    public static string ToTurkeyString(this DateTime dateTime, string format = "dd.MM.yyyy HH:mm:ss")
    {
        return dateTime.ToTurkeyTime().ToString(format);
    }

    /// <summary>
    /// Tarih formatÄ± (TÃ¼rkiye standardÄ±)
    /// </summary>
    public static string ToTurkeyString(this DateTimeOffset dateTime, string format = "dd.MM.yyyy HH:mm:ss")
    {
        return dateTime.ToTurkeyTime().ToString(format);
    }
}


