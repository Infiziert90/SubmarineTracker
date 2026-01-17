using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Utility;
using Lumina.Text.ReadOnly;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;

namespace SubmarineTracker;

public static class Utils
{
    public static string ToTime(TimeSpan time) =>
        $"{(int)time.TotalHours:#00}:{time:mm}:{time:ss}";

    public static string GetStringFromTimespan(TimeSpan span) =>
        $"{span.Days}d {span.Hours}h {span.Minutes}m {span.Seconds}s";

    public static string UpperCaseStr(ReadOnlySeString s, sbyte article = 0)
    {
        if (article == 1)
            return s.ExtractText();

        var sb = new StringBuilder(s.ExtractText());
        var lastSpace = true;
        for (var i = 0; i < sb.Length; ++i)
        {
            if (sb[i] == ' ')
            {
                lastSpace = true;
            }
            else if (lastSpace)
            {
                lastSpace = false;
                sb[i]     = char.ToUpperInvariant(sb[i]);
            }
        }

        return sb.ToString();
    }

    public static string MapToShort(int key, bool resolveToMap = false) => MapToShort((uint)key, resolveToMap);
    public static string MapToShort(uint key, bool resolveToMap = false)
    {
        if (resolveToMap)
            key = Voyage.FindMapFromSector(key);

        return key switch
        {
            1 => "Deep-sea",
            2 => "Sea of Ash",
            3 => "Sea of Jade",
            4 => "Sirensong",
            5 => "Lilac Sea",
            6 => "South Indigo",
            7 => "Northern Empty",
            _ => Language.TermUnknown
        };
    }

    public static string MapToThreeLetter(int key, bool resolveToMap = false) => MapToThreeLetter((uint) key, resolveToMap);
    public static string MapToThreeLetter(uint key, bool resolveToMap = false)
    {
        if (resolveToMap)
            key = Voyage.FindMapFromSector(key);

        return key switch
        {
            1 => "DSS",
            2 => "SOA",
            3 => "SOJ",
            4 => "SSS",
            5 => "TLS",
            6 => "SID",
            7 => "TNE",
            _ => ""
        };
    }

    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public static string NumToLetter(uint num, bool findStart = false)
    {
        if (findStart)
            num -= Voyage.FindVoyageStart(num);

        var index = (int)(num - 1);  // 0 indexed

        var value = "";
        if (index >= Letters.Length)
            value += Letters[(index / Letters.Length) - 1];

        value += Letters[index % Letters.Length];

        return value;
    }

    public static string SectorsToPath(string separator, IReadOnlyList<uint> points)
    {
        if (points.Count == 0)
            return "No Voyage";

        var start = Voyage.FindVoyageStart(points[0]);
        return string.Join(separator, points.Select(p => NumToLetter(p - start)));
    }

    public static string FormattedRouteBuild(string name, Build.RouteBuild build)
    {
        var route = "No Route";
        if (build.Sectors.Count != 0)
            route = $"{MapToThreeLetter(build.MapRowId)}: {SectorsToPath(" -> ", build.Sectors)}";

        return $"{name.Replace("%", "%%")} (R: {build.Rank} B: {build.GetSubmarineBuild.FullIdentifier()})\n{route}";
    }

    public static SeString SuccessMessage(string success)
    {
        return new SeStringBuilder()
               .AddUiForeground("[Submarine Tracker] ", 540)
               .AddUiForeground($"{success}", 43)
               .BuiltString;
    }

    public static SeString ErrorMessage(string error)
    {
        return new SeStringBuilder()
               .AddUiForeground("[Submarine Tracker] ", 540)
               .AddUiForeground($"{error}", 17)
               .BuiltString;
    }

    public static void AddNotification(string content, NotificationType type, bool minimized = true)
    {
        Plugin.Notification.AddNotification(new Notification { Content = content, Type = type, Minimized = minimized });
    }

