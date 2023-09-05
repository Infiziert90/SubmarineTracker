using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using SubmarineTracker.Data;
using static SubmarineTracker.Data.Loot;

namespace SubmarineTracker.Windows.Loot;

public partial class LootWindow
{
    private bool ExportAll = true;
    private Dictionary<ulong, bool> ExportSpecific = new();

    private static CsvConfiguration CsvConfig = new(CultureInfo.InvariantCulture) { HasHeaderRecord = false };

    private static readonly DateTime ExportMinimalDate = new(2023, 6, 11);
    private DateTime ExportMinDate = ExportMinimalDate;
    private DateTime ExportMaxDate = DateTime.Now.AddDays(5);

    private string ExportMinString = "";
    private string ExportMaxString = "";

    public class ExportLoot
    {
        public uint Sector { get; set; }

        public uint Primary { get; set; }
        public ushort PrimaryCount { get; set; }
        public uint Additional { get; set; }
        public ushort AdditionalCount { get; set; }

        public int Rank { get; set; }
        public int Surv { get; set; }
        public int Ret { get; set; }
        public int Fav { get; set; }

        public uint PrimarySurvProc { get; set; }
        public uint AdditionalSurvProc { get; set; }
        public uint PrimaryRetProc { get; set; }
        public uint FavProc { get; set; }

        [Format("s")] public DateTime Date { get; set; }
        public string Hash { get; set; } = "";

        public ExportLoot() {}

        public ExportLoot(DetailedLoot loot)
        {
            Sector = loot.Sector;

            Primary = loot.Primary;
            PrimaryCount = loot.PrimaryCount;
            Additional = loot.Additional;
            AdditionalCount = loot.AdditionalCount;

            Rank = loot.Rank;
            Surv = loot.Surv;
            Ret = loot.Ret;
            Fav = loot.Fav;

            PrimarySurvProc = loot.PrimarySurvProc;
            AdditionalSurvProc = loot.AdditionalSurvProc;
            PrimaryRetProc = loot.PrimaryRetProc;
            FavProc = loot.FavProc;
            Date = loot.Date;

            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(Date.Ticks);
                writer.Write(Sector);
            }
            stream.Position = 0;

