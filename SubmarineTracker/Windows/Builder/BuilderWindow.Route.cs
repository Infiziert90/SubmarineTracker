using Lumina.Excel.Sheets;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    private int CommonSelection;

    private void RouteTab()
    {
        using var tabItem = ImRaii.TabItem($"{Language.BuilderTabRoute}##Route");
        if (!tabItem.Success)
            return;

        using var child = ImRaii.Child("SubSelector", new Vector2(0, -(170 * ImGuiHelpers.GlobalScale)));
        if (!child.Success)
            return;

        if (!Plugin.DatabaseCache.GetFreeCompanies().TryGetValue(Plugin.GetFCId, out var fcSub))
        {
            Helper.NoData();
            return;
        }

        var maps = Voyage.MapNames;
        var selectedMap = CurrentBuild.Map;
        ImGui.Combo("##mapsSelection", ref selectedMap, maps, maps.Length);
        if (selectedMap != CurrentBuild.Map)
            CurrentBuild.ChangeMap(selectedMap);

        var explorations = Sheets.ExplorationSheet
                                 .Where(r => r.Map.RowId == CurrentBuild.MapRowId && !r.StartingPoint)
                                 .Where(r => !CurrentBuild.Sectors.Contains(r.RowId))
                                 .ToList();

        Helper.TextColored(ImGuiColors.HealerGreen, $"{Language.TermsSectors} {CurrentBuild.Sectors.Count} / 5");
        var startPoint = Voyage.FindStartFromMap(CurrentBuild.MapRowId).RowId;

        var height = ImGui.GetTextLineHeight() * 6.5f; // 5 items max, we give padding space for 6.5
        using (var listBox = ImRaii.ListBox("##selectedPoints", new Vector2(-1, height)))
        {
            if (listBox.Success)
            {
                foreach (var location in Voyage.ToExplorationArray(CurrentBuild.Sectors))
                    if (ImGui.Selectable($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}"))
                        CurrentBuild.Sectors.Remove(location.RowId);
            }
        }

        Helper.TextColored(ImGuiColors.ParsedOrange, Language.BuilderTabRouteSelection);
        using (var listBox = ImRaii.ListBox("##sectorToSelect", new Vector2(-1, height * 2.30f)))
        {
            if (listBox.Success)
            {
                foreach (var location in explorations)
                {
                    fcSub.UnlockedSectors.TryGetValue(location.RowId, out var unlocked);
                    fcSub.ExploredSectors.TryGetValue(location.RowId, out var explored);

                    var unlockTooltip = false;
                    if (CurrentBuild.Sectors.Count < 5)
                    {
                        if (unlocked && explored)
                        {
                            if (ImGui.Selectable($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}"))
                                CurrentBuild.Sectors.Add(location.RowId);
                        }
                        else if (unlocked)
                        {
                            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudViolet))
                                if (ImGui.Selectable($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}"))
                                    CurrentBuild.Sectors.Add(location.RowId);
                        }
                        else
                        {
                            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed))
                                if (ImGui.Selectable($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}"))
                                    CurrentBuild.Sectors.Add(location.RowId);

                            unlockTooltip = true;
                        }
                    }
                    else
                    {
                        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DPSRed))
                            ImGui.Selectable($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}");
                    }

                    if (ImGui.IsItemHovered())
                        UnlockedTooltip(location, fcSub, unlockTooltip);
                }
            }
        }

        if (CurrentBuild.Sectors.Count != 0)
            CurrentBuild.UpdateOptimized(Voyage.FindCalculatedRoute(CurrentBuild.Sectors.ToArray()));

        CommonRoutes();
    }

    private void UnlockedTooltip(SubmarineExploration location, FreeCompany fcSub, bool unlockTooltip)
    {
        if (!Unlocks.SectorToUnlock.TryGetValue(location.RowId, out var unlockedFrom))
            unlockedFrom = new Unlocks.UnlockedFrom(9876);

        fcSub.UnlockedSectors.TryGetValue(unlockedFrom.Sector, out var otherUnlocked);

        using var tooltip = ImRaii.Tooltip();
        using var textWrapPos = ImRaii.TextWrapPos(ImGui.GetFontSize() * 35.0f);

        Helper.TextColored(ImGuiColors.HealerGreen, $"{Language.TermsRank}: ");
        ImGui.SameLine();
        Helper.TextColored(ImGuiColors.HealerGreen, $"{location.RankReq}");

        if (!unlockTooltip)
            return;

        Helper.TextColored(ImGuiColors.DalamudViolet, $"{Language.TermsUnlockedBy}: ");
        ImGui.SameLine();

        if (unlockedFrom.Sector != 9876)
        {
            if (unlockedFrom.Sector != 9000)
            {
                var unlockPoint = Sheets.ExplorationSheet.GetRow(unlockedFrom.Sector);
                var mapPoint = Voyage.FindVoyageStart(unlockPoint.RowId);
                Helper.TextColored(otherUnlocked ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed, $"{NumToLetter(unlockedFrom.Sector - mapPoint)}. {UpperCaseStr(unlockPoint.Destination)}");

                if (unlockedFrom.Sub)
                    Helper.TextColored(ImGuiColors.TankBlue, Language.BuilderWindowTooltipUnlocksSlot);
            }
            else
            {
                Helper.TextColored(ImGuiColors.TankBlue, Language.BuilderWindowTooltipAlwaysUnlocked);
            }
        }
        else
        {
            Helper.TextColored(ImGuiColors.DalamudRed, Language.TermsUnknown);
        }
    }

    private void CommonRoutes()
    {
        var names = Voyage.Common.Keys.Prepend("None").ToArray();

        ImGui.AlignTextToFramePadding();
        Helper.TextColored(ImGuiColors.HealerGreen, Language.BuilderTabRouteCommon);
        ImGui.SameLine(0, 20.0f * ImGuiHelpers.GlobalScale);
        ImGui.SetNextItemWidth(200.0f * ImGuiHelpers.GlobalScale);
        using var combo = ImRaii.Combo("##CommonRouteSelection", names[CommonSelection]);
        if (!combo.Success)
            return;

        if (ImGui.Selectable("None"))
        {
            CommonSelection = 0;
            CurrentBuild.NotOptimized();
        }

        foreach (var ((name, route), idx) in Voyage.Common.Select((val, i) => (val, i)))
        {
            if (ImGui.Selectable(name))
            {
                CurrentBuild.ChangeMap(route.Map);
                CurrentBuild.UpdateOptimized(Voyage.FindCalculatedRoute(route.Route));

                CommonSelection = idx + 1; // + 1 as we manually add None
            }
        }
    }
}
