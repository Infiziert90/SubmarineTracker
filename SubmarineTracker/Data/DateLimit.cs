using SubmarineTracker.Resources;

namespace SubmarineTracker.Data;

public enum DurationLimit
{
    None = 0,
    H24 = 1,
    H36 = 2,
    H48 = 3,
    Custom = 99,
}

public enum DateLimit
{
    None = 0,
    D1 = 1,
    D3 = 2,
    D5 = 3,
    W1 = 4,
    W2 = 5,
    W3 = 6,
    W4 = 7,
    M1 = 8,
    M3 = 9,
    M6 = 10,
    M9 = 11,
    Year = 12,
}

public static class DateUtil
{
    public static string GetName(this DurationLimit n)
    {
        return n switch
        {
            DurationLimit.None => Language.DurationLimitNoLimit,
            DurationLimit.H24 => Language.DurationLimit24Hours,
            DurationLimit.H36 => Language.DurationLimit36Hours,
            DurationLimit.H48 => Language.DurationLimit48Hours,
            DurationLimit.Custom => Language.DurationLimitCustom,
            _ => Language.TermUnknown
        };
    }

    public static uint ToSeconds(this DurationLimit n)
    {
        return n switch
        {
            DurationLimit.H24 => 24 * 60 * 60,
            DurationLimit.H36 => 36 * 60 * 60,
            DurationLimit.H48 => 48 * 60 * 60,
            DurationLimit.Custom => (uint) (((Plugin.Configuration.CustomHour * 60) + Plugin.Configuration.CustomMinute) * 60),
            _ => uint.MaxValue
        };
    }

    public static string GetName(this DateLimit n)
    {
        return n switch
        {
            DateLimit.None => Language.DurationLimitNoLimit,
            DateLimit.D1 => Language.DateLimit1Day,
            DateLimit.D3 => Language.DateLimit3Days,
            DateLimit.D5 => Language.DateLimit5Days,
            DateLimit.W1 => Language.DateLimit1Week,
            DateLimit.W2 => Language.DateLimit2Weeks,
            DateLimit.W3 => Language.DateLimit3Weeks,
            DateLimit.W4 => Language.DateLimit4Weeks,
            DateLimit.M1 => Language.DateLimit1Month,
            DateLimit.M3 => Language.DateLimit3Months,
            DateLimit.M6 => Language.DateLimit6Months,
            DateLimit.M9 => Language.DateLimit9Months,
            DateLimit.Year => Language.DateLimit1Year,
            _ => Language.TermUnknown
        };
    }

    public static DateTime ToDate(this DateLimit n)
    {
        return n switch
        {
            DateLimit.None => DateTime.UnixEpoch,
            DateLimit.D1 => GetPreviousDay(1),
            DateLimit.D3 => GetPreviousDay(3),
            DateLimit.D5 => GetPreviousDay(5),
            DateLimit.W1 => GetPreviousWeek(1),
            DateLimit.W2 => GetPreviousWeek(2),
            DateLimit.W3 => GetPreviousWeek(3),
            DateLimit.W4 => GetPreviousWeek(4),
            DateLimit.M1 => GetPreviousMonth(1),
            DateLimit.M3 => GetPreviousMonth(3),
            DateLimit.M6 => GetPreviousMonth(6),
            DateLimit.M9 => GetPreviousMonth(9),
            DateLimit.Year => GetPreviousYear(1),
            _ => DateTime.UnixEpoch,
        };
    }

    private static DateTime GetPreviousDay(int days) => DateTime.UtcNow.AddDays(-(1 * days));
    private static DateTime GetPreviousWeek(int weeks) => DateTime.UtcNow.AddDays(-(7 * weeks));
    private static DateTime GetPreviousMonth(int months) => DateTime.UtcNow.AddMonths(-(1 * months));
    private static DateTime GetPreviousYear(int years) => DateTime.UtcNow.AddYears(-(1 * years));
}
