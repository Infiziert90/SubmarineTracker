using SubmarineTracker.Data;

namespace SubmarineTracker.Windows;

public partial class BuilderWindow
{
    private void BuildTab(ref Submarines.Submarine sub)
    {
        if (ImGui.BeginTabItem("Build"))
        {
            if (ImGui.BeginChild("SubSelector", new Vector2(0, -110)))
            {
                var existingSubs = Configuration.FCOrder
                                             .SelectMany(id => Submarines.KnownSubmarines[id].Submarines.Select(s => $"{s.Name} ({s.BuildIdentifier()})"))
                                             .ToArray();
                existingSubs = existingSubs.Prepend("Custom").ToArray();

                var windowWidth = ImGui.GetWindowWidth() / 2;
                ImGui.PushItemWidth(windowWidth - 5.0f);
                ImGui.Combo("##existingSubs", ref CurrentBuild.OriginalSub, existingSubs, existingSubs.Length);
                ImGui.PopItemWidth();

                // Calculate first so rank can be changed afterwards
                if (existingSubs[CurrentBuild.OriginalSub] != "Custom")
                {
                    var fc = Submarines.KnownSubmarines.Values.First(fc => fc.Submarines.Any(s => $"{s.Name} ({s.BuildIdentifier()})" == existingSubs[CurrentBuild.OriginalSub]));
                    sub = fc.Submarines.First(s => $"{s.Name} ({s.BuildIdentifier()})" == existingSubs[CurrentBuild.OriginalSub]);

                    CurrentBuild.UpdateBuild(sub);
                }

                ImGui.SameLine();
                ImGui.PushItemWidth(windowWidth - 3.0f);
                ImGui.SliderInt("##SliderRank", ref CurrentBuild.Rank, 1, (int) RankSheet.Last().RowId, "Rank %d");
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
        ImGui.RadioButton($"##{label}H", ref CurrentBuild.Hull, number + 3);
        ImGui.TableNextColumn();
        ImGui.RadioButton($"##{label}S", ref CurrentBuild.Stern, number + 4);
        ImGui.TableNextColumn();
        ImGui.RadioButton($"##{label}B", ref CurrentBuild.Bow, number + 1);
        ImGui.TableNextColumn();
        ImGui.RadioButton($"##{label}Br", ref CurrentBuild.Bridge, number + 2);
        ImGui.TableNextRow();
    }
}
