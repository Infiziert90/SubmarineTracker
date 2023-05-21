using System;

namespace SubmarineTracker.Data
{
    public enum DurationLimit
    {
        None = 0,
        H24 = 1,
        H36 = 2,
        H48 = 3,
    }


    public enum DateLimit
    {
        None = 0,
        W1 = 1,
        W2 = 2,
        W3 = 3,
        W4 = 4,
        M1 = 5,
        M3 = 6,
        M6 = 7,
        M9 = 8,
        Year = 9,
    }

    public static class DateUtil {
        public static string GetDurationLimitName(DurationLimit n)
        {
            return n switch
            {
                DurationLimit.None => "No Limit",
                DurationLimit.H24 => "24 Hours",
                DurationLimit.H36 => "36 Hours",
                DurationLimit.H48 => "48 Hours",
                _ => "Unknown"
            };
        }

        public static TimeSpan DurationToTime(DurationLimit n)
        {
            return n switch
            {
                DurationLimit.None => TimeSpan.MaxValue,
                DurationLimit.H24 => TimeSpan.FromHours(24),
                DurationLimit.H36 => TimeSpan.FromHours(36),
                DurationLimit.H48 => TimeSpan.FromHours(48),
                _ => TimeSpan.MaxValue
            };
        }

        public static string GetDateLimitName(DateLimit n)
        {
            return n switch
            {
                DateLimit.None => "No Limit",
                DateLimit.W1 => "1 Week",
                DateLimit.W2 => "2 Weeks",
                DateLimit.W3 => "3 Weeks",
                DateLimit.W4 => "4 Weeks",
                DateLimit.M1 => "1 Month",
                DateLimit.M3 => "3 Months",
                DateLimit.M6 => "6 Months",
                DateLimit.M9 => "9 Months",
                DateLimit.Year => "1 Year",
                _ => "Unknown"
            };
        }

        public static DateTime LimitToDate(DateLimit n)
        {
            return n switch
            {
                DateLimit.None => DateTime.UnixEpoch,
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

        private static DateTime GetPreviousWeek(int weeks) => DateTime.UtcNow.AddDays(-(7 * weeks));
        private static DateTime GetPreviousMonth(int months) => DateTime.UtcNow.AddMonths(-(1 * months));
        private static DateTime GetPreviousYear(int years) => DateTime.UtcNow.AddYears(-(1 * years));
    }
}
