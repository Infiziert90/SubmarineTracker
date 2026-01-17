using System.IO;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private static readonly Submarine TestSub = new() { Name = "Apollo 11", ReturnTime = new DateTime(1969, 7, 21, 3, 15, 16) };
    private static readonly FreeCompany TestFC = new() { CharacterName = "Buzz Aldrin", World = "Moon", Tag = "Apollo" };

    private string InputPathItem = string.Empty;
    private string InputPath = string.Empty;
    private ulong Worth;
    private int Records;
    private bool ImportDone;


    private bool About()
    {
        using var tabItem = ImRaii.TabItem($"{Language.ConfigTabAbout}##About");
        if (!tabItem.Success)
            return false;

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextUnformatted(Language.ConfigTabEntryAuthor);
        ImGui.SameLine();
        Helper.TextColored(ImGuiColors.ParsedGold, Plugin.PluginInterface.Manifest.Author);

        ImGui.TextUnformatted(Language.ConfigTabEntryDiscord);
        ImGui.SameLine();
        Helper.TextColored(ImGuiColors.ParsedGold, "@infi");

        ImGui.TextUnformatted(Language.ConfigTabEntryVersion);
        ImGui.SameLine();
        Helper.TextColored(ImGuiColors.ParsedOrange, Plugin.PluginInterface.Manifest.AssemblyVersion.ToString());

        ImGui.TextUnformatted(Language.ConfigTabEntryLocalization);
        ImGui.SameLine();
        Helper.TextColored(ImGuiColors.ParsedOrange, Language.ConfigAboutTranslationCommunity);
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(Language.ConfigAboutTranslationTooltip);

        #if DEBUG
        ImGuiHelpers.ScaledDummy(10.0f);

        ImGui.TextUnformatted("Debug:");
        using var indent = ImRaii.PushIndent(10.0f);
        if (ImGui.Button("Send Return"))
            Plugin.Notify.SendReturn(TestSub, TestFC);

        if (ImGui.Button("Send Repair"))
            Plugin.Notify.SendRepair(TestSub, TestFC);

        if (ImGui.Button("Dispatch Webhook"))
            Plugin.Notify.SendDispatchWebhook(TestSub, TestFC, (uint) ((DateTimeOffset) DateTime.Now.ToUniversalTime()).ToUnixTimeSeconds());

        if (ImGui.Button("Return Webhook"))
            Task.Run(() => Plugin.Notify.SendReturnWebhook(TestSub, TestFC));

        if (ImGui.Button("Test Entry Upload"))
            Task.Run(() => Export.UploadLoot(GenerateLootList().Last()));

        if (ImGui.Button("Calculate All Routes"))
        {
            var pwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Plugin.PluginInterface.AssemblyLocation.DirectoryName!);
            Importer.Export();
            Directory.SetCurrentDirectory(pwd);
        }

        Helper.TextColored(ImGuiColors.DalamudViolet, "Input Item File:");
        ImGui.InputText("##InputPathItem", ref InputPathItem, 255);
        ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
        if (ImGuiComponents.IconButton("##itemPicker", FontAwesomeIcon.FolderClosed))
            ImGui.OpenPopup("InputPathDialogItem");

        using (var popup = ImRaii.Popup("InputPathDialogItem"))
        {
            if (popup.Success)
                Plugin.FileDialogManager.OpenFileDialog("Pick the item file", ".json", (b, s) => { if (b) InputPathItem = s[0]; }, 1);
        }

        if (ImGui.Button("Calculate Item Detailed"))
        {
            Importer.ExportDetailed(InputPathItem);
        }

        Helper.TextColored(ImGuiColors.DalamudViolet, "Input Folder:");
        ImGui.InputText("##InputPath", ref InputPath, 255);
        ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
        if (ImGuiComponents.IconButton("##folderPicker", FontAwesomeIcon.FolderClosed))
            ImGui.OpenPopup("InputPathDialog");

        using (var popup = ImRaii.Popup("InputPathDialog"))
        {
            if (popup.Success)
                Plugin.FileDialogManager.OpenFolderDialog("Pick the folder", (b, s) => { if (b) InputPath = s; }, null, true);
        }

        if (ImGui.Button("Import Data"))
        {
            Task.Run(() =>
            {
                ImportDone = false;

                var profile = Plugin.Configuration.CustomLootProfiles["Default"];
                foreach (var file in new DirectoryInfo(InputPath).EnumerateFiles())
                {
                    foreach (var loot in Export.Import(file))
                    {
                        Records += 1;

                        if (profile.TryGetValue(loot.Primary, out var value))
                            Worth += (ulong)value * loot.PrimaryCount;
                        else
                            Worth += Sheets.GetItem(loot.Primary).PriceLow * loot.PrimaryCount;

                        if (loot.Additional > 0)
                        {
                            if (profile.TryGetValue(loot.Additional, out value))
                                Worth += (ulong)value * loot.AdditionalCount;
                            else
                                Worth += Sheets.GetItem(loot.Additional).PriceLow * loot.AdditionalCount;
                        }
                    }
                }

                ImportDone = true;
            });

        }

        if (Worth != 0)
        {
            Helper.TextColored(ImGuiColors.ParsedOrange, $"Voyages recorded: {Records:N0}");
            Helper.TextColored(ImGuiColors.ParsedOrange, $"Worth of all items: {Worth:N0} Gil");
            Helper.TextColored(ImGuiColors.ParsedOrange, $"Import Done?: {ImportDone}");
        }
        #endif

        return true;
    }

    private List<SubmarineTracker.Loot> GenerateLootList()
    {
        // some of the corrupted loot data is still around, so we check that Rank is above 0
        return Plugin.DatabaseCache.GetLoot().Where(loot => loot is { Valid: true, Rank: > 0 }).ToList();
    }
}
