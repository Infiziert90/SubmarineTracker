using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Helpy;

public partial class HelpyWindow
{
    private int FcSelection;

    private FreeCompany LastFC;
    private List<(uint, Unlocks.UnlockedFrom)> UnlockPath = null!;

    private void InitProgression()
    {
        UnlockPath = Unlocks.FindUnlockPath(Unlocks.SectorToUnlock.Last(s => s.Value.Sector != 9876).Key);
        UnlockPath.Reverse();
    }

    private void ProgressionTab()
    {
        using var tabItem = ImRaii.TabItem($"{Loc.Localize("Helpy Tab - Progression", "Progression")}##Progression");
        if (!tabItem.Success)
            return;

        using var tabBar = ImRaii.TabBar("##ProgressionTabBar");
        if (!tabBar.Success)
            return;

        AllSlotsTab();

        LastSectorTab();

        InfoTab();
    }

    private void AllSlotsTab()
    {
        using var tabItem = ImRaii.TabItem($"{Loc.Localize("Progression Tab - Submarine Path", "4 Submarines")}##SubmarinesPath");
        if (!tabItem.Success)
            return;

        FCSelection();
        if (!Plugin.DatabaseCache.TryGetFC(Plugin.GetManagedFCOrDefault(FcSelection).Id, out var fcSub))
        {
            Helper.NoData();
            return;
        }

        var unlockPath = Unlocks.FindUnlockPath(20);
        unlockPath.Reverse();

        LastFC = fcSub;
        var mod = new Box.Modifier();
        mod.Padding(10 * ImGuiHelpers.GlobalScale);
        mod.BorderColor(ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudGrey)));

        ImGuiHelpers.ScaledDummy(10.0f);
        BoxList.RenderList(unlockPath, mod, 2f, PathBoxRenderer);
    }

    private void LastSectorTab()
    {
        using var tabItem = ImRaii.TabItem($"{Loc.Localize("Progression Tab - Last Sector", "Last Sector")}##LastSector");
        if (!tabItem.Success)
            return;

        FCSelection();
        if (!Plugin.DatabaseCache.TryGetFC(Plugin.GetManagedFCOrDefault(FcSelection).Id, out var fcSub))
        {
            Helper.NoData();
            return;
        }

        LastFC = fcSub;
        var mod = new Box.Modifier();
        mod.Padding(10 * ImGuiHelpers.GlobalScale);
        mod.BorderColor(ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudGrey)));

        ImGuiHelpers.ScaledDummy(10.0f);
        BoxList.RenderList(UnlockPath, mod, 2f, PathBoxRenderer);
    }

    private static void InfoTab()
    {
        using var tabItem = ImRaii.TabItem($"{Loc.Localize("Helpy Tab - Info", "Info")}##Info");
        if (!tabItem.Success)
            return;

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Helpy Tab Info - Sector Unlock", "How to unlock new Sectors?"));
        ImGui.TextWrapped(Loc.Localize("Helpy Tab Info - Sector Unlock Text", "Unlocking new sectors is RNG.\nBy increasing the speed and frequency of voyages, you will have more chances to discover a new sector."));

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Helpy Tab Info - Map Unlock", "How to unlock new maps / slots?"));
        ImGui.TextWrapped(Loc.Localize("Helpy Tab Info - Map Unlock Text", "You must visit the unlocked sector at least once.\nNote: You won't see more submarines until you filled the previous slot.\ne.g You won't see slot 3 if slot 2 is empty"));

        ImGuiHelpers.ScaledDummy(5.0f);

        var spacing = ImGui.CalcTextSize("Violet").X + 20.0f;
        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Helpy Tab Info - Colors", "Colors:"));
        ImGui.TextColored(ImGuiColors.HealerGreen,Loc.Localize("Colors - Green", "Green"));
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Loc.Localize("Helpy Tab Info - Green Text", "Unlocked and visited"));
        ImGui.TextColored(ImGuiColors.DalamudViolet,Loc.Localize("Colors - Violet", "Violet"));
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Loc.Localize("Helpy Tab Info - Violet Text", "Unlocked but not visited"));
        ImGui.TextColored(ImGuiColors.DalamudRed,Loc.Localize("Colors - Red", "Red"));
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Loc.Localize("Helpy Tab Info - Red Text", "Not unlocked"));
    }

    private void PathBoxRenderer((uint, Unlocks.UnlockedFrom) tuple)
    {
        var textHeight = ImGui.CalcTextSize("XXX").Y * 3.5f; // 3.5 items padding

        var (point, unlockedFrom) = tuple;
        var explorationPoint = Sheets.ExplorationSheet.GetRow(point)!;
        var startPoint = Voyage.FindVoyageStart(explorationPoint.RowId);

        var letter = Utils.NumToLetter(explorationPoint.RowId - startPoint);
        var dest = Utils.UpperCaseStr(explorationPoint.Destination);
        var rank = explorationPoint.RankReq;
        var special = unlockedFrom.Sub
                          ? $"{Loc.Localize("Terms - Rank", "Rank")} {rank} <{Loc.Localize("Progression Tab Tooltip - Unlocks Slot", "Unlocks slot")}>"
                          : unlockedFrom.Map
                              ? $"{Loc.Localize("Terms - Rank", "Rank")} {rank} <{Loc.Localize("Progression Tab Tooltip - Unlocks Map", "Unlocks map")}>"
                              : $"{Loc.Localize("Terms - Rank", "Rank")} {rank}";

        LastFC.UnlockedSectors.TryGetValue(point, out var hasUnlocked);
        LastFC.ExploredSectors.TryGetValue(point, out var hasExplored);
        var color = hasUnlocked
                        ? hasExplored
                              ? ImGuiColors.HealerGreen : ImGuiColors.DalamudViolet
                        : ImGuiColors.DalamudRed;

        using var child = ImRaii.Child($"##{point}", new Vector2(150.0f * ImGuiHelpers.GlobalScale, textHeight));
        ImGui.PushTextWrapPos();
        ImGui.TextColored(color, $"{letter}. {dest}");
        ImGui.TextColored(ImGuiColors.TankBlue, special);
        ImGui.PopTextWrapPos();
    }

    private void FCSelection()
    {
        Plugin.EnsureFCOrderSafety();
        var existingFCs = Plugin.Configuration.ManagedFCs
                                .Select(status => $"{Plugin.NameConverter.GetName(Plugin.DatabaseCache.GetFreeCompanies()[status.Id])}##{status.Id}")
                                .ToArray();

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.ParsedOrange, "FC:");
        ImGui.SameLine();
        Helper.DrawComboWithArrows("##fcSelection", ref FcSelection, ref existingFCs);
    }
}
