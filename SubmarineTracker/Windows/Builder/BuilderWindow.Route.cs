using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    private void RouteTab()
    {
        if (ImGui.BeginTabItem("Route"))
        {
            if (ImGui.BeginChild("SubSelector", new Vector2(0, -(110 * ImGuiHelpers.GlobalScale))))
            {
                var maps = MapSheet.Where(r => r.RowId != 0).Select(r => ToStr(r.Name)).ToArray();
                var selectedMap = CurrentBuild.Map;
                ImGui.Combo("##mapsSelection", ref selectedMap, maps, maps.Length);
                if (selectedMap != CurrentBuild.Map)
                {
                    CurrentBuild.ChangeMap(selectedMap);
                }

                var fcSub = Submarines.KnownSubmarines[Plugin.ClientState.LocalContentId];

                var explorations = ExplorationSheet
                                   .Where(r => r.Map.Row == CurrentBuild.Map + 1)
                                   .Where(r => !r.StartingPoint)
                                   .Where(r => !CurrentBuild.Sectors.Contains(r.RowId))
                                   .ToList();

                ImGui.TextColored(ImGuiColors.HealerGreen, $"Sectors {CurrentBuild.Sectors.Count} / 5");
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

                ImGui.TextColored(ImGuiColors.ParsedOrange, $"Select sector by clicking");
                if (ImGui.BeginListBox("##pointsToSelect", new Vector2(-1, height * 1.95f)))
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

                if (CurrentBuild.Sectors.Any())
                {
                    var points = CurrentBuild.Sectors.Prepend(startPoint).Select(ExplorationSheet.GetRow).ToList();
                    CurrentBuild.UpdateOptimized(Voyage.CalculateDistance(points!));
                }
            }
            ImGui.EndChild();

            ImGui.EndTabItem();
        }
    }

    private void UnlockedTooltip(SubmarineExplorationPretty location, Submarines.FcSubmarines fcSub, bool unlockTooltip)
    {
        if (!Unlocks.PointToUnlockPoint.TryGetValue(location.RowId, out var unlockedFrom))
            unlockedFrom = new Unlocks.UnlockedFrom(0);

        fcSub.UnlockedSectors.TryGetValue(unlockedFrom.Point, out var otherUnlocked);

        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
        ImGui.TextColored(ImGuiColors.HealerGreen, "Rank: ");
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.HealerGreen, $"{location.RankReq}");

        if (unlockTooltip)
        {
            ImGui.TextColored(ImGuiColors.DalamudViolet, "Unlocked by: ");
            ImGui.SameLine();
            if (unlockedFrom.Point != 9876)
            {
                if (unlockedFrom.Point != 9000)
                {
                    var unlockPoint = ExplorationSheet.GetRow(unlockedFrom.Point)!;
                    var mapPoint = Voyage.FindVoyageStartPoint(unlockPoint.RowId);
                    ImGui.TextColored(otherUnlocked
                                          ? ImGuiColors.HealerGreen
                                          : ImGuiColors.DalamudRed,
                                      $"{NumToLetter(unlockedFrom.Point - mapPoint)}. {UpperCaseStr(unlockPoint.Destination)}");

                    if (unlockedFrom.Sub)
                        ImGui.TextColored(ImGuiColors.TankBlue, $"#Extra Sub Slot");
                }
                else
                    ImGui.TextColored(ImGuiColors.TankBlue, $"Always unlocked");
            }
            else
                ImGui.TextColored(ImGuiColors.DalamudRed, "Unknown");
        }

        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }
}
