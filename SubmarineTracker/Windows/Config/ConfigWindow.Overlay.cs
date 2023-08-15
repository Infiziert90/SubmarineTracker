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

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Open:");
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox("On Startup", ref Configuration.OverlayStartUp);
            ImGuiComponents.HelpMarker("Opens the overlay on startup, if at least one sub is done");
            changed |= ImGui.Checkbox("On Return", ref Configuration.OverlayAlwaysOpen);
            ImGuiComponents.HelpMarker("Always opens the overlay if one submarine returns");
            if (Configuration.OverlayStartUp || Configuration.OverlayAlwaysOpen)
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

            ImGui.TextColored(ImGuiColors.DalamudViolet, "General:");
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox("Use Character Name", ref Configuration.OverlayCharacterName);
            ImGuiComponents.HelpMarker("Use character name instead of FC tag.\nBe aware this option can lead to cut-off text.");
            changed |= ImGui.Checkbox("Show Rank", ref Configuration.OverlayShowRank);
            changed |= ImGui.Checkbox("Show Build", ref Configuration.OverlayShowBuild);
            changed |= ImGui.Checkbox("Show As Date", ref Configuration.OverlayShowDate);
            changed |= ImGui.Checkbox("Show First Return Time", ref Configuration.OverlayFirstReturn);
            if (ImGui.Checkbox("Sort By Lowest Time", ref Configuration.OverlaySort))
            {
                changed = true;
                Configuration.OverlaySortReverse = false;
            }

            if (Configuration.OverlaySort)
            {
                ImGui.Indent(10.0f);
                changed |= ImGui.Checkbox("Reverse Sort", ref Configuration.OverlaySortReverse);
                ImGui.Unindent(10.0f);
            }
            changed |= ImGui.Checkbox("Only Show Returned", ref Configuration.OverlayOnlyReturned);
            ImGui.Unindent(10.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Window:");
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox("Lock Size", ref Configuration.OverlayLockSize);
            changed |= ImGui.Checkbox("Lock Position", ref Configuration.OverlayLockLocation);
            ImGui.Unindent(10.0f);

            if (changed)
                Configuration.Save();

            ImGui.EndTabItem();
        }
    }
}
