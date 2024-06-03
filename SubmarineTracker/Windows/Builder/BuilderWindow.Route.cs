using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    private int CommonSelection;

    private void RouteTab()
    {
        if (ImGui.BeginTabItem($"{Loc.Localize("Builder Tab - Route", "Route")}##Route"))
        {
            if (ImGui.BeginChild("SubSelector", new Vector2(0, -(170 * ImGuiHelpers.GlobalScale))))
            {
                if (!Plugin.DatabaseCache.GetFreeCompanies().TryGetValue(Plugin.GetFCId, out var fcSub))
                {
                    Helper.NoData();

                    ImGui.EndChild();
                    ImGui.EndTabItem();
                    return;
                }

                var maps = MapSheet.Where(r => r.RowId != 0).Select(r => ToStr(r.Name)).ToArray();
                var selectedMap = CurrentBuild.Map;
                ImGui.Combo("##mapsSelection", ref selectedMap, maps, maps.Length);
                if (selectedMap != CurrentBuild.Map)
                {
                    CurrentBuild.ChangeMap(selectedMap);
                }

                var explorations = ExplorationSheet
                                   .Where(r => r.Map.Row == CurrentBuild.Map + 1)
                                   .Where(r => !r.StartingPoint)
                                   .Where(r => !CurrentBuild.Sectors.Contains(r.RowId))
                                   .ToList();

                ImGui.TextColored(ImGuiColors.HealerGreen, $"{Loc.Localize("Terms - Sectors", "Sectors")} {CurrentBuild.Sectors.Count} / 5");
                var startPoint = ExplorationSheet.First(r => r.Map.Row == CurrentBuild.Map + 1).RowId;

                var height = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
                if (ImGui.BeginListBox("##selectedPoints", new Vector2(-1, height)))
                {
                    foreach (var location in CurrentBuild.Sectors.ToArray())
                    {
                        var p = ExplorationSheet.GetRow(location)!;
                        if (ImGui.Selectable($"{NumToLetter(location - startPoint)}. {UpperCaseStr(p.Destination)}"))
                            CurrentBuild.Sectors.Remove(location);
                    }

                    ImGui.EndListBox();
                }

                ImGui.TextColored(ImGuiColors.ParsedOrange, Loc.Localize("Builder Tab Route - Selection", "Select sector by clicking"));
                if (ImGui.BeginListBox("##pointsToSelect", new Vector2(-1, height * 2.30f)))
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
                                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudViolet);
                                if (ImGui.Selectable($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}"))
                                    CurrentBuild.Sectors.Add(location.RowId);
                                ImGui.PopStyleColor();
                            }
                            else
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                if (ImGui.Selectable($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}"))
                                    CurrentBuild.Sectors.Add(location.RowId);
                                ImGui.PopStyleColor();

                                unlockTooltip = true;
                            }
                        }
                        else
                        {
                            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ImGuiColors.DPSRed);
                            ImGui.Selectable($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}");
                            ImGui.PopStyleColor();
                        }

                        if (ImGui.IsItemHovered())
                            UnlockedTooltip(location, fcSub, unlockTooltip);
                    }

                    ImGui.EndListBox();
                }

                if (CurrentBuild.Sectors.Count != 0)
                    CurrentBuild.UpdateOptimized(Voyage.FindCalculatedRoute(CurrentBuild.Sectors.ToArray()));

                CommonRoutes();
            }
            ImGui.EndChild();

            ImGui.EndTabItem();
        }
    }

    private void UnlockedTooltip(SubExplPretty location, FreeCompany fcSub, bool unlockTooltip)
    {
        if (!Unlocks.SectorToUnlock.TryGetValue(location.RowId, out var unlockedFrom))
            unlockedFrom = new Unlocks.UnlockedFrom(9876);

        fcSub.UnlockedSectors.TryGetValue(unlockedFrom.Sector, out var otherUnlocked);

        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
        ImGui.TextColored(ImGuiColors.HealerGreen, $"{Loc.Localize("Terms - Rank", "Rank")}: ");
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.HealerGreen, $"{location.RankReq}");

        if (unlockTooltip)
        {
            ImGui.TextColored(ImGuiColors.DalamudViolet, $"{Loc.Localize("Terms - Unlocked By", "Unlocked By")}: ");
            ImGui.SameLine();
            if (unlockedFrom.Sector != 9876)
            {
                if (unlockedFrom.Sector != 9000)
                {
                    var unlockPoint = ExplorationSheet.GetRow(unlockedFrom.Sector)!;
                    var mapPoint = Voyage.FindVoyageStart(unlockPoint.RowId);
                    ImGui.TextColored(otherUnlocked
                                          ? ImGuiColors.HealerGreen
                                          : ImGuiColors.DalamudRed,
                                      $"{NumToLetter(unlockedFrom.Sector - mapPoint)}. {UpperCaseStr(unlockPoint.Destination)}");

                    if (unlockedFrom.Sub)
                        ImGui.TextColored(ImGuiColors.TankBlue, $"{Loc.Localize("Builder Window Tooltip - Unlocks Slot", "#Extra Sub Slot")}");
                }
                else
                {
                    ImGui.TextColored(ImGuiColors.TankBlue, Loc.Localize("Builder Window Tooltip - Always Unlocked", "Always Unlocked"));
                }
            }
            else
            {
                ImGui.TextColored(ImGuiColors.DalamudRed, Loc.Localize("Terms - Unknown", "Unknown"));
            }
        }

        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }

    private void CommonRoutes()
    {
        var names = Voyage.Common.Keys.Prepend("None").ToArray();

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.HealerGreen, Loc.Localize("Builder Tab Route - Common", "Common Routes"));
        ImGui.SameLine(0, 20.0f * ImGuiHelpers.GlobalScale);
        ImGui.SetNextItemWidth(200.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginCombo($"##CommonRouteSelection", names[CommonSelection]))
        {
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

            ImGui.EndCombo();
        }
    }
}
