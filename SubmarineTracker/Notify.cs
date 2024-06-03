using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace SubmarineTracker;

public class Notify
{
    private readonly Plugin Plugin;

    private bool IsInitialized;
    private readonly HashSet<string> FinishedNotifications = new();

    public Notify(Plugin plugin)
    {
        Plugin = plugin;
    }

    public void Init()
    {
        // We call this in the first framework update to ensure all subs have loaded
        IsInitialized = true;
        foreach (var sub in Plugin.DatabaseCache.GetSubmarines())
            FinishedNotifications.Add($"Dispatch{sub.Register}{sub.Return}");
    }

    public unsafe void NotifyLoop(IFramework _)
    {
        var subs = Plugin.DatabaseCache.GetSubmarines();
        var fcs = Plugin.DatabaseCache.GetFreeCompanies();
        if (subs.Length == 0 || fcs.Count == 0)
            return;

        if (!IsInitialized)
            Init();

        var localId = Plugin.ClientState.LocalContentId;
        foreach (var (id, fc) in fcs)
        {
            foreach (var sub in subs.Where(s => s.FreeCompanyId == id))
            {
                var found = Plugin.Configuration.NotifyFCSpecific.TryGetValue($"{sub.Name}{id}", out var ok);
                if (!Plugin.Configuration.NotifyForAll && !(found && ok))
                    continue;

                // this state happens after the rewards got picked up
                if (sub.Return == 0 || sub.ReturnTime > DateTime.Now.ToUniversalTime())
                    continue;

                if (FinishedNotifications.Add($"Notify{sub.Name}{id}{sub.Return}"))
                {
                    if (Plugin.Configuration.NotifyForReturns)
                        SendReturn(sub, fc);

                    if (Plugin.Configuration.WebhookReturn)
                        Task.Run(() => SendReturnWebhook(sub, fc));
                }
            }
        }

        var fcId = Plugin.GetFCId;
        if (!Plugin.Configuration.NotifyForRepairs || !fcs.TryGetValue(fcId, out var currentFC))
            return;

        foreach (var sub in subs.Where(s => s.FreeCompanyId == fcId))
        {
            // We want this state, as it signals a returned submarine
            if (sub.Return != 0 || sub.NoRepairNeeded)
                continue;

            // using just date here because subs can't come back the same day and be broken again
            if (FinishedNotifications.Add($"Repair{sub.Name}{sub.Register}{localId}{DateTime.Now.Date}"))
                SendRepair(sub, currentFC);
        }
    }

    public unsafe void TriggerDispatch(uint key, uint returnTime)
    {
        if (!Plugin.Configuration.WebhookDispatch)
            return;

        if (!FinishedNotifications.Add($"Dispatch{key}{returnTime}"))
            return;

        var subs = Plugin.DatabaseCache.GetSubmarines();
        var fcs = Plugin.DatabaseCache.GetFreeCompanies();

        var fcId = Plugin.GetFCId;
        if (!fcs.TryGetValue(fcId, out var currentFC))
            return;

        var sub = subs.Where(s => s.FreeCompanyId == fcId).First(s => s.Register == key);

        var found = Plugin.Configuration.NotifyFCSpecific.TryGetValue($"{sub.Name}{fcId}", out var ok);
        if (!Plugin.Configuration.NotifyForAll && !(found && ok))
            return;

        SendDispatchWebhook(sub, currentFC, returnTime);
    }

    public void SendDispatchWebhook(Submarine sub, FreeCompany fc, uint returnTime)
    {
        if (!Plugin.Configuration.WebhookUrl.StartsWith("https://"))
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

    public void SendReturnWebhook(Submarine sub, FreeCompany fc)
    {
        // No need to send messages if the user isn't logged in (also prevents sending on startup)
        if (!Plugin.ClientState.IsLoggedIn)
            return;

        if (!Plugin.Configuration.WebhookUrl.StartsWith("https://"))
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

    public void SendReturn(Submarine sub, FreeCompany fc)
    {
        Plugin.ChatGui.Print(GenerateMessage(Plugin.NameConverter.GetSub(sub, fc)));

        if (Plugin.Configuration.OverlayAlwaysOpen)
            Plugin.ReturnOverlay.IsOpen = true;
    }

    public void SendRepair(Submarine sub, FreeCompany fc)
    {
        Plugin.ChatGui.Print(RepairMessage(Plugin.NameConverter.GetSub(sub, fc)));

        if (Plugin.Configuration.ShowRepairToast)
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
