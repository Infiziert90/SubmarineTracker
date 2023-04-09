using System;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;

namespace SubmarineTracker;

public static class Utils
{
    public static string ToStr(SeString content) => content.ToString();
    public static string ToStr(Lumina.Text.SeString content) => content.ToDalamudString().ToString();

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
}
