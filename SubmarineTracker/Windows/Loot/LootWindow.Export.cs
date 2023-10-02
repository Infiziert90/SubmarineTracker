using System.IO;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Utility;
using SubmarineTracker.Data;
using static SubmarineTracker.Data.Loot;

namespace SubmarineTracker.Windows.Loot;

public partial class LootWindow
{
    private bool ExportAll = true;
    private Dictionary<ulong, bool> ExportSpecific = new();

    private static readonly DateTime ExportMinimalDate = new(2023, 6, 11);
    private DateTime ExportMinDate = ExportMinimalDate;
    private DateTime ExportMaxDate = DateTime.Now.AddDays(5);

    private string ExportMinString = "";
    private string ExportMaxString = "";

    private void ExportTab()
    {
        if (ImGui.BeginTabItem($"{Loc.Localize("Loot Tab - Export", "Export")}##Export"))
        {
            var existingSubs = Submarines.KnownSubmarines.Values
                                         .SelectMany(fc => fc.Submarines.Select(s => $"{s.Name} ({s.Build.FullIdentifier()})"))
                                         .ToArray();
            if (!existingSubs.Any())
            {
                Helper.NoData();
                ImGui.EndTabItem();
                return;
            }

            ImGuiHelpers.ScaledDummy(10.0f);
            var wip = Loc.Localize("Terms - WiP", "- Work in Progress -");
            var width = ImGui.GetWindowWidth();
            var textWidth = ImGui.CalcTextSize(wip).X;

            ImGui.SetCursorPosX((width - textWidth) * 0.5f);
            ImGui.TextColored(ImGuiColors.DalamudOrange, wip);
            ImGuiHelpers.ScaledDummy(10.0f);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5.0f);

            var changed = false;
            ImGui.Checkbox(Loc.Localize("Loot Tab Checkbox - Export All", "Export All FCs"), ref ExportAll);
            if (!ExportAll)
            {
                ImGui.Indent(10.0f);
                foreach (var (key, fc) in Submarines.KnownSubmarines)
                {
                    ExportSpecific.TryGetValue(key, out var check);
                    if (ImGui.Checkbox($"{Helper.GetFCName(fc)}##{key}", ref check))
                        ExportSpecific[key] = check;

                }
                ImGui.Unindent(10.0f);
            }
            changed |= ImGui.Checkbox(Loc.Localize("Loot Tab Checkbox - Exclude Date", "Exclude Date"), ref Configuration.ExportExcludeDate);
            changed |= ImGui.Checkbox(Loc.Localize("Loot Tab Checkbox - Exclude Hash", "Exclude Hash"), ref Configuration.ExportExcludeHash);

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Loot Tab Entry - FromTo Date Selection", "FromTo:"));
            DateWidget.DatePickerWithInput("FromDate", 1, ref ExportMinString, ref ExportMinDate, Format);
            DateWidget.DatePickerWithInput("ToDate", 2, ref ExportMaxString, ref ExportMaxDate, Format, true);
            ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Recycle))
                ExportReset();

            if (DateWidget.Validate(ExportMinimalDate, ref ExportMinDate, ref ExportMaxDate))
                ExportRefresh();

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Loot Tab Entry - Output Folder", "Output Folder:"));
            changed |= ImGui.InputText("##OutputPathInput", ref Configuration.ExportOutputPath, 255);
            ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
            if (ImGuiComponents.IconButton(FontAwesomeIcon.FolderClosed))
                ImGui.OpenPopup("OutputPathDialog");

            if (ImGui.BeginPopup("OutputPathDialog"))
            {
                Plugin.FileDialogManager.OpenFolderDialog(Loc.Localize("Loot Tab Title - Pick Folder", "Pick a folder"), (b, s) =>
                {
                    if (b)
                    {
                        Configuration.ExportOutputPath = s;
                        Configuration.Save();
                    }
                }, null, true);
                ImGui.EndPopup();
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Loot Tab Entry - Export", "Export:"));
            if (ImGui.Button(Loc.Localize("Loot Tab Button - File", "File")))
            {
                var fcLootList = BuildExportList();
                if (CheckList(ref fcLootList))
                    ExportToFile(fcLootList);
            }

            ImGui.SameLine();

            if (ImGui.Button(Loc.Localize("Loot Tab Button - Clipboard", "Clipboard")))
            {
                var fcLootList = BuildExportList();
                if (CheckList(ref fcLootList))
                    ExportToClipboard(fcLootList);
            }

            if (changed)
                Configuration.Save();

            ImGui.EndTabItem();
        }
    }

    private List<DetailedLoot> BuildExportList()
    {
        var min = new DateTime(ExportMinDate.Year, ExportMinDate.Month, ExportMinDate.Day, 0, 0, 0);
        var max = new DateTime(ExportMaxDate.Year, ExportMaxDate.Month, ExportMaxDate.Day, 23, 59, 59);

        // some of the corrupted loot data is still around, so we check that Rank is above 0
        return Submarines.KnownSubmarines
                                   .Where(kv => ExportAll || (ExportSpecific.TryGetValue(kv.Key, out var check) && check))
                                   .Select(kv => kv.Value.SubLoot)
                                   .SelectMany(kv => kv.Values)
                                   .SelectMany(subLoot => subLoot.Loot)
                                   .SelectMany(innerLoot => innerLoot.Value)
                                   .Where(detailedLoot => detailedLoot is { Valid: true, Rank: > 0 })
                                   .Where(detailedLoot => detailedLoot.Date > min && detailedLoot.Date < max)
                                   .ToList();
    }

    private bool CheckList(ref List<DetailedLoot> fcLootList)
    {
        if (!fcLootList.Any())
        {
            Plugin.ChatGui.Print(Utils.ErrorMessage(Loc.Localize("Loot Export Error - Nothing Found", "Nothing to export in the selected time frame.")));
            return false;
        }

        return true;
    }

    private void ExportToClipboard(List<DetailedLoot> fcLootList)
    {
        var s = Export.ExportToString(fcLootList, Configuration.ExportExcludeDate, Configuration.ExportExcludeHash);
        if (s != string.Empty)
        {
            ImGui.SetClipboardText(s);
            Plugin.ChatGui.Print(Utils.SuccessMessage(Loc.Localize("Loot Export Success - Clipboard", "Successfully exported to clipboard.")));
        }
    }

    private void ExportToFile(List<DetailedLoot> fcLootList)
    {
        if (Directory.Exists(Configuration.ExportOutputPath))
        {
            try
            {
                var file = Path.Combine(Configuration.ExportOutputPath, $"{DateTime.Now:yyyy_MM_dd__HH_mm_ss}_dump.csv");
                var s = Export.ExportToString(fcLootList, Configuration.ExportExcludeDate, Configuration.ExportExcludeHash);

                if (s != string.Empty)
                {
                    if (File.Exists(file))
                        File.Delete(file);

                    File.WriteAllText(file, s);

                    Plugin.ChatGui.Print(Utils.SuccessMessage(Loc.Localize("Loot Export Success - File", "Export done.")));
                    Plugin.ChatGui.Print(Utils.SuccessMessage(Loc.Localize("Loot Export Success - Output Path", "Output Path: {0}").Format(file)));
                }
            }
            catch (Exception e)
            {
                Plugin.Log.Error(e.StackTrace ?? "No Stacktrace");
                Plugin.ChatGui.Print(Utils.ErrorMessage($"{e.Message}. For further information /xllog."));
            }
        }
        else
        {
            Plugin.ChatGui.Print(Utils.ErrorMessage(Loc.Localize("Loot Export Error - Invalid Path", "Invalid path.")));
        }
    }

    private void ExportRefresh()
    {
        ExportMinString = ExportMinDate.ToString(Format);
        ExportMaxString = ExportMaxDate.ToString(Format);
    }

    public void ExportReset()
    {
        ExportMinDate = ExportMinimalDate;
        ExportMaxDate = DateTime.Now;

        ExportRefresh();
    }
}
