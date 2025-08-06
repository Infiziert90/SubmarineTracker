using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Overlays;

public class NextOverlay : Window, IDisposable
{
    private readonly Plugin Plugin;

    private readonly List<(uint, Unlocks.UnlockedFrom)> UnlockPath;
    private (uint Sector, Unlocks.UnlockedFrom UnlockedFrom)? NextSector;

    private ImRaii.Color PushedColor = null!;

    public NextOverlay(Plugin plugin) : base("Next Overlay##SubmarineTracker")
    {
        Size = new Vector2(300, 60);

        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
        ForceMainWindow = true;

        Plugin = plugin;

        UnlockPath = Unlocks.FindUnlockPath(Unlocks.SectorToUnlock.Last(s => s.Value.Sector != 9876).Key);
        UnlockPath.Reverse();
    }

    public void Dispose() { }

    public override unsafe void PreOpenCheck()
    {
        IsOpen = false;
        if (!Plugin.Configuration.AutoSelectCurrent || !Plugin.Configuration.ShowNextOverlay)
            return;

        // Always refresh submarine if we have interface selection
        Plugin.BuilderWindow.RefreshCache();
        try
        {
            var agent = AgentSubmersibleExploration.Instance();
            if (agent == null || agent->MapId == 0)
                return;

            var addonPtr = Plugin.GameGui.GetAddonByName("AirShipExploration");
            if (addonPtr == nint.Zero)
                return;

            Position = new Vector2(addonPtr.X + 5, addonPtr.Y - (Size!.Value.Y * ImGuiHelpers.GlobalScale));
            PositionCondition = ImGuiCond.Always;

            NextSector = null;
            var fcSub = Plugin.DatabaseCache.GetFreeCompanies()[Plugin.GetFCId];
            foreach (var (sector, unlockedFrom) in UnlockPath)
            {
                fcSub.UnlockedSectors.TryGetValue(sector, out var hasUnlocked);
                if (hasUnlocked)
                    continue;

                NextSector = (sector, unlockedFrom);
                break;
            }

            if (!NextSector.HasValue || Voyage.FindMapFromSector(NextSector.Value.UnlockedFrom.Sector) != agent->MapId)
                return;

            IsOpen = true;
        }
        catch
        {
            // Something went wrong, we don't draw
        }
    }

    public override void PreDraw()
    {
        PushedColor = ImRaii.PushColor(ImGuiCol.WindowBg, Helper.TransparentBackground);
    }

    public override void Draw()
    {
        if (!NextSector.HasValue)
            return;

        var nextSector = NextSector.Value;

        var nextUnlock = Sheets.ExplorationSheet.GetRow(nextSector.Sector);
        var unlockedFrom = Sheets.ExplorationSheet.GetRow(nextSector.UnlockedFrom.Sector);
        if (unlockedFrom.RankReq > Plugin.BuilderWindow.CurrentBuild.Rank)
        {
            if (ImGui.IsWindowHovered())
                Helper.Tooltip(Language.NextOverlayTooltipLowRank);

            return;
        }

        var isMap = false;
        if (Unlocks.SectorToUnlock.TryGetValue(nextSector.UnlockedFrom.Sector, out var previousSector))
            isMap = previousSector.Map;

        var unlockText = $"{Language.NextOverlayTextNextSector} {NumToLetter(nextUnlock.RowId, true)}. {UpperCaseStr(nextUnlock.Destination)}";
        var visitText = $"{Language.NextOverlayTextVisit} {NumToLetter(unlockedFrom.RowId, true)}. {UpperCaseStr(unlockedFrom.Destination)}";
        if (isMap)
        {
            unlockText = $"{Language.NextOverlayTextNextMap} {MapToShort(nextUnlock.RowId, true)}";
            visitText = $"{Language.NextOverlayTextVisit} {NumToLetter(unlockedFrom.RowId, true)}. {UpperCaseStr(unlockedFrom.Destination)}";
        }

        var avail = ImGui.GetWindowSize().X;
        var textWidth1 = ImGui.CalcTextSize(unlockText).X;
        var textWidth2 = ImGui.CalcTextSize(visitText).X;

        ImGui.SetCursorPosX((avail - textWidth1) * 0.5f);
        Helper.TextColored(ImGuiColors.DalamudOrange, unlockText);

        ImGui.SetCursorPosX((avail - textWidth2) * 0.5f);
        Helper.TextColored(ImGuiColors.HealerGreen, visitText);

        if (Plugin.Configuration.MainRouteAutoInclude && Plugin.RouteOverlay.MustInclude.Add(unlockedFrom))
            Plugin.RouteOverlay.Calculate = true;
    }

    public override void PostDraw()
    {
        PushedColor.Dispose();
    }
}
