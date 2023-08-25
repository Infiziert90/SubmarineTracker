using Dalamud.Interface.Components;
using SubmarineTracker.Data;
using static SubmarineTracker.Data.Loot;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Loot;

public partial class LootWindow
{
    private SubmarineExplorationPretty SelectedSector = null!;
    private Dictionary<uint, List<DetailedLoot>> LootCache = new();

    private ExcelSheetSelector.ExcelSheetPopupOptions<SubmarineExplorationPretty> Options = null!;

    private void InitializeAnalyse()
    {
        SelectedSector = ExplorationSheet.GetRow(1)!;
        Options = new ExcelSheetSelector.ExcelSheetPopupOptions<SubmarineExplorationPretty>
        {
            FormatRow = e => $"{MapToThreeLetter(e.RowId, true)} - {NumToLetter(e.RowId, true)}. {UpperCaseStr(e.Destination)} (Rank {e.RankReq})",
            FilteredSheet = ExplorationSheet.Where(r => r.RankReq > 0)
        };
    }

    private void AnalyseTab()
    {
        if (!ImGui.BeginTabItem("Analyse"))
            return;

        ImGuiHelpers.ScaledDummy(10.0f);

        var wip = "- Work in Progress -";
        var width = ImGui.GetWindowWidth();
        var textWidth = ImGui.CalcTextSize(wip).X;

        ImGui.SetCursorPosX((width - textWidth) * 0.5f);
        ImGui.TextColored(ImGuiColors.DalamudOrange, wip);
        ImGuiHelpers.ScaledDummy(10.0f);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5.0f);

        if (LootCache.Count == 0)
        {
            var submarineLoot = Submarines.KnownSubmarines.Values
                                          .Select(fc => fc.SubLoot)
                                          .SelectMany(dict => dict.Values)
                                          .SelectMany(loot => loot.Loot.Values.SelectMany(l => l).Where(detailed => !Configuration.ExcludeLegacy || detailed.Valid).Where(detailed => detailed.Primary > 0));

            foreach (var loot in submarineLoot)
                LootCache.GetOrCreate(loot.Sector).Add(loot);
        }

        ImGuiComponents.IconButton(FontAwesomeIcon.Search);
        if (ExcelSheetSelector.ExcelSheetPopup("LootSectorAnalyseAddPopup", out var row, Options))
            SelectedSector = ExplorationSheet.GetRow(row)!;

        ImGui.SameLine();

        if (Helper.DrawButtonWithTooltip(FontAwesomeIcon.ArrowCircleUp, "Rebuild cache"))
        {
            LootCache.Clear();

            ImGui.EndTabItem();
            return;
        }

        ImGui.TextColored(ImGuiColors.ParsedOrange, $"Searched for {MapToThreeLetter(SelectedSector.RowId, true)} - {NumToLetter(SelectedSector.RowId, true)}. {UpperCaseStr(SelectedSector.Destination)}");
        if (!LootCache.TryGetValue(SelectedSector.RowId, out var history))
        {
            ImGui.TextColored(ImGuiColors.ParsedOrange, $"Nothing found for this sector ...");

            ImGui.EndTabItem();
            return;
        }

        var statDict = new Dictionary<uint, (uint Min, uint Max)>();
        foreach (var result in history)
        {
            if (!statDict.TryAdd(result.Primary, (result.PrimaryCount, result.PrimaryCount)))
            {
                var stat = statDict[result.Primary];
                if (stat.Min > result.PrimaryCount)
                    statDict[result.Primary] = (result.PrimaryCount, stat.Max);

                if (stat.Max < result.PrimaryCount)
                    statDict[result.Primary] = (stat.Min, result.PrimaryCount);
            }

            if (result.ValidAdditional && !statDict.TryAdd(result.Additional, (result.AdditionalCount, result.AdditionalCount)))
            {
                var stat = statDict[result.Additional];
                if (stat.Min > result.AdditionalCount)
                    statDict[result.Additional] = (result.AdditionalCount, stat.Max);

                if (stat.Max < result.AdditionalCount)
                    statDict[result.Additional] = (stat.Min, result.AdditionalCount);
            }
        }

        var doubleDips = history.Sum(ll => ll.ValidAdditional ? 1 : 0);
        var sectorHits = history.Count;
        ImGui.TextColored(ImGuiColors.HealerGreen, $"Hit {sectorHits:N0} time{(sectorHits > 1 ? "s" : "")}");
        ImGui.TextColored(ImGuiColors.HealerGreen, $"DD {doubleDips:N0} time{(doubleDips > 1 ? "s" : "")} ({(double) doubleDips / sectorHits * 100.0:F2}%%)");
        if (ImGui.BeginTable($"##AnalyseStats", 4, 0, new Vector2(300, 0)))
        {
            ImGui.TableSetupColumn("##statItemName", 0, 0.6f);
            ImGui.TableSetupColumn("##statMin", 0, 0.1f);
            ImGui.TableSetupColumn("##statSymbol", 0, 0.05f);
            ImGui.TableSetupColumn("##statMax", 0, 0.1f);

            foreach (var statPair in statDict.OrderByDescending(pair => pair.Key))
            {
                var name = ToStr(ItemSheet.GetRow(statPair.Key)!.Name);
                ImGui.TableNextColumn();
                if (ImGui.Selectable($"{name}"))
                    ImGui.SetClipboardText(name);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{statPair.Value.Min}");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted("-");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{statPair.Value.Max}");

                ImGui.TableNextRow();
            }

            ImGui.EndTable();
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        var percentageDict = new Dictionary<uint, uint>();
        foreach (var result in history)
        {
            if (!percentageDict.TryAdd(result.Primary, 1))
                percentageDict[result.Primary] += 1;

            if (result.ValidAdditional && !percentageDict.TryAdd(result.Additional, 1))
                percentageDict[result.Additional] += 1;
        }

        var sortedList = percentageDict.Where(pair => pair.Value > 0).Select(pair =>
        {
            var item = ItemSheet.GetRow(pair.Key)!;
            var count = pair.Value;
            var percentage = (double) count / (sectorHits + doubleDips) * 100.0;
            return new SortedEntry(item.Icon, ToStr(item.Name), count, percentage);
        }).OrderByDescending(x => x.Percentage);

        ImGui.TextColored(ImGuiColors.HealerGreen, $"Percentages:");

        if (ImGui.BeginTable($"##PercentageSourceTable", 3))
        {
            ImGui.TableSetupColumn("##icon", ImGuiTableColumnFlags.WidthFixed, IconSize.X + 10.0f);
            ImGui.TableSetupColumn("Item##item");
            ImGui.TableSetupColumn("Pct##percentage", 0, 0.25f);

            ImGui.Indent(10.0f);
            foreach (var sortedEntry in sortedList)
            {
                ImGui.TableNextColumn();
                Helper.DrawScaledIcon(sortedEntry.Icon, IconSize);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(sortedEntry.Name);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{sortedEntry.Percentage:F2}%");

                ImGui.TableNextRow();
            }
            ImGui.Unindent(10.0f);

            ImGui.EndTable();
        }

        ImGui.EndTabItem();
    }

    public record SortedEntry(uint Icon, string Name, uint Count, double Percentage);
}
