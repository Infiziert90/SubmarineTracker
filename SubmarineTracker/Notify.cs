using Dalamud.Game;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using SubmarineTracker.Data;
using SubmarineTracker.Windows;

namespace SubmarineTracker;

public class Notify
{
    private readonly Plugin Plugin;
    private readonly Configuration Configuration;

    private readonly HashSet<string> FinishedNotifications = new();

    public Notify(Plugin plugin)
    {
        Plugin = plugin;
        Configuration = plugin.Configuration;
    }

    public void NotifyLoop(Framework _)
    {
        if (!Submarines.KnownSubmarines.Any())
            return;

        var localId = Plugin.ClientState.LocalContentId;
        foreach (var (id, fc) in Submarines.KnownSubmarines)
        {
            foreach (var sub in fc.Submarines)
            {
                var found = Configuration.NotifySpecific.TryGetValue($"{sub.Name}{id}", out var ok);
                if (!Configuration.NotifyForAll && !(found && ok))
                    continue;

                // this state happens after the rewards got picked up
                if (sub.Return == 0 || sub.ReturnTime > DateTime.Now.ToUniversalTime())
                    continue;

                if (FinishedNotifications.Add($"Notify{sub.Name}{id}{sub.Return}"))
                {
                    Plugin.ChatGui.Print(GenerateMessage(Helper.GetSubName(sub, fc)));

                    if (Configuration.OverlayAlwaysOpen)
                        Plugin.OverlayWindow.IsOpen = true;
                }
            }
        }

        if (!Configuration.NotifyForRepairs || !Submarines.KnownSubmarines.TryGetValue(localId, out var currentFC))
            return;

        foreach (var sub in currentFC.Submarines)
        {
            // We want this state, as it signals a returned submarine
            if (sub.Return != 0 || sub.NoRepairNeeded)
                continue;

            // using just date here because subs can't come back the same day and be broken again
            if (FinishedNotifications.Add($"Repair{sub.Name}{sub.Register}{localId}{DateTime.Now.Date}"))
            {
                Plugin.ChatGui.Print(RepairMessage(Helper.GetSubName(sub, currentFC)));

                if (Configuration.ShowRepairToast)
                    Plugin.ToastGui.ShowQuest(ShortRepairMessage(), new QuestToastOptions {IconId = 60858, PlaySound = true});
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

    public static SeString RepairMessage(string name)
    {
        return new SeStringBuilder()
               .AddUiForeground("[Submarine Tracker] ", 540)
               .AddUiForeground($"{name} has returned and requires repair before being dispatched again.", 43)
               .BuiltString;
    }

    public static SeString ShortRepairMessage()
    {
        return new SeStringBuilder()
               .AddUiForeground($"Requires repair before being dispatched again", 43)
               .BuiltString;
    }
}
