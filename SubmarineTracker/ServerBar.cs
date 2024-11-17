using Dalamud.Game.Gui.Dtr;
using Dalamud.Plugin.Services;

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
        if (!Plugin.Configuration.ShowDtrEntry)
        {
            UpdateVisibility(false);
            return;
        }

        UpdateVisibility(true);
        UpdateBarString();
    }

    private void UpdateBarString()
    {
        var subs = Plugin.DatabaseCache.GetSubmarines();
        var sub = !Plugin.Configuration.OverlayFirstReturn ? subs.MaxBy(s => s.Return) : subs.MinBy(s => s.Return);
        if (sub is not { FreeCompanyId: > 0 })
            return;

        if (!Plugin.DatabaseCache.GetFreeCompanies().TryGetValue(sub.FreeCompanyId, out var fc))
            return;

        var name = Plugin.NameConverter.GetSub(sub, fc);
        var time = Loc.Localize("Terms - No Voyage", "No Voyage");
        if (sub.IsOnVoyage())
        {
            time = Loc.Localize("Terms - Done", "Done");

            var returnTime = sub.LeftoverTime();
            if (returnTime.TotalSeconds > 0)
                time = Utils.ToTime(returnTime);
        }

        var numbers = "";
        if (Plugin.Configuration.DtrShowOverlayNumbers)
            numbers = $" - [{Plugin.ReturnOverlay.OverlayNumbers()}]";

        DtrEntry!.Text = $"{name} ({time}){numbers}";
    }

    private void UpdateVisibility(bool shown) => DtrEntry!.Shown = shown;

    private void OnClick()
    {
        Plugin.OpenTracker();
    }
}
