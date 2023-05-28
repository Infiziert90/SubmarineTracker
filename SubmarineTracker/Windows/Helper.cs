namespace SubmarineTracker.Windows;

public static class Helper
{
    public static void NoData()
    {
        ImGuiHelpers.ScaledDummy(10.0f);
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudOrange);
        ImGui.TextWrapped("No data found for this character's FC\n" +
                          "Please visit your Company Workshop and access Submersible Management at the Voyage Control Panel.");
        ImGui.PopStyleColor();
    }
}
