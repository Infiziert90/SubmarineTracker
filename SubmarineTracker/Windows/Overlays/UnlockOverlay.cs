using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Overlays;

public class UnlockOverlay : Window, IDisposable
{
    private readonly Plugin Plugin;
    private readonly Vector2 OriginalSize = new(300, 60);

    private readonly List<(uint, Unlocks.UnlockedFrom)> PossibleUnlocks = [];

    private ImRaii.Color PushedColor = null!;

    public UnlockOverlay(Plugin plugin) : base("Unlock Overlay##SubmarineTracker")
    {
        Size = OriginalSize;

        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
        ForceMainWindow = true;

        Plugin = plugin;
    }

    public void Dispose() { }

    public override unsafe void PreOpenCheck()
    {
        IsOpen = false;
        if (!Plugin.Configuration.AutoSelectCurrent || !Plugin.Configuration.ShowUnlockOverlay)
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

            var explorationBaseNode = (AtkUnitBase*) addonPtr;
            var y = (Plugin.RouteOverlay.Size!.Value.Y * ImGuiHelpers.GlobalScale) + 10.0f;
            Position = new Vector2(explorationBaseNode->X - (Size!.Value.X * ImGuiHelpers.GlobalScale), explorationBaseNode->Y + y);
            PositionCondition = ImGuiCond.Always;

            var fcSub = Plugin.DatabaseCache.GetFreeCompanies()[Plugin.GetFCId];

            PossibleUnlocks.Clear();
            foreach (var sector in Sheets.ExplorationSheet.Where(s => s.Map.RowId == agent->MapId))
            {
                if (!Unlocks.SectorToUnlock.TryGetValue(sector.RowId, out var unlockedFrom))
                    continue;

                if (unlockedFrom.Main)
                    continue;

                fcSub.UnlockedSectors.TryGetValue(sector.RowId, out var hasUnlocked);
                if (hasUnlocked)
                    continue;

                fcSub.UnlockedSectors.TryGetValue(unlockedFrom.Sector, out hasUnlocked);
                if (hasUnlocked)
                    PossibleUnlocks.Add((sector.RowId, unlockedFrom));
            }

            if (PossibleUnlocks.Count == 0)
                return;

            Size = OriginalSize with { Y = (OriginalSize.Y * PossibleUnlocks.Count) + 50.0f };
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
        if (PossibleUnlocks.Count == 0)
            return;

        foreach (var (sector, from) in PossibleUnlocks.ToArray())
        {

            var unlocked = Sheets.ExplorationSheet.GetRow(sector);
            var unlockedFrom = Sheets.ExplorationSheet.GetRow(from.Sector);
            if (unlockedFrom.RankReq > Plugin.BuilderWindow.CurrentBuild.Rank)
            {
                PossibleUnlocks.Remove((sector, from));
                continue;
            }

            var unlockText = $"{NumToLetter(unlocked.RowId, true)}. {UpperCaseStr(unlocked.Destination)}";
            var visitText = $"{Language.NextOverlayTextVisit} {NumToLetter(unlockedFrom.RowId, true)}. {UpperCaseStr(unlockedFrom.Destination)}";

            var avail = ImGui.GetWindowSize().X;
            var textWidth1 = ImGui.CalcTextSize(unlockText).X;
            var textWidth2 = ImGui.CalcTextSize(visitText).X;

            ImGui.SetCursorPosX((avail - textWidth1) * 0.5f);
            Helper.TextColored(ImGuiColors.DalamudOrange, unlockText);

            ImGui.SetCursorPosX((avail - textWidth2) * 0.5f);
            Helper.TextColored(ImGuiColors.HealerGreen, visitText);

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5.0f);
        }

        if (PossibleUnlocks.Count == 0)
        {
            if (ImGui.IsWindowHovered())
                Helper.Tooltip(Language.NextOverlayTooltipLowRank);
            return;
        }

        if (ImGui.Button(Language.TermsMustInclude))
        {
            foreach (var (_, from) in PossibleUnlocks)
                if (Plugin.RouteOverlay.MustInclude.Add(Sheets.ExplorationSheet.GetRow(from.Sector)))
                    Plugin.RouteOverlay.Calculate = true;
        }
    }

    public override void PostDraw()
    {
        PushedColor.Dispose();
    }
}