    public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
    {
        if (!dict.TryGetValue(key, out TValue? val))
        {
            val = new TValue();
            dict.Add(key, val);
        }

        return val;
    }

    public static bool ContainsAllItems<T>(this IEnumerable<T> a, IEnumerable<T> b)
    {
        return !b.Except(a).Any();
    }

    public class ArrayComparer : IEqualityComparer<uint[]>
    {
        public bool Equals(uint[]? x, uint[]? y)
        {
            if (x == null)
                return false;
            if (y == null)
                return false;

            return x.Length == y.Length && !x.Except(y).Any();
        }

        public int GetHashCode(uint[] obj) => GetUniqueHash(obj);
    }

    public static int GetUniqueHash(uint[] obj)
    {
        var hash = 19;
        foreach (var element in obj.OrderBy(x => x))
            hash = (hash * 31) + element.GetHashCode();

        return hash;
    }

    public static uint GetUniqueId(uint x, uint y)
    {
        return x > y ? y | (x << 8) : x | (y << 8);
    }

    public static string GenerateHashedName(string name)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(name));
        var sb = new StringBuilder(hash.Length * 2);

        foreach (var b in hash)
            sb.Append(b.ToString("X2"));

        return $"{sb.ToString()[..10]}";
    }

    public static bool SubmarinesEqual(List<Submarine> l, List<Submarine> r)
    {
        if (l.Count == 0 || r.Count == 0)
            return false;

        if (l.Count != r.Count)
            return false;

        foreach (var (subL, subR) in l.Zip(r))
            if (!subL.Equals(subR))
                return false;

        return true;
    }

    // From: https: //stackoverflow.com/a/36634935
    public static class Permutations
    {
        public static List<T[]> GetAllPermutation<T>(T[] items)
        {
            var countOfItem = items.Length;
            if (countOfItem <= 1)
                return [Array.Empty<T>()];

            var indexes = new int[countOfItem];
            var permutations = new List<T[]> { items.ToArray() };
            for (var i = 1; i < countOfItem;)
            {
                if (indexes[i] < i)
                {
                    if ((i & 1) == 1)
                        Swap(ref items[i], ref items[indexes[i]]);
                    else
                        Swap(ref items[i], ref items[0]);

                    permutations.Add(items.ToArray());

                    indexes[i]++;
                    i = 1;
                }
                else
                {
                    indexes[i++] = 0;
                }
            }

            return permutations;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap<T>(ref T a, ref T b)
        {
            (a, b) = (b, a);
        }
    }
}

public static class Extensions
{
    /// <summary>
    /// Truncate a string after its max length is reached.
    /// </summary>
    public static string? Truncate(this string? value, int maxLength, string truncationSuffix = "...")
    {
        return value?.Length > maxLength ? string.Concat(value.AsSpan(0, maxLength), truncationSuffix) : value;
    }

    /// <summary>
    /// Quick swap two list objects.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap<T>(this List<T> list, int i, int j)
    {
        (list[i], list[j]) = (list[j], list[i]);
    }

    /// <summary>
    /// Format a Datetime as just Date.
    /// </summary>
    public static string ToLongDateWithoutWeekday(this DateTime d)
    {
        return d.ToString(CultureInfo.CurrentCulture.DateTimeFormat.GetAllDateTimePatterns('D')
                                     .FirstOrDefault(a => !a.Contains("ddd") && !a.Contains("dddd")) ?? "D");
    }

    /// <summary>
    /// Adds the index to a simple iteration.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<(T Val, int Idx)> WithIndex<T>(this IEnumerable<T> list)
    {
        return list.Select((val, idx) => (val, idx));
    }

    /// <summary>
    /// Simple filter for Span like LINQs `Where()`.
    /// </summary>
    public static List<T> Filter<T>(this Span<T> span, Func<T, bool> predicate)
    {
        var ret = new List<T>(span.Length);
        foreach(var item in span)
            if (predicate(item))
                ret.Add(item);

        return ret;
    }
}
