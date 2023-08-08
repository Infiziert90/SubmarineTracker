using Dalamud.Interface.Components;

using static SubmarineTracker.Data.Submarines;

namespace SubmarineTracker.Windows;

public static class Helper
{
    public static readonly Vector4 TransparentBackground = new(0.0f, 0.0f, 0.0f, 0.8f);
    public static readonly Vector4 CustomFullyDone = new(0.12549f, 0.74902f, 0.33333f, 0.6f);
    public static readonly Vector4 CustomPartlyDone = new(1.0f, 0.81569f, 0.27451f, 0.6f);
    public static readonly Vector4 CustomOnRoute = new(0.85882f, 0.22745f, 0.20392f, 0.6f);

    private static PopupMenu SettingsMenu = null!;

    public static void Initialize(Plugin plugin)
    {
        SettingsMenu = new PopupMenu("configMenu",
                                     PopupMenu.PopupMenuButtons.LeftRight,
                                     new List<PopupMenu.IPopupMenuItem>
                                     {
                                         new PopupMenu.PopupMenuItemSelectable("Tracker Window", plugin.OpenTracker,"Open the tracker window."),
                                         new PopupMenu.PopupMenuItemSelectable("Builder Window", plugin.OpenBuilder,"Open the builder window."),
                                         new PopupMenu.PopupMenuItemSelectable("Loot Window", plugin.OpenLoot,"Open the loot window."),
                                         new PopupMenu.PopupMenuItemSelectable("Helpy Window", plugin.OpenHelpy,"Open the helper window."),
                                         new PopupMenu.PopupMenuItemSelectable("Config Window", plugin.OpenConfig,"Open the config window."),
                                         new PopupMenu.PopupMenuItemSeparator(),
                                         new PopupMenu.PopupMenuItemSelectable("Sync", plugin.Sync,"Syncs all data from disk and refresh cached data"),
                                     });
    }

    public static void NoData()
    {
        ImGuiHelpers.ScaledDummy(10.0f);
        WrappedError("No data found for this character's FC\n" +
                     "Please visit your Company Workshop and access Submersible Management at the Voyage Control Panel.");
    }

    public static string BuildFcName(FcSubmarines fc, bool useCharName)
    {
        return !useCharName ? $"{fc.Tag}@{fc.World}" : $"{fc.CharacterName}@{fc.World}";
    }

    public static void WrappedError(string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudOrange);
        ImGui.TextWrapped(text);
        ImGui.PopStyleColor();
    }

    public static string GenerateVoyageText(Submarine sub, bool useTime = false)
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

        ImGui.SameLine(avail - (33.0f * ImGuiHelpers.GlobalScale));
        ImGuiComponents.IconButton(FontAwesomeIcon.Cog);
        SettingsMenu.Draw();
    }

    public static bool DrawButtonWithTooltip(FontAwesomeIcon icon, string tooltip)
    {
        var clicked = ImGuiComponents.IconButton(icon);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);

        return clicked;
    }

    public static void DrawComboWithArrows(string label, ref int selected, ref string[] comboArray, int id = 0)
    {
        ImGui.PushItemWidth((ImGui.GetWindowWidth() / 2) - (5.0f * ImGuiHelpers.GlobalScale));
        ImGui.Combo(label, ref selected, comboArray, comboArray.Length);
        ImGui.PopItemWidth();
        DrawArrows(ref selected, comboArray.Length, id);
    }

    public static void DrawArrows(ref int selected, int length, int id = 0)
    {
        // Prevents changing values from triggering EndDisable
        var isMin = selected == 0;
        var isMax = selected + 1 == length;

        ImGui.SameLine();
        if (isMin) ImGui.BeginDisabled();
        if (ImGuiComponents.IconButton(id, FontAwesomeIcon.ArrowLeft)) selected--;
        if (isMin) ImGui.EndDisabled();

        ImGui.SameLine();
        if (isMax) ImGui.BeginDisabled();
        if (ImGuiComponents.IconButton(id+1, FontAwesomeIcon.ArrowRight)) selected++;
        if (isMax) ImGui.EndDisabled();
    }

    public static void DrawIcon(uint iconId, Vector2 iconSize)
    {
        var texture = TexturesCache.Instance!.GetTextureFromIconId(iconId);
        ImGui.Image(texture.ImGuiHandle, iconSize);
    }
}
