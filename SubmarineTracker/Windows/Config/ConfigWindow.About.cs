using System.Threading.Tasks;
using SubmarineTracker.Data;
using static SubmarineTracker.Data.Submarines;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private static readonly Submarine TestSub = new() { Name = "Apollo 11", ReturnTime = new DateTime(1969, 7, 21, 3, 15, 16) };
    private static readonly FcSubmarines TestFC = new() { CharacterName = "Buzz Aldrin", World = "Moon" };

    private void About()
    {
        if (ImGui.BeginTabItem("About"))
        {
            var buttonHeight = ImGui.CalcTextSize("RRRR").Y + (20.0f * ImGuiHelpers.GlobalScale);
            if (ImGui.BeginChild("AboutContent", new Vector2(0, -buttonHeight)))
            {
                ImGuiHelpers.ScaledDummy(5.0f);

                ImGui.TextUnformatted("Author:");
                ImGui.SameLine();
                ImGui.TextColored(ImGuiColors.ParsedGold, Plugin.Authors);

                ImGui.TextUnformatted("Discord:");
                ImGui.SameLine();
                ImGui.TextColored(ImGuiColors.ParsedGold, "@infi");

                ImGui.TextUnformatted("Version:");
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
                ImGui.Unindent(10.0f);
                #endif
            }

            ImGui.EndChild();

            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(1.0f);

            if (ImGui.BeginChild("AboutBottomBar", new Vector2(0, 0), false, 0))
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedBlue);
                if (ImGui.Button("Discord Thread"))
                    Dalamud.Utility.Util.OpenLink("https://canary.discord.com/channels/581875019861328007/1094255662860599428");
                ImGui.PopStyleColor();

                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.DPSRed);
                if (ImGui.Button("Issues"))
                    Dalamud.Utility.Util.OpenLink("https://github.com/Infiziert90/SubmarineTracker/issues");
                ImGui.PopStyleColor();

                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Button, Helper.CustomFullyDone);
                if (ImGui.Button("Ko-Fi Tip"))
                    Dalamud.Utility.Util.OpenLink("https://ko-fi.com/infiii");
                ImGui.PopStyleColor();
            }

            ImGui.EndChild();

            ImGui.EndTabItem();
        }
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
