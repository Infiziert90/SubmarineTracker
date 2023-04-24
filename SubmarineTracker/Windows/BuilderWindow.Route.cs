using Dalamud.Interface.Colors;
using ImGuiNET;
using SubmarineTracker.Data;
using System.Linq;
using System.Numerics;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows;

public partial class BuilderWindow
{
    private void RouteTab()
    {
        if (ImGui.BeginTabItem("Route"))
        {
            if (ImGui.BeginChild("SubSelector", new Vector2(0, -110)))
            {
                var maps = MapSheet.Where(r => r.RowId != 0).Select(r => ToStr(r.Name)).ToArray();
                var selectedMap = SelectedMap;
                ImGui.Combo("##mapsSelection", ref selectedMap, maps, maps.Length);
                if (selectedMap != SelectedMap)
                {
                    SelectedMap = selectedMap;
                    SelectedLocations.Clear();
                }

                var fcSub = Submarines.KnownSubmarines[Plugin.ClientState.LocalContentId];

                var explorations = ExplorationSheet
                                   .Where(r => r.Map.Row == SelectedMap + 1)
                                   .Where(r => !r.Passengers)
                                   .Where(r => !SelectedLocations.Contains(r.RowId))
                                   .ToList();

                ImGui.TextColored(ImGuiColors.HealerGreen, $"Sectors {SelectedLocations.Count} / 5");
                var startPoint = ExplorationSheet.First(r => r.Map.Row == SelectedMap + 1).RowId;

                var height = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
                if (ImGui.BeginListBox("##selectedPoints", new Vector2(-1, height)))
                {
                    foreach (var location in SelectedLocations.ToArray())
                    {
                        var p = ExplorationSheet.GetRow(location)!;
                        if (ImGui.Selectable($"{NumToLetter(location - startPoint)}. {UpperCaseStr(p.Destination)}"))
                            SelectedLocations.Remove(location);
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

                        if (SelectedLocations.Count < 5)
                        {
                            if (unlocked && explored)
                            {
                                if (ImGui.Selectable($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}"))
                                    SelectedLocations.Add(location.RowId);
                            }
                            else if (unlocked)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudViolet);
                                ImGui.Selectable($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}");
                                ImGui.PopStyleColor();
                            }
                            else
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                ImGui.Selectable($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}");
                                ImGui.PopStyleColor();

                                if (ImGui.IsItemHovered())
                                    UnlockedTooltip(location, fcSub);
                            }
                        }
                        else
                        {
                            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ImGuiColors.DPSRed);
                            ImGui.Selectable($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}");
                            ImGui.PopStyleColor();
                        }
                    }

                    ImGui.EndListBox();
                }

                if (SelectedLocations.Any())
                {
                    var points = SelectedLocations.Prepend(startPoint).Select(ExplorationSheet.GetRow).ToList();
                    OptimizedRoute = Submarines.CalculateDistance(points);
                }
            }
            ImGui.EndChild();

            ImGui.EndTabItem();
        }
    }

    private void UnlockedTooltip(SubmarineExplorationPretty location, Submarines.FcSubmarines fcSub)
    {
        if (!Unlocks.PointToUnlockPoint.TryGetValue(location.RowId, out var unlockedFrom))
            unlockedFrom = new Unlocks.UnlockedFrom(0);

        fcSub.UnlockedSectors.TryGetValue(unlockedFrom.Point, out var otherUnlocked);

        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Unlocked by: ");
        ImGui.SameLine();
        if (unlockedFrom.Point != 0)
        {
            if (unlockedFrom.Point != 9000)
            {
                var unlockPoint = ExplorationSheet.GetRow(unlockedFrom.Point)!;
                var mapPoint = Submarines.FindVoyageStartPoint(unlockPoint.RowId);
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

        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }
}
