using Dalamud.Interface;
using ImGuiNET;
using SubmarineTracker.Data;
using System.Linq;
using System.Numerics;

namespace SubmarineTracker.Windows;

public partial class BuilderWindow
{
    private void BuildTab(ref Submarines.Submarine sub)
    {
        if (ImGui.BeginTabItem("Build"))
        {
            if (ImGui.BeginChild("SubSelector", new Vector2(0, -110)))
            {
                var existingSubs = Submarines.KnownSubmarines.Values
                                             .SelectMany(fc => fc.Submarines.Select(s => $"{s.Name} ({s.BuildIdentifier()})"))
                                             .ToArray();
                existingSubs = existingSubs.Prepend("Custom").ToArray();

                var windowWidth = ImGui.GetWindowWidth() / 2;
                ImGui.PushItemWidth(windowWidth - 5.0f);
                ImGui.Combo("##existingSubs", ref SelectSub, existingSubs, existingSubs.Length);
                ImGui.PopItemWidth();

                // Calculate first so rank can be changed afterwards
                if (existingSubs[SelectSub] != "Custom")
                {
                    var fc = Submarines.KnownSubmarines.Values.First(
                        fc => fc.Submarines.Any(s => $"{s.Name} ({s.BuildIdentifier()})" == existingSubs[SelectSub]));
                    sub = fc.Submarines.First(s => $"{s.Name} ({s.BuildIdentifier()})" == existingSubs[SelectSub]);

                    SelectedRank = sub.Rank;
                    SelectedHull = sub.Hull;
                    SelectedStern = sub.Stern;
                    SelectedBow = sub.Bow;
                    SelectedBridge = sub.Bridge;
                }

                ImGui.SameLine();
                ImGui.PushItemWidth(windowWidth - 3.0f);
                ImGui.SliderInt("##SliderRank", ref SelectedRank, 1, (int)RankSheet.Last().RowId, "Rank %d");
                ImGui.PopItemWidth();

                ImGuiHelpers.ScaledDummy(5);

                if (ImGui.BeginTable("##submarinePartSelection", 5))
                {
                    ImGui.TableSetupColumn("Type");
                    ImGui.TableSetupColumn("Hull");
                    ImGui.TableSetupColumn("Stern");
                    ImGui.TableSetupColumn("Bow");
                    ImGui.TableSetupColumn("Bridge");

                    ImGui.TableHeadersRow();

                    BuildTableEntries("Shark", 0);
                    BuildTableEntries("Uniki", 4);
                    BuildTableEntries("Whale", 8);
                    BuildTableEntries("Coelac.", 12);
                    BuildTableEntries("Syldra", 16);

                    BuildTableEntries("MShark", 20);
                    BuildTableEntries("MUniki", 24);
                    BuildTableEntries("MWhale", 28);
                    BuildTableEntries("MCoelac.", 32);
                    BuildTableEntries("MSyldra", 36);
                }

                ImGui.EndTable();
            }
            ImGui.EndChild();

            ImGui.EndTabItem();
        }
    }

    public void BuildTableEntries(string label, int number)
    {
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(label);
        ImGui.TableNextColumn();
        ImGui.RadioButton($"##{label}H", ref SelectedHull, number + 3);
        ImGui.TableNextColumn();
        ImGui.RadioButton($"##{label}S", ref SelectedStern, number + 4);
        ImGui.TableNextColumn();
        ImGui.RadioButton($"##{label}B", ref SelectedBow, number + 1);
        ImGui.TableNextColumn();
        ImGui.RadioButton($"##{label}Br", ref SelectedBridge, number + 2);
        ImGui.TableNextRow();
    }
}
