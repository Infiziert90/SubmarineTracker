using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;
using SubmarineTracker.Data;

namespace SubmarineTracker;

public class Notify
{
    private Plugin Plugin;
    private Configuration Configuration;

    public List<string> OverlayNotifications = new();
    private List<string> FinishedNotifications = new();

    public Notify(Plugin plugin)
    {
        Plugin = plugin;
        Configuration = plugin.Configuration;
    }

    public void NotifyLoop(Framework _)
    {
        if (!Submarines.KnownSubmarines.Any())
            return;

        foreach (var (id, fc) in Submarines.KnownSubmarines)
        {
            foreach (var sub in fc.Submarines)
            {
                if (!Configuration.NotifySpecific.TryGetValue($"{sub.Name}{id}", out var ok))
                    ok = false;

                if (!Configuration.NotifyForAll && !ok)
                    continue;

                // this state happens after the rewards got picked up
                if (sub.Return == 0)
                    continue;

                var returnTime = sub.ReturnTime - DateTime.Now.ToUniversalTime();
                if (returnTime.TotalSeconds > 0)
                    continue;

                if (!FinishedNotifications.Contains($"{sub.Name}{id}{sub.Return}"))
                {
                    FinishedNotifications.Add($"{sub.Name}{id}{sub.Return}");

                    var text = $"{sub.Name}@{fc.World}";
                    if (Configuration.UseCharacterName && fc.CharacterName != "")
                        text = $"{sub.Name}@{fc.CharacterName}";

                    OverlayNotifications.Add($"{text} has returned.");
                    Plugin.ChatGui.Print(GenerateMessage(text));

                    Plugin.OpenNotify();
                }
            }
        }
    }

    public static SeString GenerateMessage(string text)
    {
        return new SeStringBuilder()
               .AddUiForeground("[Submarine Tracker] ", 540)
               .AddUiForeground($"{text} has returned.", 566)
               .BuiltString;
    }
}
