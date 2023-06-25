using Dalamud.Interface.Components;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows;

public static class Helper
{
    public static void NoData()
    {
        ImGuiHelpers.ScaledDummy(10.0f);
        WrappedError("No data found for this character's FC\n" +
                     "Please visit your Company Workshop and access Submersible Management at the Voyage Control Panel.");
    }

    public static string BuildNameHeader(Submarines.FcSubmarines fc, bool useCharacterName)
    {
        return !useCharacterName ? $"{fc.Tag}@{fc.World}" : $"{fc.CharacterName}@{fc.World}";;
    }

    public static void WrappedError(string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudOrange);
        ImGui.TextWrapped(text);
        ImGui.PopStyleColor();
    }

    public static void MainMenuIcon(Plugin plugin)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        ImGui.SameLine(avail - (60.0f * ImGuiHelpers.GlobalScale));

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Sync))
        {
            Storage.Refresh = true;
            CharacterConfiguration.LoadCharacters();
            plugin.LoadFCOrder();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Reload all saved FCs from your disk");

        ImGui.SameLine(avail - (33.0f * ImGuiHelpers.GlobalScale));

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
            plugin.DrawConfigUI();

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Open the config menu");
    }
}
