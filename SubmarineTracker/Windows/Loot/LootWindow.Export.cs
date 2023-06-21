using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Loot;

public partial class LootWindow
{
    private bool ExportAll = true;
    private bool ExcludeDate = true;
    private Dictionary<ulong, bool> ExportSpecific = new();
    private string OutputPath = string.Empty;

    private static CsvConfiguration CsvConfig = new(CultureInfo.InvariantCulture) { HasHeaderRecord = false };

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

        [Format("s")]
        public DateTime Date { get; set; }

        public ExportLoot() {}

        public ExportLoot(Data.Loot.DetailedLoot loot)
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
        }
    }

    public sealed class ExportLootMap : ClassMap<ExportLoot>
    {
        public ExportLootMap(bool ignoreDate)
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
            ImGui.Checkbox("Export All", ref ExportAll);
            if (!ExportAll)
            {
                ImGui.Indent(10.0f);
                foreach (var (key, fc) in Submarines.KnownSubmarines)
                {
                    var text = $"{fc.Tag}@{fc.World}";
                    if (Configuration.UseCharacterName && fc.CharacterName != "")
                        text = $"{fc.CharacterName}@{fc.World}";

                    ExportSpecific.TryGetValue(key, out var check);
                    if (ImGui.Checkbox($"{text}##{key}", ref check))
                        ExportSpecific[key] = check;

                }
                ImGui.Unindent(10.0f);
            }
            ImGui.Checkbox("Exclude Date", ref ExcludeDate);
            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Output Folder:");
            ImGui.InputText("##OutputPathInput", ref OutputPath, 255);
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.FolderClosed))
                ImGui.OpenPopup("OutputPathDialog");

            if (ImGui.BeginPopup("OutputPathDialog"))
            {
                Plugin.FileDialogManager.OpenFolderDialog("Pick folder", (b, s) => { if (b) OutputPath = s; }, null, true);
                ImGui.EndPopup();
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            if (ImGui.Button("Export"))
            {
                // some of the corrupted loot data is still around, so we check that Rank is actually above 0
                var fcLootList = Submarines.KnownSubmarines
                                           .Where(kv => ExportAll || (ExportSpecific.TryGetValue(kv.Key, out var check) && check))
                                           .Select(kv => kv.Value.SubLoot)
                                           .SelectMany(kv => kv.Values)
                                           .SelectMany(subLoot => subLoot.Loot)
                                           .SelectMany(innerLoot => innerLoot.Value)
                                           .Where(detailedLoot => detailedLoot is { Valid: true, Rank: > 0 });

                if (Directory.Exists(OutputPath))
                {
                    try
                    {
                        var file = Path.Combine(OutputPath, $"{DateTime.Now:yyyy_MM_dd__HH_mm_ss}_dump.csv");
                        using var writer = new StreamWriter(file);
                        using var csv = new CsvWriter(writer, CsvConfig);

                        csv.Context.RegisterClassMap(new ExportLootMap(ExcludeDate));

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
                        PluginLog.Error(e.StackTrace);
                        Plugin.ChatGui.Print(Utils.ErrorMessage($"{e.Message}. For further information /xllog."));
                    }
                }
                else
                {
                    Plugin.ChatGui.Print(Utils.ErrorMessage("Invalid Path"));
                }
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.EndTabItem();
        }
    }
}
