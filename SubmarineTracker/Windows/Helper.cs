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

    public static string GenerateVoyageText(Submarines.Submarine sub)
    {
        var time = "No Voyage";
        if (sub.IsOnVoyage())
        {
            time = "Done";

            var returnTime = sub.LeftoverTime();
            if (returnTime.TotalSeconds > 0)
                time = $"{Utils.ToTime(returnTime)}";
        }

        return time;
    }

    public static void MainMenuIcon(Plugin plugin)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        ImGui.SameLine(avail - (60.0f * ImGuiHelpers.GlobalScale));

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Sync))
        {
            Storage.Refresh = true;
            plugin.ConfigurationBase.Load();
            plugin.LoadFCOrder();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Reload all data from disk and refresh all cached data");

        ImGui.SameLine(avail - (33.0f * ImGuiHelpers.GlobalScale));

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
            plugin.DrawConfigUI();

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Open the config menu");
    }

    public static void DrawArrows(ref int selected, int length, int id = 0)
    {
        ImGui.SameLine();
        if (selected == 0) ImGui.BeginDisabled();
        if (ImGuiComponents.IconButton(id, FontAwesomeIcon.ArrowLeft)) selected--;
        if (selected == 0) ImGui.EndDisabled();

        ImGui.SameLine();
        if (selected + 1 == length) ImGui.BeginDisabled();
        if (ImGuiComponents.IconButton(id+1, FontAwesomeIcon.ArrowRight)) selected++;
        if (selected + 1 == length) ImGui.EndDisabled();
    }

    public static void DrawIcon(uint iconId, Vector2 iconSize)
    {
        var texture = TexturesCache.Instance!.GetTextureFromIconId(iconId);
        ImGui.Image(texture.ImGuiHandle, iconSize);
    }
}
