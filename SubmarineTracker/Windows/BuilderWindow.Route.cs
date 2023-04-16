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

                var explorations = ExplorationSheet
                                   .Where(r => r.Map.Row == SelectedMap + 1)
                                   .Where(r => !r.Passengers)
                                   .Where(r => !SelectedLocations.Contains(r.RowId))
                                   .ToList();

                ImGui.TextColored(ImGuiColors.HealerGreen, $"Selected {SelectedLocations.Count} / 5");
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

                ImGui.TextColored(ImGuiColors.ParsedOrange, $"Select with click");
                if (ImGui.BeginListBox("##pointsToSelect", new Vector2(-1, height * 1.95f)))
                {
                    foreach (var location in explorations)
                    {
                        if (SelectedLocations.Count < 5)
                        {
                            if (ImGui.Selectable(
                                    $"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}"))
                                SelectedLocations.Add(location.RowId);
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

                var points = SelectedLocations.Prepend(startPoint).ToList();
                OptimizedRoute = Submarines.CalculateDistance(points);
            }
            ImGui.EndChild();

            ImGui.EndTabItem();
        }
    }
}
