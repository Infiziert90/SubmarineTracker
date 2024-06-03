using System.IO;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private static readonly Submarine TestSub = new() { Name = "Apollo 11", ReturnTime = new DateTime(1969, 7, 21, 3, 15, 16) };
    private static readonly FreeCompany TestFC = new() { CharacterName = "Buzz Aldrin", World = "Moon", Tag = "Apollo" };

    private string InputPath = string.Empty;
    private ulong Worth;
    private int Records;


    private bool About()
    {
        if (!ImGui.BeginTabItem($"{Loc.Localize("Config Tab - About", "About")}##About"))
            return false;

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextUnformatted(Loc.Localize("Config Tab Entry - Author", "Author:"));
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.ParsedGold, Plugin.PluginInterface.Manifest.Author);

        ImGui.TextUnformatted(Loc.Localize("Config Tab Entry - Discord", "Discord:"));
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.ParsedGold, "@infi");

        ImGui.TextUnformatted(Loc.Localize("Config Tab Entry - Version", "Version:"));
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.ParsedOrange, Plugin.PluginInterface.Manifest.AssemblyVersion.ToString());

        ImGui.TextUnformatted(Loc.Localize("Config Tab Entry - Localization", "Localization:"));
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.ParsedOrange, Loc.Localize("Config About Translation - Community", "Community"));
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(Loc.Localize("Config About Translation - Tooltip", "All localizations are done by the community.\nIf you want to help improve them, please visit Crowdin"));

        #if DEBUG
        ImGuiHelpers.ScaledDummy(10.0f);

        ImGui.TextUnformatted("Debug:");
        ImGuiHelpers.ScaledIndent(10.0f);
        if (ImGui.Button("Send Return"))
            Plugin.Notify.SendReturn(TestSub, TestFC);

        if (ImGui.Button("Send Repair"))
            Plugin.Notify.SendRepair(TestSub, TestFC);

        if (ImGui.Button("Dispatch Webhook"))
            Plugin.Notify.SendDispatchWebhook(TestSub, TestFC, (uint) ((DateTimeOffset) DateTime.Now.ToUniversalTime()).ToUnixTimeSeconds());

        if (ImGui.Button("Return Webhook"))
            Task.Run(() => Plugin.Notify.SendReturnWebhook(TestSub, TestFC));

        if (ImGui.Button("Test Entry Upload"))
            Task.Run(() => Export.UploadEntry(GenerateLootList().Last()));

        if (ImGui.Button("Export Loc"))
        {
            var pwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Plugin.PluginInterface.AssemblyLocation.DirectoryName!);
            Plugin.Localization.ExportLocalizable();
            Directory.SetCurrentDirectory(pwd);
        }

        if (ImGui.Button("Calculate All Routes"))
        {
            var pwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Plugin.PluginInterface.AssemblyLocation.DirectoryName!);
            Importer.Export();
            Directory.SetCurrentDirectory(pwd);
        }

        if (ImGui.Button("Calculate Item Detailed"))
        {
            var pwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Plugin.PluginInterface.AssemblyLocation.DirectoryName!);
            Importer.ExportDetailed();
            Directory.SetCurrentDirectory(pwd);
        }

        ImGui.TextColored(ImGuiColors.DalamudViolet, "Input Folder:");
        ImGui.InputText("##InputPath", ref InputPath, 255);
        ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
        if (ImGuiComponents.IconButton(FontAwesomeIcon.FolderClosed))
            ImGui.OpenPopup("InputPathDialog");

        if (ImGui.BeginPopup("InputPathDialog"))
        {
            Plugin.FileDialogManager.OpenFolderDialog(
                "Pick the folder",
                (b, s) => { if (b) InputPath = s; },
                null,
                true);

            ImGui.EndPopup();
        }

        if (ImGui.Button("Import Data"))
        {
            Task.Run(() =>
            {
                var dict = Export.Import(InputPath);

                var profile = Plugin.Configuration.CustomLootProfiles["Default"];
                foreach (var loot in dict.Values)
                {
                    if (profile.TryGetValue(loot.Primary, out var value))
                        Worth += (ulong) value * loot.PrimaryCount;
                    else
                        Worth += ItemSheet.GetRow(loot.Primary)!.PriceLow * loot.PrimaryCount;

                    if (loot.Additional > 0)
                    {
                        if (profile.TryGetValue(loot.Additional, out value))
                            Worth += (ulong) value * loot.AdditionalCount;
                        else
                            Worth += ItemSheet.GetRow(loot.Additional)!.PriceLow * loot.AdditionalCount;
                    }
                }

                Records = dict.Count;
            });

        }

        if (Worth != 0)
        {
            ImGui.TextColored(ImGuiColors.ParsedOrange, $"Voyages recorded: {Records:N0}");
            ImGui.TextColored(ImGuiColors.ParsedOrange, $"Worth of all items: {Worth:N0} Gil");
        }
        ImGuiHelpers.ScaledIndent(-10.0f);
        #endif

        ImGui.EndTabItem();

        return true;
    }

    private List<SubmarineTracker.Loot> GenerateLootList()
    {
        // some of the corrupted loot data is still around, so we check that Rank is above 0
        return Plugin.DatabaseCache.GetLoot()
               .Where(loot => loot is { Valid: true, Rank: > 0 })
               .ToList();
    }
}
