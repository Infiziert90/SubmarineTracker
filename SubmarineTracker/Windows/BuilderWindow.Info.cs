using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;

namespace SubmarineTracker.Windows;
public partial class BuilderWindow
{
    private static bool InfoTab()
    {
        var open = ImGui.BeginTabItem("Info");
        if (open)
        {
            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.TextColored(ImGuiColors.DalamudViolet, "How are these stats calculated?");
            ImGui.TextUnformatted("All calculations are based on sheets from the game");
            ImGuiHelpers.ScaledDummy(10.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "How are these breakpoints calculated?");
            ImGui.TextWrapped("The range breakpoint is calculated using the same method as the game. However all other breakpoints are calculated off of community data gathered from the submarine discord.\nSpecial thanks to Mystic Spirit for maintaining the current sheet.");
            ImGuiHelpers.ScaledDummy(10.0f);

            var spacing = ImGui.CalcTextSize("Optimal").X + 20.0f;

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Breakpoints:");
            ImGui.TextUnformatted("T2");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Surveillance required for a chance to get loot from Tier 2");
            ImGui.TextUnformatted("T3");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Surveillance required for a chance to get loot from Tier 3");
            ImGui.TextUnformatted("Normal");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Retrieval required for normal retrieval level.");
            ImGui.TextUnformatted("Optimal");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Retrieval required for optimal retrieval level");
            ImGui.TextUnformatted("Favor");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Favor required for chance to get two items from a sector");

            ImGuiHelpers.ScaledDummy(10.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Colors:");
            ImGui.TextUnformatted("White");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Nothing selected or no data available");
            ImGui.TextColored(ImGuiColors.ParsedGold,"Gold");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Requirement exceeded");
            ImGui.TextColored(ImGuiColors.HealerGreen,"Green");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("T3/Optimal/Favor reached");
            ImGui.TextColored(ImGuiColors.ParsedPink,"Pink");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("T2/Normal reached, followed by T3/Optimal");
            ImGui.TextColored(ImGuiColors.DalamudRed,"Red");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Requirement not fulfilled, followed by requirement");
        }
        ImGui.EndTabItem();

        return open;
    }
}
