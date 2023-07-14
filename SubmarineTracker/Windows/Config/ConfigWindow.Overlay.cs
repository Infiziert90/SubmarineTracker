using Dalamud.Interface.Components;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void Overlay()
    {
        if (ImGui.BeginTabItem("Overlay"))
        {
            var changed = false;
            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Startup:");
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox("Open If Subs Have Returned", ref Configuration.OverlayStartUp);
            if (Configuration.OverlayStartUp)
            {
                ImGui.Indent(10.0f);
                ImGui.BeginDisabled();
                changed |= ImGui.Checkbox("Open unminimized", ref Configuration.OverlayUnminimized);
                ImGui.EndDisabled();
                ImGuiComponents.HelpMarker("Disabled for now");
                ImGui.Unindent(10.0f);
            }
            ImGui.Unindent(10.0f);

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Headers:");
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox("Show First Return Instead", ref Configuration.OverlayFirstReturn);
            changed |= ImGui.Checkbox("Show Return Date", ref Configuration.OverlayShowDate);
            ImGui.Unindent(10.0f);

            if (changed)
                Configuration.Save();

            ImGui.EndTabItem();
        }
    }
}
