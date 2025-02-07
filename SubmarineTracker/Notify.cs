using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using SubmarineTracker.Data;
using SubmarineTracker.Windows.Config;

namespace SubmarineTracker;

public class Notify
{
    private readonly Plugin Plugin;

    private bool IsInitialized;
    private readonly HashSet<string> FinishedNotifications = [];

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

    public void NotifyLoop(IFramework _)
    {
        var subs = Plugin.DatabaseCache.GetSubmarines();
        var fcs = Plugin.DatabaseCache.GetFreeCompanies();
        if (subs.Length == 0 || fcs.Count == 0)
            return;

        if (!IsInitialized)
            Init();

        foreach (var id in Plugin.GetFCOrderWithoutHidden())
        {
            var fc = fcs[id];
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
            if (FinishedNotifications.Add($"Repair{sub.Name}{sub.Register}{Plugin.ClientState.LocalContentId}{DateTime.Now.Date}"))
                SendRepair(sub, currentFC);
        }
    }

    public void CheckForDispatch(uint key, uint returnTime)
    {
        var fcId = Plugin.GetFCId;
        if (!Plugin.DatabaseCache.GetFreeCompanies().TryGetValue(fcId, out var currentFC))
            return;

        var subs = Plugin.DatabaseCache.GetSubmarines(fcId);
        if (subs.Length == 0)
            return;

        var sub = subs.FirstOrDefault(s => s.Register == key);
        if (sub == null || !sub.IsValid())
            return;

        if (!FinishedNotifications.Add($"Dispatch{key}{returnTime}"))
            return;

        var found = Plugin.Configuration.NotifyFCSpecific.TryGetValue($"{sub.Name}{fcId}", out var ok);
        if (!Plugin.Configuration.NotifyForAll && !(found && ok))
            return;

        if (Plugin.Configuration.WebhookDispatch)
            SendDispatchWebhook(sub, currentFC, returnTime);

        if (Plugin.Configuration.WebhookOfflineMode)
            SendOfflineModeData(sub, currentFC, returnTime);
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

        if (Plugin.Configuration.WebhookOfflineMode)
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

    public void SendOfflineModeData(Submarine sub, FreeCompany fc, uint returnTime)
    {
        if (!ConfigWindow.WebhookRegex().IsMatch(Plugin.Configuration.WebhookUrl))
            return;

        Plugin.UploadNotify(new Export.SubNotify(
                                Plugin.Configuration.WebhookUrl,
                                Loc.Localize("Webhook On Return", "Returned at <t:{0}:f>").Format(returnTime),
                                Plugin.NameConverter.GetSub(sub, fc),
                                Plugin.Configuration.WebhookMention,
                                Plugin.Configuration.WebhookRoleMention,
                                returnTime));
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
