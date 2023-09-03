using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.RouteOverlay;

public class NextOverlay : Window, IDisposable
{
    private readonly Plugin Plugin;
    private readonly Configuration Configuration;

    public static ExcelSheet<SubmarineExplorationPretty> ExplorationSheet = null!;

    private readonly List<(uint, Unlocks.UnlockedFrom)> UnlockPath;
    private (uint Sector, Unlocks.UnlockedFrom UnlockedFrom)? NextSector;

    public NextOverlay(Plugin plugin, Configuration configuration) : base("Next Overlay")
    {
        Size = new Vector2(300, 60);

        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;

        Plugin = plugin;
        Configuration = configuration;

        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExplorationPretty>()!;

        UnlockPath = Unlocks.FindUnlockPath(Unlocks.PointToUnlockPoint.Last(s => s.Value.Sector != 9876).Key);
        UnlockPath.Reverse();
    }

    public void Dispose() { }

    public override unsafe void PreOpenCheck()
    {
        IsOpen = false;
        if (!Configuration.AutoSelectCurrent || !Configuration.ShowNextOverlay)
            return;

        // Always refresh submarine if we have interface selection
        Plugin.BuilderWindow.RefreshCache();
        try
        {
            var addonPtr = Plugin.GameGui.GetAddonByName("AirShipExploration");
            if (addonPtr == nint.Zero)
                return;

            var explorationBaseNode = (AtkUnitBase*) addonPtr;
            Position = new Vector2(explorationBaseNode->X + 5, explorationBaseNode->Y - (Size!.Value.Y * ImGuiHelpers.GlobalScale));
            PositionCondition = ImGuiCond.Always;

            // Check if submarine voyage log is open and not Airship
            var map = (int) explorationBaseNode->AtkValues[2].UInt;
            if (map < 63191)
                return;

            var selectedMap = map - 63191; // 63191 = Deep-sea Site
            var fcSub = Submarines.KnownSubmarines[Plugin.ClientState.LocalContentId];

            NextSector = null;
            foreach (var (sector, unlockedFrom) in UnlockPath)
            {
                fcSub.UnlockedSectors.TryGetValue(sector, out var hasUnlocked);
                if (hasUnlocked)
                    continue;

                NextSector = (sector, unlockedFrom);
                break;
            }

            if (!NextSector.HasValue || Voyage.FindMapFromSector(NextSector.Value.UnlockedFrom.Sector) != selectedMap + 1)
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
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Helper.TransparentBackground);
    }

    public override void Draw()
    {
        if (!NextSector.HasValue)
            return;

        var nextSector = NextSector.Value;

        var nextUnlock = ExplorationSheet.GetRow(nextSector.Sector)!;
        var unlockedFrom = ExplorationSheet.GetRow(nextSector.UnlockedFrom.Sector)!;
        if (unlockedFrom.RankReq > Plugin.BuilderWindow.CurrentBuild.Rank)
        {
            if (ImGui.IsWindowHovered())
                ImGui.SetTooltip("Your submarine is below the required level to visit the sector.");

            return;
        }

        var isMap = false;
        if (Unlocks.PointToUnlockPoint.TryGetValue(nextSector.UnlockedFrom.Sector, out var previousSector))
            isMap |= previousSector.Map;

        var unlockText = $"Next Sector: {NumToLetter(nextUnlock.RowId, true)}. {UpperCaseStr(nextUnlock.Destination)}";
        var visitText = $"Visit: {NumToLetter(unlockedFrom.RowId, true)}. {UpperCaseStr(unlockedFrom.Destination)}";
        if (isMap)
        {
            unlockText = $"Next Map: {MapToShort(nextUnlock.RowId, true)}";
            visitText = $"Visit: {NumToLetter(unlockedFrom.RowId, true)}. {UpperCaseStr(unlockedFrom.Destination)}";
        }

        var avail = ImGui.GetWindowSize().X;
        var textWidth1 = ImGui.CalcTextSize(unlockText).X;
        var textWidth2 = ImGui.CalcTextSize(visitText).X;

        ImGui.SetCursorPosX((avail - textWidth1) * 0.5f);
        ImGui.TextColored(ImGuiColors.DalamudOrange, unlockText);

        ImGui.SetCursorPosX((avail - textWidth2) * 0.5f);
        ImGui.TextColored(ImGuiColors.HealerGreen, visitText);
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor();
    }
}
