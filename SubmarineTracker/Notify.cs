using Dalamud.Game;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using SubmarineTracker.Data;

namespace SubmarineTracker;

public class Notify
{
    private Plugin Plugin;
    private Configuration Configuration;

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

                    Plugin.ChatGui.Print(GenerateMessage(text));
                }
            }
        }

        if (!Configuration.NotifyForRepairs)
            return;

        if (Submarines.KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var currentFC))
        {
            foreach (var (sub, idx) in currentFC.Submarines.Select((val, i) => (val, i)))
            {
                // We want this state, as it signals a returned submarine
                if (sub.Return != 0)
                    continue;

                if (sub.NoRepairNeeded)
                    continue;

                // using just date here because subs can't come back the same day and be broken again
                if (!FinishedNotifications.Contains($"Repair{sub.Name}{idx}{Plugin.ClientState.LocalContentId}{DateTime.Now.Date}"))
                {
                    FinishedNotifications.Add($"Repair{sub.Name}{idx}{Plugin.ClientState.LocalContentId}{DateTime.Now.Date}");

                    var text = $"{sub.Name}@{currentFC.World}";
                    if (Configuration.UseCharacterName && currentFC.CharacterName != "")
                        text = $"{sub.Name}@{currentFC.CharacterName}";

                    Plugin.ChatGui.Print(RepairMessage(text));

                    if (Configuration.ShowRepairToast)
                        Plugin.ToastGui.ShowQuest(ShortRepairMessage(), new QuestToastOptions {IconId = 60858, PlaySound = true});
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
