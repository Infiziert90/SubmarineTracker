using Dalamud.Game.Gui.Dtr;
using Dalamud.Plugin.Services;
using SubmarineTracker.Data;

namespace SubmarineTracker;

public class ServerBar
{
    private readonly Plugin Plugin;
    private readonly DtrBarEntry? DtrEntry;

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
        DtrEntry.Dispose();
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
        var showLast = !Plugin.Configuration.OverlayFirstReturn;

        Submarines.FcSubmarines? fcSub = null;
        Submarines.Submarine? timerSub = null;
        foreach (var fc in Submarines.KnownSubmarines.Values)
        {
            var timer = showLast ? fc.GetLastReturn() : fc.GetFirstReturn();
            if (timer == null)
                continue;

            if (timerSub == null || (showLast ? timer.ReturnTime > timerSub.ReturnTime : timer.ReturnTime < timerSub.ReturnTime))
            {
                fcSub = fc;
                timerSub = timer;
            }
        }

        if (fcSub == null || timerSub == null)
            return;

        var name = Plugin.NameConverter.GetSub(timerSub, fcSub);
        var time = Loc.Localize("Terms - No Voyage", "No Voyage");
        if (timerSub.IsOnVoyage())
        {
            time = Loc.Localize("Terms - Done", "Done");

            var returnTime = timerSub.LeftoverTime();
            if (returnTime.TotalSeconds > 0)
                time = Utils.ToTime(returnTime);
        }

        DtrEntry!.Text = $"{name} ({time})";
    }

    private void UpdateVisibility(bool shown) => DtrEntry!.Shown = shown;

    private void OnClick()
    {
        Plugin.OpenTracker();
    }
}
