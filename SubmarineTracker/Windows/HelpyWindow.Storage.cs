using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows;

public partial class HelpyWindow
{
    private void StorageTab(Submarines.FcSubmarines fcSub)
    {
        if (ImGui.BeginTabItem("Storage"))
        {
            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.TextColored(ImGuiColors.ParsedOrange, "Coming soon ...");
        }
        ImGui.EndTabItem();
    }
}
