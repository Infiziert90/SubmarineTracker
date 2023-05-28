using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows;

public partial class BuilderWindow
{
    public static readonly List<Submarines.SubmarineBuild> AllBuilds = new();
    public int selectedRank;

    public void Initialize()
    {
        AllBuilds.Clear();

        selectedRank = (int)RankSheet.Last().RowId;

        PluginLog.Debug($"MaxRank: {selectedRank}");

        const int partsCount = 10;

        for (var hull = 0; hull < partsCount; hull++)
        {
            for (var stern = 0; stern < partsCount; stern++)
            {
                for (var bow = 0; bow < partsCount; bow++)
                {
                    for (var bridge = 0; bridge < partsCount; bridge++)
                    {
                        var build = new Submarines.SubmarineBuild(selectedRank, (hull * 4) + 3, (stern * 4) + 4, (bow * 4) + 1, (bridge * 4) + 2);
                        AllBuilds.Add(build);
                    }
                }
            }
        }
    }

    public IEnumerable<Submarines.SubmarineBuild> SortBuilds(ImGuiTableColumnSortSpecsPtr sortSpecsPtr)
    {
        Func<Submarines.SubmarineBuild, int> sortFunc = sortSpecsPtr.ColumnIndex switch
        {
            0 => x => x.BuildCost,
            1 => x => x.RepairCosts,
            2 => x => (int)x.Hull.RowId,
            3 => x => (int)x.Stern.RowId,
            4 => x => (int)x.Bow.RowId,
            5 => x => (int)x.Bridge.RowId,
            6 => x => x.Surveillance,
            7 => x => x.Retrieval,
            8 => x => x.Speed,
            9 => x => x.Range,
            10 => x => x.Favor,
            _ => _ => 0
        };

        return sortSpecsPtr.SortDirection switch
        {
            ImGuiSortDirection.Ascending => AllBuilds.OrderBy(sortFunc),
            ImGuiSortDirection.Descending => AllBuilds.OrderByDescending(sortFunc),
            _ => AllBuilds
        };
    }

    public void ShipTab()
    {
        if (ImGui.Combo("Rank", ref selectedRank, RankSheet.Select(x => $"Rank: {x.RowId}").ToArray(), RankSheet.Count()))
        {
            PluginLog.Debug($"Selected Rank: {selectedRank}");
        }
        if (ImGui.BeginTabItem("Ship"))
        {
            ImGui.BeginTable("##shipTable", 11, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SortMulti | ImGuiTableFlags.Sortable);
            ImGui.TableSetupColumn("Cost");
            ImGui.TableSetupColumn("Repair");
            ImGui.TableSetupColumn("Hull", ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn("Stern", ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn("Bow", ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn("Bridge", ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn("Surveillance");
            ImGui.TableSetupColumn("Retrieval");
            ImGui.TableSetupColumn("Speed");
            ImGui.TableSetupColumn("Range");
            ImGui.TableSetupColumn("Favor");
            ImGui.TableHeadersRow();

            foreach (var build in SortBuilds(ImGui.TableGetSortSpecs().Specs))
            {
                ImGui.TableNextColumn();
                ImGui.Text($"{build.BuildCost}");
                ImGui.TableNextColumn();
                ImGui.Text($"{build.RepairCosts}");
                ImGui.TableNextColumn();
                ImGui.Text($"{Submarines.SectionIdToChar[build.HullCharId]}");
                ImGui.TableNextColumn();
                ImGui.Text($"{Submarines.SectionIdToChar[build.SternCharId]}");
                ImGui.TableNextColumn();
                ImGui.Text($"{Submarines.SectionIdToChar[build.BowCharId]}");
                ImGui.TableNextColumn();
                ImGui.Text($"{Submarines.SectionIdToChar[build.BridgeCharId]}");
                ImGui.TableNextColumn();
                ImGui.Text($"{build.Surveillance}");
                ImGui.TableNextColumn();
                ImGui.Text($"{build.Retrieval}");
                ImGui.TableNextColumn();
                ImGui.Text($"{build.Speed}");
                ImGui.TableNextColumn();
                ImGui.Text($"{build.Range}");
                ImGui.TableNextColumn();
                ImGui.Text($"{build.Favor}");
                ImGui.TableNextRow();
            }
            ImGui.EndTable();
            ImGui.EndTabItem();
        }
    }
}
