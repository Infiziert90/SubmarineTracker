using Dalamud.Interface.Components;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void Tracker()
    {
        if (ImGui.BeginTabItem("Tracker"))
        {
            ImGuiHelpers.ScaledDummy(5.0f);

            var changed = false;
            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - FC Buttons", "FC Buttons:"));
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Show All", "Show 'All' Button"), ref Configuration.ShowAll);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Show All", "Adds an 'All' button for easy overview of all FCs.\nNote: Messy with too many FCs."));
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Only FC Tag", "Only FC Tag"), ref Configuration.OnlyFCTag);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Only FC Tag", "Shows only the FC tag without @World."));
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Use Character Name", "Use Character Name"), ref Configuration.UseCharacterName);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Use Character Name", "Use character name instead of FC tag.\nBe aware this option can lead to cut-off text."));
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Anonymize Names", "Anonymize Name & Tag"), ref Configuration.AnonNames);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Anonymize Names", "Anonymize all names and tags for taking screenshots.\nBe aware this option can lead to cut-off text."));
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Let Me Resize", "Let Me Resize"), ref Configuration.UserResize);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Let Me Resize", "This allows you to resize the FC buttons,\nbut stops them from automatically adjusting size."));
            ImGui.Unindent(10.0f);

            if (Configuration.ShowAll)
            {
                ImGuiHelpers.ScaledDummy(5.0f);
                ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - All Section", "All Section:"));
                ImGui.Indent(10.0f);
                changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Show Route", "Show Route"), ref Configuration.ShowRouteInAll);
                ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Show Route", "This option will also revert back to 1 FC in each column, if the window is too small."));
                changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Show Date", "Show Date"), ref Configuration.ShowDateInAll);
                ImGui.Unindent(10.0f);
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Overview", "Overview:"));
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Repair Status", "Show Repair Status"), ref Configuration.ShowOnlyLowest);
            if (Configuration.ShowOnlyLowest)
            {
                ImGui.Indent(10.0f);
                changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Repair After Voyage", "Show Repair Status After Voyage Return"), ref Configuration.ShowPrediction);
                ImGui.Unindent(10.0f);
            }

            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Return Time", "Show Return Time"), ref Configuration.ShowTimeInOverview);
            if (Configuration.ShowTimeInOverview)
            {
                ImGui.Indent(10.0f);
                changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Return Date", "Show Return Date"), ref Configuration.UseDateTimeInstead);
                changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Time&Date Show Both", "Show Both Options"), ref Configuration.ShowBothOptions);
                ImGui.Unindent(10.0f);
            }

            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Show Route", "Show Route"), ref Configuration.ShowRouteInOverview);
            ImGui.Unindent(10.0f);

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Detailed View", "Detailed View:"));
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Extended Parts", "Show Extended Parts List"), ref Configuration.ShowExtendedPartsList);
            ImGui.Unindent(10.0f);

            if (changed)
                Configuration.Save();

            ImGui.EndTabItem();
        }
    }
}
