namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private static void About()
    {
        if (ImGui.BeginTabItem("About"))
        {
            if (ImGui.BeginChild("AboutContent", new Vector2(0, -(50 * ImGuiHelpers.GlobalScale))))
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
            }

            ImGui.EndChild();

            ImGuiHelpers.ScaledDummy(5);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5);

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
            }

            ImGui.EndChild();

            ImGui.EndTabItem();
        }
    }
}
