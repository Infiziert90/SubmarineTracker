using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;

namespace SubmarineTracker;

public static class Utils
{
    public static string ToStr(SeString content) => content.ToString();
    public static string ToStr(Lumina.Text.SeString content) => content.ToDalamudString().ToString();
    public static string UpperCaseStr(Lumina.Text.SeString content) => string.Join(" ", content.ToDalamudString().ToString().Split(' ').Select(t => string.Concat(t[0].ToString().ToUpper(), t.AsSpan(1))));
    public static string ToTime(TimeSpan time) => $"{(int)time.TotalHours:#00}:{time:mm}:{time:ss}";

    public static string NumToLetter(uint num)
    {
        var index = (int)(num - 1);  // 0 indexed

        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        var value = "";

        if (index >= letters.Length)
            value += letters[(index / letters.Length) - 1];

        value += letters[index % letters.Length];

        return value;
    }

    public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
    {
        if (!dict.TryGetValue(key, out TValue val))
        {
            val = new TValue();
            dict.Add(key, val);
        }

        return val;
    }

    public class ListComparer : IEqualityComparer<List<uint>>
    {
        public bool Equals(List<uint>? x, List<uint>? y)
        {
            if (x == null)
                return false;
            if (y == null)
                return false;

            return x.Count == y.Count && !x.Except(y).Any();
        }

        public int GetHashCode(List<uint> obj)
        {
            var hash = 19;
            foreach (var element in obj.OrderBy(x => x))
            {
                hash = (hash * 31) + element.GetHashCode();
            }

            return hash;
        }
    }
}

public static class StringExt
{
    public static string? Truncate(this string? value, int maxLength, string truncationSuffix = "...")
    {
        return value?.Length > maxLength
                   ? string.Concat(value.AsSpan(0, maxLength), truncationSuffix)
                   : value;
    }
}