            using (var hash = SHA256.Create())
            {
                var result = hash.ComputeHash(stream);
                Hash = string.Join("", result.Select(b => $"{b:X2}"));
            }
        }
    }

    public sealed class ExportLootMap : ClassMap<ExportLoot>
    {
        public ExportLootMap(bool ignoreDate, bool ignoreHash)
        {
            Map(m => m.Sector).Index(0).Name("Sector");
            Map(m => m.Primary).Index(1).Name("Primary");
            Map(m => m.PrimaryCount).Index(2).Name("PrimaryCount");
            Map(m => m.Additional).Index(3).Name("Additional");
            Map(m => m.AdditionalCount).Index(4).Name("AdditionalCount");
            Map(m => m.Rank).Index(5).Name("Rank");
            Map(m => m.Surv).Index(6).Name("Surv");
            Map(m => m.Ret).Index(7).Name("Ret");
            Map(m => m.Fav).Index(8).Name("Fav");
            Map(m => m.PrimarySurvProc).Index(9).Name("PrimarySurvProc");
            Map(m => m.AdditionalSurvProc).Index(10).Name("AdditionalSurvProc");
            Map(m => m.PrimaryRetProc).Index(11).Name("PrimaryRetProc");
            Map(m => m.FavProc).Index(12).Name("FavProc");

            if (ignoreDate)
                Map(m => m.Date).Ignore();
            else
                Map(m => m.Date).Index(13).Name("Date");

            if (ignoreHash)
                Map(m => m.Hash).Ignore();
            else
                Map(m => m.Hash).Index(99).Name("Hash");
        }
    }

    private void ExportTab()
    {
        if (ImGui.BeginTabItem("Export"))
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
            var wip = "- Work in Progress -";
            var width = ImGui.GetWindowWidth();
            var textWidth = ImGui.CalcTextSize(wip).X;

            ImGui.SetCursorPosX((width - textWidth) * 0.5f);
            ImGui.TextColored(ImGuiColors.DalamudOrange, wip);
            ImGuiHelpers.ScaledDummy(10.0f);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5.0f);

            var changed = false;
            ImGui.Checkbox("Export All FCs", ref ExportAll);
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
            changed |= ImGui.Checkbox("Exclude Date", ref Configuration.ExportExcludeDate);
            changed |= ImGui.Checkbox("Exclude Hash", ref Configuration.ExportExcludeHash);

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "FromTo:");
            DateWidget.DatePickerWithInput("FromDate", 1, ref ExportMinString, ref ExportMinDate, Format);
            DateWidget.DatePickerWithInput("ToDate", 2, ref ExportMaxString, ref ExportMaxDate, Format, true);
            ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Recycle))
                ExportReset();

            if (DateWidget.Validate(ExportMinimalDate, ref ExportMinDate, ref ExportMaxDate))
                ExportRefresh();

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Output Folder:");
            changed |= ImGui.InputText("##OutputPathInput", ref Configuration.ExportOutputPath, 255);
            ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
            if (ImGuiComponents.IconButton(FontAwesomeIcon.FolderClosed))
                ImGui.OpenPopup("OutputPathDialog");

            if (ImGui.BeginPopup("OutputPathDialog"))
            {
                Plugin.FileDialogManager.OpenFolderDialog("Pick folder", (b, s) =>
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

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Export:");
            if (ImGui.Button("File"))
            {
                var fcLootList = BuildExportList();
                if (CheckList(ref fcLootList))
                    ExportToFile(fcLootList);
            }

            ImGui.SameLine();

            if (ImGui.Button("Clipboard"))
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
            Plugin.ChatGui.Print(Utils.ErrorMessage($"Nothing to export in the selected time frame."));
            return false;
        }

        return true;
    }

    private void ExportToClipboard(List<DetailedLoot> fcLootList)
    {
        try
        {
            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CsvConfig);

            csv.Context.RegisterClassMap(new ExportLootMap(Configuration.ExportExcludeDate, Configuration.ExportExcludeHash));

            csv.WriteHeader<ExportLoot>();
            csv.NextRecord();

            foreach (var detailedLoot in fcLootList)
            {
                csv.WriteRecord(new ExportLoot(detailedLoot));
                csv.NextRecord();
            }

            ImGui.SetClipboardText(writer.ToString());

            Plugin.ChatGui.Print(Utils.SuccessMessage($"Export to clipboard done."));
        }
        catch (Exception e)
        {
            PluginLog.Error(e.StackTrace ?? "No Stacktrace");
            Plugin.ChatGui.Print(Utils.ErrorMessage($"{e.Message}. For further information /xllog."));
        }
    }

    private void ExportToFile(List<DetailedLoot> fcLootList)
    {
        if (Directory.Exists(Configuration.ExportOutputPath))
        {
            try
            {
                var file = Path.Combine(Configuration.ExportOutputPath, $"{DateTime.Now:yyyy_MM_dd__HH_mm_ss}_dump.csv");
                using var writer = new StreamWriter(file);
                using var csv = new CsvWriter(writer, CsvConfig);

                csv.Context.RegisterClassMap(new ExportLootMap(Configuration.ExportExcludeDate, Configuration.ExportExcludeHash));

                csv.WriteHeader<ExportLoot>();
                csv.NextRecord();

                foreach (var detailedLoot in fcLootList)
                {
                    csv.WriteRecord(new ExportLoot(detailedLoot));
                    csv.NextRecord();
                }

                Plugin.ChatGui.Print(Utils.SuccessMessage($"Export done."));
                Plugin.ChatGui.Print(Utils.SuccessMessage($"Output: {file}"));
            }
            catch (Exception e)
            {
                PluginLog.Error(e.StackTrace ?? "No Stacktrace");
                Plugin.ChatGui.Print(Utils.ErrorMessage($"{e.Message}. For further information /xllog."));
            }
        }
        else
        {
            Plugin.ChatGui.Print(Utils.ErrorMessage("Invalid Path"));
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
