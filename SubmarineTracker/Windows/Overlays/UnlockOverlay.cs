using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Overlays;

public class UnlockOverlay : Window, IDisposable
{
    private readonly Plugin Plugin;
    private readonly Configuration Configuration;
    private readonly Vector2 OriginalSize = new(300, 60);

    private static ExcelSheet<SubmarineExplorationPretty> ExplorationSheet = null!;

    private readonly List<(uint, Unlocks.UnlockedFrom)> PossibleUnlocks = new();

    public UnlockOverlay(Plugin plugin, Configuration configuration) : base("Unlock Overlay")
    {
        Size = OriginalSize;

        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;

        Plugin = plugin;
        Configuration = configuration;

        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExplorationPretty>()!;
    }

    public void Dispose() { }

    public override unsafe void PreOpenCheck()
    {
        IsOpen = false;
        if (!Configuration.AutoSelectCurrent || !Configuration.ShowUnlockOverlay)
            return;

        // Always refresh submarine if we have interface selection
        Plugin.BuilderWindow.RefreshCache();
        try
        {
            var addonPtr = Plugin.GameGui.GetAddonByName("AirShipExploration");
            if (addonPtr == nint.Zero)
                return;

            var explorationBaseNode = (AtkUnitBase*) addonPtr;
            var y = (Plugin.RouteOverlay.Size!.Value.Y * ImGuiHelpers.GlobalScale) + 10.0f;
            Position = new Vector2(explorationBaseNode->X - (Size!.Value.X * ImGuiHelpers.GlobalScale), explorationBaseNode->Y + y);
            PositionCondition = ImGuiCond.Always;

            // Check if submarine voyage log is open and not Airship
            var map = (int) explorationBaseNode->AtkValues[2].UInt;
            if (map < 63191)
                return;

            var selectedMap = map - 63191; // 63191 = Deep-sea Site
            var fcSub = Submarines.KnownSubmarines[Plugin.ClientState.LocalContentId];

            PossibleUnlocks.Clear();
            foreach (var sector in ExplorationSheet.Where(s => s.Map.Row == selectedMap + 1))
            {
                if (!Unlocks.PointToUnlockPoint.TryGetValue(sector.RowId, out var unlockedFrom))
                    continue;

                fcSub.UnlockedSectors.TryGetValue(sector.RowId, out var hasUnlocked);
                if (hasUnlocked)
                    continue;

                fcSub.UnlockedSectors.TryGetValue(unlockedFrom.Sector, out hasUnlocked);
                if (hasUnlocked)
                    PossibleUnlocks.Add((sector.RowId, unlockedFrom));
            }

            if (!PossibleUnlocks.Any())
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
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Helper.TransparentBackground);
    }

    public override void Draw()
    {
        if (!PossibleUnlocks.Any())
            return;

        foreach (var (sector, from) in PossibleUnlocks.ToArray())
        {

            var unlocked = ExplorationSheet.GetRow(sector)!;
            var unlockedFrom = ExplorationSheet.GetRow(from.Sector)!;
            if (unlockedFrom.RankReq > Plugin.BuilderWindow.CurrentBuild.Rank)
            {
                PossibleUnlocks.Remove((sector, from));
                continue;
            }

            var unlockText = $"{UpperCaseStr(unlocked.Destination)}";
            var visitText = $"Visit: {NumToLetter(unlockedFrom.RowId, true)}. {UpperCaseStr(unlockedFrom.Destination)}";

            var avail = ImGui.GetWindowSize().X;
            var textWidth1 = ImGui.CalcTextSize(unlockText).X;
            var textWidth2 = ImGui.CalcTextSize(visitText).X;

            ImGui.SetCursorPosX((avail - textWidth1) * 0.5f);
            ImGui.TextColored(ImGuiColors.DalamudOrange, unlockText);

            ImGui.SetCursorPosX((avail - textWidth2) * 0.5f);
            ImGui.TextColored(ImGuiColors.HealerGreen, visitText);

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5.0f);
        }

        if (PossibleUnlocks.Count == 0)
        {
            if (ImGui.IsWindowHovered())
                ImGui.SetTooltip("Your submarine is below the required level to visit the sector.");
            return;
        }

        if (ImGui.Button("Must Include"))
        {
            foreach (var (_, from) in PossibleUnlocks)
                Plugin.RouteOverlay.MustInclude.Add(ExplorationSheet.GetRow(from.Sector)!);
        }
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor();
    }
}
