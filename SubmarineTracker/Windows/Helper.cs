using Dalamud.Interface.Components;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows;

public static class Helper
{
    public static readonly Vector4 TransparentBackground = new(0.0f, 0.0f, 0.0f, 0.8f);
    public static readonly Vector4 CustomFullyDone = new(0.12549f, 0.74902f, 0.33333f, 0.6f);
    public static readonly Vector4 CustomPartlyDone = new(1.0f, 0.81569f, 0.27451f, 0.6f);
    public static readonly Vector4 CustomOnRoute = new(0.85882f, 0.22745f, 0.20392f, 0.6f);

    public static void NoData()
    {
        ImGuiHelpers.ScaledDummy(10.0f);
        WrappedError("No data found for this character's FC\n" +
                     "Please visit your Company Workshop and access Submersible Management at the Voyage Control Panel.");
    }

    public static string BuildFcName(Submarines.FcSubmarines fc, bool useCharacterName)
    {
        return !useCharacterName ? $"{fc.Tag}@{fc.World}" : $"{fc.CharacterName}@{fc.World}";;
    }

    public static void WrappedError(string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudOrange);
        ImGui.TextWrapped(text);
        ImGui.PopStyleColor();
    }

    public static string GenerateVoyageText(Submarines.Submarine sub, bool useTime = false)
    {
        var time = "No Voyage";
        if (sub.IsOnVoyage())
        {
            time = "Done";

            var returnTime = sub.LeftoverTime();
            if (returnTime.TotalSeconds > 0)
                time = $"{(useTime ? Utils.ToTime(returnTime) : sub.ReturnTime.ToLocalTime())}";
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

    public static void DrawComboWithArrows(ref int selected, ref string[] comboArray, int id = 0)
    {
        var windowWidth = ImGui.GetWindowWidth() / 2;
        ImGui.PushItemWidth(windowWidth - (5.0f * ImGuiHelpers.GlobalScale));
        ImGui.Combo("##existingSubs", ref selected, comboArray, comboArray.Length);
        ImGui.PopItemWidth();
        DrawArrows(ref selected, comboArray.Length, id);
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
