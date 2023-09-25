using System.IO;
using System.Threading.Tasks;
using SubmarineTracker.Data;

using static SubmarineTracker.Data.Submarines;
namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private static readonly Submarine TestSub = new() { Name = "Apollo 11", ReturnTime = new DateTime(1969, 7, 21, 3, 15, 16) };
    private static readonly FcSubmarines TestFC = new() { CharacterName = "Buzz Aldrin", World = "Moon" };

    private bool About()
    {
        if (!ImGui.BeginTabItem($"{Loc.Localize("Config Tab - About", "About")}##About"))
            return false;

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextUnformatted(Loc.Localize("Config Tab Entry - Author", "Author:"));
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.ParsedGold, Plugin.Authors);

        ImGui.TextUnformatted(Loc.Localize("Config Tab Entry - Discord", "Discord:"));
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.ParsedGold, "@infi");

        ImGui.TextUnformatted(Loc.Localize("Config Tab Entry - Version", "Version:"));
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.ParsedOrange, Plugin.Version);

        #if DEBUG
        ImGuiHelpers.ScaledDummy(10.0f);

        ImGui.TextUnformatted("Debug:");
        ImGui.Indent(10.0f);
        if (ImGui.Button("Send Return"))
            Plugin.Notify.SendReturn(TestSub, TestFC);

        if (ImGui.Button("Send Repair"))
            Plugin.Notify.SendRepair(TestSub, TestFC);

        if (ImGui.Button("Dispatch Webhook"))
            Plugin.Notify.SendDispatchWebhook(TestSub, TestFC, (uint) ((DateTimeOffset) DateTime.Now.ToUniversalTime()).ToUnixTimeSeconds());

        if (ImGui.Button("Return Webhook"))
            Task.Run(() => Plugin.Notify.SendReturnWebhook(TestSub, TestFC));

        if (ImGui.Button("Test Full Upload"))
            Task.Run(() => Export.UploadFullExport(GenerateLootList()));

        if (ImGui.Button("Test Entry Upload"))
            Task.Run(() => Export.UploadEntry(GenerateLootList().Last()));

        if(ImGui.Button("Export Loc"))
        {
            var pwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Plugin.PluginInterface.AssemblyLocation.DirectoryName!);
            Loc.ExportLocalizable();
            Directory.SetCurrentDirectory(pwd);
        }
        ImGui.Unindent(10.0f);
        #endif

        ImGui.EndTabItem();

        return true;
    }

    private List<Data.Loot.DetailedLoot> GenerateLootList()
    {
        // some of the corrupted loot data is still around, so we check that Rank is above 0
        return KnownSubmarines
               .Select(kv => kv.Value.SubLoot)
               .SelectMany(kv => kv.Values)
               .SelectMany(subLoot => subLoot.Loot)
               .SelectMany(innerLoot => innerLoot.Value)
               .Where(detailedLoot => detailedLoot is { Valid: true, Rank: > 0 })
               .ToList();
    }
}
