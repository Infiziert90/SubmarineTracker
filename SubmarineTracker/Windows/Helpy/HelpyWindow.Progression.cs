using SubmarineTracker.Data;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Helpy;

public partial class HelpyWindow
{
    private int FcSelection;

    private FreeCompany LastFC = null!;
    private List<(uint, Unlocks.UnlockedFrom)> UnlockPath = null!;

    private void InitProgression()
    {
        UnlockPath = Unlocks.FindUnlockPath(Unlocks.SectorToUnlock.Last(s => s.Value.Sector != SectorType.UnknownUnlock).Key);
        UnlockPath.Reverse();
    }

    private void ProgressionTab()
    {
        using var tabItem = ImRaii.TabItem($"{Language.HelpyTabProgression}##Progression");
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
        using var tabItem = ImRaii.TabItem($"{Language.ProgressionTabSubmarinePath}##SubmarinesPath");
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
        using var tabItem = ImRaii.TabItem($"{Language.ProgressionTabLastSector}##LastSector");
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
        using var tabItem = ImRaii.TabItem($"{Language.HelpyTabInfo}##Info");
        if (!tabItem.Success)
            return;

        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.HelpyTabInfoSectorUnlock);
        ImGui.TextWrapped(Language.HelpyTabInfoSectorUnlockText);

        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.HelpyTabInfoMapUnlock);
        ImGui.TextWrapped(Language.HelpyTabInfoMapUnlockText);

        ImGuiHelpers.ScaledDummy(5.0f);

        var spacing = ImGui.CalcTextSize("Violet").X + 20.0f;
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.HelpyTabInfoColors);
        Helper.TextColored(ImGuiColors.HealerGreen,Language.ColorsGreen);
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Language.HelpyTabInfoGreenText);
        Helper.TextColored(ImGuiColors.DalamudViolet,Language.ColorsViolet);
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Language.HelpyTabInfoVioletText);
        Helper.TextColored(ImGuiColors.DalamudRed,Language.ColorsRed);
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Language.HelpyTabInfoRedText);
    }

    private void PathBoxRenderer((uint, Unlocks.UnlockedFrom) tuple)
    {
        var textHeight = ImGui.GetTextLineHeight() * 3.5f; // 3.5 items padding

        var (point, unlockedFrom) = tuple;
        var explorationPoint = Sheets.ExplorationSheet.GetRow(point);
        var startPoint = Voyage.FindVoyageStart(explorationPoint.RowId);

        var letter = Utils.NumToLetter(explorationPoint.RowId - startPoint);
        var dest = Utils.UpperCaseStr(explorationPoint.Destination);
        var rank = explorationPoint.RankReq;
        var special = unlockedFrom.Sub
                          ? $"{Language.TermsRank} {rank} <{Language.ProgressionTabTooltipUnlocksSlot}>"
                          : unlockedFrom.Map
                              ? $"{Language.TermsRank} {rank} <{Language.ProgressionTabTooltipUnlocksMap}>"
                              : $"{Language.TermsRank} {rank}";

        LastFC.UnlockedSectors.TryGetValue(point, out var hasUnlocked);
        LastFC.ExploredSectors.TryGetValue(point, out var hasExplored);
        var color = hasUnlocked ? hasExplored ? ImGuiColors.HealerGreen : ImGuiColors.DalamudViolet : ImGuiColors.DalamudRed;

        using var child = ImRaii.Child($"##{point}", new Vector2(150.0f * ImGuiHelpers.GlobalScale, textHeight));
        if (!child.Success)
            return;

        using var textWrapm = ImRaii.TextWrapPos(0.0f);
        Helper.TextColored(color, $"{letter}. {dest}");
        Helper.TextColored(ImGuiColors.TankBlue, special);
    }

    private void FCSelection()
    {
        Plugin.EnsureFCOrderSafety();
        var existingFCs = Plugin.Configuration.ManagedFCs
                                .Select(status => $"{Plugin.NameConverter.GetName(Plugin.DatabaseCache.GetFreeCompanies()[status.Id])}##{status.Id}")
                                .ToArray();

        ImGui.AlignTextToFramePadding();
        Helper.TextColored(ImGuiColors.ParsedOrange, "FC:");
        ImGui.SameLine();
        Helper.DrawComboWithArrows("##fcSelection", ref FcSelection, ref existingFCs);
    }
}
