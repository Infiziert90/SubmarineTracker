using Dalamud.Game.Addon.Events.EventDataTypes;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Plugin.Services;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;

namespace SubmarineTracker;

public class ServerBar
{
    private readonly Plugin Plugin;
    private readonly IDtrBarEntry? DtrEntry;

    public ServerBar(Plugin plugin)
    {
        Plugin = plugin;

        if (Plugin.DtrBar.Get("SubmarineTracker") is not { } entry)
            return;

        DtrEntry = entry;

        DtrEntry.Text = "Submarines are cool...";
        DtrEntry.Shown = false;
        DtrEntry.OnClick += OnClick;

        Plugin.Framework.Update += UpdateDtrBar;
    }

    public void Dispose()
    {
        if (DtrEntry == null)
            return;

        Plugin.Framework.Update -= UpdateDtrBar;
        DtrEntry.OnClick -= OnClick;
        DtrEntry.Remove();
    }

    public void UpdateDtrBar(IFramework framework)
    {
        if (!Plugin.Configuration.ShowDtrEntry && !Plugin.Configuration.DtrShowOverlayNumbers)
        {
            UpdateVisibility(false);
            return;
        }

        UpdateVisibility(true);
        UpdateBarString();
    }

    private void UpdateBarString()
    {
        var returning = "";
        if (Plugin.Configuration.ShowDtrEntry)
        {
            var subs = Plugin.DatabaseCache.GetSubmarines();
            var sub = !Plugin.Configuration.OverlayFirstReturn ? subs.MaxBy(s => s.Return) : subs.MinBy(s => s.Return);
            if (sub is not { FreeCompanyId: > 0 })
                return;

            if (!Plugin.DatabaseCache.GetFreeCompanies().TryGetValue(sub.FreeCompanyId, out var fc))
                return;

            var name = Plugin.NameConverter.GetSub(sub, fc, Plugin.Configuration.DtrShowSubmarineName);
            var time = Language.TermsNoVoyage;
            if (sub.IsOnVoyage())
            {
                time = Language.TermsDone;

                var returnTime = sub.LeftoverTime();
                if (returnTime.TotalSeconds > 0)
                    time = Utils.ToTime(returnTime);
            }

            returning = $"{name} ({time})";
        }

        var separator = Plugin.Configuration.DtrShowOverlayNumbers && Plugin.Configuration.ShowDtrEntry ? " - " : string.Empty;

        var numbers = "";
        if (Plugin.Configuration.DtrShowOverlayNumbers)
            numbers = $"[{Plugin.ReturnOverlay.OverlayNumbers()}]";

        var slots = "";
        if (Plugin.Configuration.DtrShowInventorySlots && Storage.InventorySlotsFree > -1)
            slots = $" - [{140 - Storage.InventorySlotsFree} / 140]";

        DtrEntry!.Text = $"{returning}{separator}{numbers}{slots}";
    }

    private void UpdateVisibility(bool shown) => DtrEntry!.Shown = shown;

    private void OnClick(AddonMouseEventData _) => Plugin.OpenTracker();
}
