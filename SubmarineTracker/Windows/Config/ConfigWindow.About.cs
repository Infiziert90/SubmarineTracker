using System.Threading.Tasks;
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
                    Plugin.Notify.SendDispatchWebhook(TestSub, TestFC, (uint) ((DateTimeOffset) DateTime.Now.AddMinutes(10).ToUniversalTime()).ToUnixTimeSeconds());

                if (ImGui.Button("Return Webhook"))
                    Task.Run(() => Plugin.Notify.SendReturnWebhook(TestSub, TestFC));
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
}
