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
            ImGui.TextColored(ImGuiColors.DalamudViolet, "FC Buttons:");
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox("Show 'All' Button", ref Configuration.ShowAll);
            ImGuiComponents.HelpMarker("Adds an 'All' button for easy overview of all FCs.\n" +
                                       "Note: Messy with too many FCs.");
            changed |= ImGui.Checkbox("Use Character Names", ref Configuration.UseCharacterName);
            ImGuiComponents.HelpMarker("Use character names instead of FC tags in the overview.\n" +
                                       "Be aware this option can lead to cut-off button text.");
            changed |= ImGui.Checkbox("Let Me Resize", ref Configuration.UserResize);
            ImGuiComponents.HelpMarker("This allows you to resize the FC buttons,\n" +
                                       "but stops them from automatically adjusting size.");
            ImGui.Unindent(10.0f);

            if (Configuration.ShowAll)
            {
                ImGuiHelpers.ScaledDummy(5.0f);
                ImGui.TextColored(ImGuiColors.DalamudViolet, "All:");
                ImGui.Indent(10.0f);
                changed |= ImGui.Checkbox("Show Route", ref Configuration.ShowRouteInAll);
                ImGuiComponents.HelpMarker("This option will also revert back to 1 FC in each column, if the window is too small.");
                changed |= ImGui.Checkbox("Show Date", ref Configuration.ShowDateInAll);
                ImGui.Unindent(10.0f);
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Overview:");
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox("Show Repair Status", ref Configuration.ShowOnlyLowest);
            if (Configuration.ShowOnlyLowest)
            {
                ImGui.Indent(10.0f);
                changed |= ImGui.Checkbox("Show Repair Status After Voyage", ref Configuration.ShowPrediction);
                ImGui.Unindent(10.0f);
            }

            changed |= ImGui.Checkbox("Show Return Time", ref Configuration.ShowTimeInOverview);
            if (Configuration.ShowTimeInOverview)
            {
                ImGui.Indent(10.0f);
                changed |= ImGui.Checkbox("Show Return Date", ref Configuration.UseDateTimeInstead);
                changed |= ImGui.Checkbox("Show Both Options", ref Configuration.ShowBothOptions);
                ImGui.Unindent(10.0f);
            }

            changed |= ImGui.Checkbox("Show Route", ref Configuration.ShowRouteInOverview);
            ImGui.Unindent(10.0f);

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Detailed View:");
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox("Show Extended Parts List", ref Configuration.ShowExtendedPartsList);
            ImGui.Unindent(10.0f);

            if (changed)
                Configuration.Save();

            ImGui.EndTabItem();
        }
    }
}
