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
            DurationLimit.None => Loc.Localize("Duration Limit - No Limit", "No Limit"),
            DurationLimit.H24 => Loc.Localize("Duration Limit - 24 Hours", "24 Hours"),
            DurationLimit.H36 => Loc.Localize("Duration Limit - 36 Hours", "36 Hours"),
            DurationLimit.H48 => Loc.Localize("Duration Limit - 48 Hours", "48 Hours"),
            DurationLimit.Custom => Loc.Localize("Duration Limit - Custom", "Custom"),
            _ => "Unknown"
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
            DateLimit.None => Loc.Localize("Duration Limit - No Limit", "No Limit"),
            DateLimit.D1 => Loc.Localize("Date Limit - 1 Day", "1 Day"),
            DateLimit.D3 => Loc.Localize("Date Limit - 3 Days", "3 Days"),
            DateLimit.D5 => Loc.Localize("Date Limit - 5 Days", "5 Days"),
            DateLimit.W1 => Loc.Localize("Date Limit - 1 Week", "1 Week"),
            DateLimit.W2 => Loc.Localize("Date Limit - 2 Weeks", "2 Weeks"),
            DateLimit.W3 => Loc.Localize("Date Limit - 3 Weeks", "3 Weeks"),
            DateLimit.W4 => Loc.Localize("Date Limit - 4 Weeks", "4 Weeks"),
            DateLimit.M1 => Loc.Localize("Date Limit - 1 Month", "1 Month"),
            DateLimit.M3 => Loc.Localize("Date Limit - 3 Months", "3 Months"),
            DateLimit.M6 => Loc.Localize("Date Limit - 6 Months", "6 Months"),
            DateLimit.M9 => Loc.Localize("Date Limit - 9 Months", "9 Months"),
            DateLimit.Year => Loc.Localize("Date Limit - 1 Year", "1 Year"),
            _ => "Unknown"
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
