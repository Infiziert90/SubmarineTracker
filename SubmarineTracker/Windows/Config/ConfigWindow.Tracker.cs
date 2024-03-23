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
            ImGuiHelpers.ScaledIndent(10.0f);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Show All", "Show 'All' Button"), ref Plugin.Configuration.ShowAll);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Show All", "Adds an 'All' button for easy overview of all FCs.\nNote: Messy with too many FCs."));
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Let Me Resize", "Let Me Resize"), ref Plugin.Configuration.UserResize);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Let Me Resize", "This allows you to resize the FC buttons,\nbut stops them from automatically adjusting size."));
            ImGuiHelpers.ScaledIndent(-10.0f);

            if (Plugin.Configuration.ShowAll)
            {
                ImGuiHelpers.ScaledDummy(5.0f);
                ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - All Section", "All Section:"));
                ImGuiHelpers.ScaledIndent(10.0f);
                changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Show Route", "Show Route"), ref Plugin.Configuration.ShowRouteInAll);
                ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Show Route", "This option will also revert back to 1 FC in each column, if the window is too small."));
                changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Show Date", "Show Date"), ref Plugin.Configuration.ShowDateInAll);
                ImGuiHelpers.ScaledIndent(-10.0f);
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Overview", "Overview:"));
            ImGuiHelpers.ScaledIndent(10.0f);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Repair Status", "Show Repair Status"), ref Plugin.Configuration.ShowOnlyLowest);
            if (Plugin.Configuration.ShowOnlyLowest)
            {
                ImGuiHelpers.ScaledIndent(10.0f);
                changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Repair After Voyage", "Show Repair Status After Voyage Return"), ref Plugin.Configuration.ShowPrediction);
                ImGuiHelpers.ScaledIndent(-10.0f);
            }

            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Return Time", "Show Return Time"), ref Plugin.Configuration.ShowTimeInOverview);
            if (Plugin.Configuration.ShowTimeInOverview)
            {
                ImGuiHelpers.ScaledIndent(10.0f);
                changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Return Date", "Show Return Date"), ref Plugin.Configuration.UseDateTimeInstead);
                changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Time&Date Show Both", "Show Both Options"), ref Plugin.Configuration.ShowBothOptions);
                ImGuiHelpers.ScaledIndent(-10.0f);
            }

            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Show Route", "Show Route"), ref Plugin.Configuration.ShowRouteInOverview);
            ImGuiHelpers.ScaledIndent(-10.0f);

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Detailed View", "Detailed View:"));
            ImGuiHelpers.ScaledIndent(10.0f);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Extended Parts", "Show Extended Parts List"), ref Plugin.Configuration.ShowExtendedPartsList);
            ImGuiHelpers.ScaledIndent(-10.0f);

            if (changed)
                Plugin.Configuration.Save();

            ImGui.EndTabItem();
        }
    }
}
