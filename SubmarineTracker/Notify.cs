using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using SubmarineTracker.Windows;

using static SubmarineTracker.Data.Submarines;

namespace SubmarineTracker;

public class Notify
{
    private readonly Plugin Plugin;
    private readonly Configuration Configuration;

    private bool IsInitialized;
    private readonly HashSet<string> FinishedNotifications = new();

    public Notify(Plugin plugin)
    {
        Plugin = plugin;
        Configuration = plugin.Configuration;
    }

    public void Init()
    {
        // We call this in the first framework update to ensure all subs have loaded
        IsInitialized = true;
        foreach (var sub in KnownSubmarines.Values.SelectMany(fc => fc.Submarines))
            FinishedNotifications.Add($"Dispatch{sub.Register}{sub.Return}");
    }

    public void NotifyLoop(IFramework _)
    {
        if (!KnownSubmarines.Any())
            return;

        if (!IsInitialized)
            Init();

        var localId = Plugin.ClientState.LocalContentId;
        foreach (var (id, fc) in KnownSubmarines)
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
                    if (Configuration.NotifyForReturns)
                        SendReturn(sub, fc);

                    if (Configuration.WebhookReturn)
                        Task.Run(() => SendReturnWebhook(sub, fc));
                }
            }
        }

        if (!Configuration.NotifyForRepairs || !KnownSubmarines.TryGetValue(localId, out var currentFC))
            return;

        foreach (var sub in currentFC.Submarines)
        {
            // We want this state, as it signals a returned submarine
            if (sub.Return != 0 || sub.NoRepairNeeded)
                continue;

            // using just date here because subs can't come back the same day and be broken again
            if (FinishedNotifications.Add($"Repair{sub.Name}{sub.Register}{localId}{DateTime.Now.Date}"))
                SendRepair(sub, currentFC);
        }
    }

    public void TriggerDispatch(uint key, uint returnTime)
    {
        if (!Configuration.WebhookDispatch)
            return;

        if (!FinishedNotifications.Add($"Dispatch{key}{returnTime}"))
            return;

        var fc = KnownSubmarines[Plugin.ClientState.LocalContentId];
        var sub = fc.Submarines.Find(s => s.Register == key)!;

        var found = Configuration.NotifySpecific.TryGetValue($"{sub.Name}{Plugin.ClientState.LocalContentId}", out var ok);
        if (!Configuration.NotifyForAll && !(found && ok))
            return;

        SendDispatchWebhook(sub, fc, returnTime);
    }

    public void SendDispatchWebhook(Submarine sub, FcSubmarines fc, uint returnTime)
    {
        if (!Configuration.WebhookUrl.StartsWith("https://"))
            return;

        var content = new Webhook.WebhookContent();
        content.Embeds.Add(new
        {
            title = Plugin.NameConverter.GetSub(sub, fc),
            description=Loc.Localize("Webhook On Dispatch", "Returns <t:{0}:R>").Format(returnTime),
            color=15124255
        });

        Webhook.PostMessage(content);
    }

    public void SendReturnWebhook(Submarine sub, FcSubmarines fc)
    {
        // No need to send messages if the user isn't logged in (also prevents sending on startup)
        if (!Plugin.ClientState.IsLoggedIn)
            return;

        if (!Configuration.WebhookUrl.StartsWith("https://"))
            return;

        // Prevent that multibox user send multiple webhook triggers
        using var mutex = new Mutex(false, "Global\\SubmarineTrackerMutex");
        if (!mutex.WaitOne(0, false))
            return;

        var content = new Webhook.WebhookContent();
        content.Embeds.Add(new
        {
            title = Plugin.NameConverter.GetSub(sub, fc),
            description=Loc.Localize("Webhook On Return", "Returned at <t:{0}:f>").Format(sub.Return),
            color=8447519
        });

        Webhook.PostMessage(content);

        // Ensure that the other process had time to catch up
        Thread.Sleep(500);
        mutex.ReleaseMutex();
    }

    public void SendReturn(Submarine sub, FcSubmarines fc)
    {
        Plugin.ChatGui.Print(GenerateMessage(Plugin.NameConverter.GetSub(sub, fc)));

        if (Configuration.OverlayAlwaysOpen)
            Plugin.ReturnOverlay.IsOpen = true;
    }

    public void SendRepair(Submarine sub, FcSubmarines fc)
    {
        Plugin.ChatGui.Print(RepairMessage(Plugin.NameConverter.GetSub(sub, fc)));

        if (Configuration.ShowRepairToast)
            Plugin.ToastGui.ShowQuest(ShortRepairMessage(), new QuestToastOptions {IconId = 60858, PlaySound = true});
    }

    public static SeString GenerateMessage(string name)
    {
        return new SeStringBuilder()
               .AddUiForeground("[Submarine Tracker] ", 540)
               .AddUiForeground(Loc.Localize("Notification Chat Return", "{0} has returned.").Format(name), 566)
               .BuiltString;
    }

    public static SeString RepairMessage(string name)
    {
        return new SeStringBuilder()
               .AddUiForeground("[Submarine Tracker] ", 540)
               .AddUiForeground(Loc.Localize("Notification Chat Repair", "{0} has returned and requires repair before being dispatched again.").Format(name), 43)
               .BuiltString;
    }

    public static SeString ShortRepairMessage()

    {
        return new SeStringBuilder()
               .AddUiForeground(Loc.Localize("Notification Toast Repair", $"Requires repair before being dispatched again"), 43)
               .BuiltString;
    }
}
