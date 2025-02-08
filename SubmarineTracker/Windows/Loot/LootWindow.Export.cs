using System.IO;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Utility;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Loot;

public partial class LootWindow
{
    private bool ExportAll = true;
    private readonly Dictionary<ulong, bool> ExportSpecific = [];

    private static readonly DateTime ExportMinimalDate = new(2023, 6, 11);
    private DateTime ExportMinDate = ExportMinimalDate;
    private DateTime ExportMaxDate = DateTime.Now.AddDays(5);

    private string ExportMinString = "";
    private string ExportMaxString = "";

    private void ExportTab()
    {
        using var tabItem = ImRaii.TabItem($"{Language.LootTabExport}##Export");
        if (!tabItem.Success)
            return;

        var existingSubs = Plugin.DatabaseCache.GetSubmarines().Select(s => $"{s.Name} ({s.Build.FullIdentifier()})").ToArray();
        if (existingSubs.Length == 0)
        {
            Helper.NoData();
            return;
        }

        ImGuiHelpers.ScaledDummy(10.0f);

        var wip = Language.TermsWiP;
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize(wip).X) * 0.5f);
        Helper.TextColored(ImGuiColors.DalamudOrange, wip);
        ImGuiHelpers.ScaledDummy(10.0f);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5.0f);

        var changed = false;
        ImGui.Checkbox(Language.LootTabCheckboxExportAll, ref ExportAll);
        if (!ExportAll)
        {
            using var indent = ImRaii.PushIndent(10.0f);
            foreach (var (key, fc) in Plugin.DatabaseCache.GetFreeCompanies())
            {
                ExportSpecific.TryGetValue(key, out var check);
                if (ImGui.Checkbox($"{Plugin.NameConverter.GetName(fc)}##{key}", ref check))
                    ExportSpecific[key] = check;

            }
        }
        changed |= ImGui.Checkbox(Language.LootTabCheckboxExcludeDate, ref Plugin.Configuration.ExportExcludeDate);
        changed |= ImGui.Checkbox(Language.LootTabCheckboxExcludeHash, ref Plugin.Configuration.ExportExcludeHash);

        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.LootTabEntryFromToDateSelection);
        DateWidget.DatePickerWithInput("FromDate", 1, ref ExportMinString, ref ExportMinDate, Format);
        DateWidget.DatePickerWithInput("ToDate", 2, ref ExportMaxString, ref ExportMaxDate, Format, true);

        ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Recycle))
            ExportReset();

        if (DateWidget.Validate(ExportMinimalDate, ref ExportMinDate, ref ExportMaxDate))
            ExportRefresh();

        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.LootTabEntryOutputFolder);
        changed |= ImGui.InputText("##OutputPathInput", ref Plugin.Configuration.ExportOutputPath, 255);
        ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
        if (ImGuiComponents.IconButton(FontAwesomeIcon.FolderClosed))
            ImGui.OpenPopup("OutputPathDialog");

        using (var popup = ImRaii.Popup("OutputPathDialog"))
        {
            if (popup.Success)
            {
                Plugin.FileDialogManager.OpenFolderDialog(Language.LootTabTitlePickFolder, (b, s) =>
                {
                    if (b)
                    {
                        Plugin.Configuration.ExportOutputPath = s;
                        Plugin.Configuration.Save();
                    }
                }, null, true);
            }
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.LootTabEntryExport);
        if (ImGui.Button(Language.LootTabButtonFile))
        {
            var fcLootList = BuildExportList();
            if (CheckList(ref fcLootList))
                ExportToFile(fcLootList);
        }

        ImGui.SameLine();

        if (ImGui.Button(Language.LootTabButtonClipboard))
            ExportToClipboard();

        if (changed)
            Plugin.Configuration.Save();
    }

    private List<SubmarineTracker.Loot> BuildExportList()
    {
        var min = new DateTime(ExportMinDate.Year, ExportMinDate.Month, ExportMinDate.Day, 0, 0, 0);
        var max = new DateTime(ExportMaxDate.Year, ExportMaxDate.Month, ExportMaxDate.Day, 23, 59, 59);

        // some of the corrupted loot data is still around, so we check that Rank is above 0
        var lootList = Plugin.DatabaseCache.GetLoot();
        return Plugin.DatabaseCache.GetFreeCompanies()
                                   .Where(pair => ExportAll || (ExportSpecific.TryGetValue(pair.Key, out var check) && check))
                                   .Select(pair => lootList.Where(loot => loot.FreeCompanyId == pair.Key))
                                   .SelectMany(loot => loot)
                                   .Where(loot => loot is { Valid: true, Rank: > 0 })
                                   .Where(loot => loot.Date > min && loot.Date < max)
                                   .ToList();
    }

    private bool CheckList(ref List<SubmarineTracker.Loot> fcLootList)
    {
        if (fcLootList.Count == 0)
        {
            Plugin.ChatGui.Print(Utils.ErrorMessage(Language.LootExportErrorNothingFound));
            return false;
        }

        return true;
    }

    private void ExportToClipboard(List<SubmarineTracker.Loot> fcLootList)
    {
        var s = Export.ExportToString(fcLootList, Plugin.Configuration.ExportExcludeDate, Plugin.Configuration.ExportExcludeHash);
        if (s != string.Empty)
        {
            ImGui.SetClipboardText(s);
            Plugin.ChatGui.Print(Utils.SuccessMessage(Language.LootExportSuccessClipboard));
        }
    }

    private void ExportToFile(List<SubmarineTracker.Loot> fcLootList)
    {
        if (Directory.Exists(Plugin.Configuration.ExportOutputPath))
        {
            try
            {
                var file = Path.Combine(Plugin.Configuration.ExportOutputPath, $"{DateTime.Now:yyyy_MM_dd__HH_mm_ss}_dump.csv");
                var s = Export.ExportToString(fcLootList, Plugin.Configuration.ExportExcludeDate, Plugin.Configuration.ExportExcludeHash);

                if (s != string.Empty)
                {
                    if (File.Exists(file))
                        File.Delete(file);

                    File.WriteAllText(file, s);

                    Plugin.ChatGui.Print(Utils.SuccessMessage(Language.LootExportSuccessFile));
                    Plugin.ChatGui.Print(Utils.SuccessMessage(Language.LootExportSuccessOutputPath.Format(file)));
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, "Export went wrong.");
                Plugin.ChatGui.Print(Utils.ErrorMessage($"{ex.Message}. For further information /xllog."));
            }
        }
        else
        {
            Plugin.ChatGui.Print(Utils.ErrorMessage(Language.LootExportErrorInvalidPath));
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

    public void ExportToClipboard()
    {
        var fcLootList = BuildExportList();
        if (CheckList(ref fcLootList))
            ExportToClipboard(fcLootList);
    }
}
