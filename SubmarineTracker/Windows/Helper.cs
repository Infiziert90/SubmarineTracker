using Dalamud.Interface;
using Dalamud.Interface.Components;
using static SubmarineTracker.Data.Submarines;

namespace SubmarineTracker.Windows;

public static class Helper
{
    private static PopupMenu SettingsMenu = null!;

    public static readonly Vector4 TransparentBackground = new(0.0f, 0.0f, 0.0f, 0.8f);
    public static readonly Vector4 CustomFullyDone = new(0.12549f, 0.74902f, 0.33333f, 0.6f);
    public static readonly Vector4 CustomPartlyDone = new(1.0f, 0.81569f, 0.27451f, 0.6f);
    public static readonly Vector4 CustomOnRoute = new(0.85882f, 0.22745f, 0.20392f, 0.6f);

    public static void Initialize(Plugin plugin)
    {
        SettingsMenu = new PopupMenu("configMenu", PopupMenu.PopupMenuButtons.LeftRight,
                                     [
                                         new PopupMenu.PopupMenuItemSelectable(
                                             Loc.Localize("Window Name - Tracker", "Tracker"), plugin.OpenTracker,
                                             Loc.Localize("Menu Tooltip - Tracker", "Open the tracker window.")),
                                         new PopupMenu.PopupMenuItemSelectable(
                                             Loc.Localize("Window Name - Builder", "Builder"), plugin.OpenBuilder,
                                             Loc.Localize("Menu Tooltip - Builder", "Open the builder window.")),
                                         new PopupMenu.PopupMenuItemSelectable(
                                             Loc.Localize("Window Name - Loot", "Loot"), plugin.OpenLoot,
                                             Loc.Localize("Menu Tooltip - Loot", "Open the loot window.")),
                                         new PopupMenu.PopupMenuItemSelectable(
                                             Loc.Localize("Window Name - Helpy", "Helpy"), plugin.OpenHelpy,
                                             Loc.Localize("Menu Tooltip - Helpy", "Open the helper window.")),
                                         new PopupMenu.PopupMenuItemSelectable(
                                             Loc.Localize("Window Name - Overlay", "Overlay"), plugin.OpenOverlay,
                                             Loc.Localize("Menu Tooltip - Overlay", "Open the return overlay.")),
                                         new PopupMenu.PopupMenuItemSelectable(
                                             Loc.Localize("Window Name - Config", "Config"), plugin.OpenConfig,
                                             Loc.Localize("Menu Tooltip - Config", "Open the config window.")),
                                         new PopupMenu.PopupMenuItemSeparator(),
                                         new PopupMenu.PopupMenuItemSelectable(
                                             Loc.Localize("Menu Entry - Discord Thread", "Discord Thread"),
                                             Plugin.DiscordSupport,
                                             Loc.Localize("Menu Tooltip - Discord Thread", "Open the discord support thread.")),
                                         new PopupMenu.PopupMenuItemSelectable(
                                             Loc.Localize("Menu Entry - Localization", "Localization"),
                                             Plugin.DiscordSupport,
                                             Loc.Localize("Menu Tooltip - Localization", "Open the crowdin page in your browser")),
                                         new PopupMenu.PopupMenuItemSelectable(
                                             Loc.Localize("Menu Entry - Issues", "Issues"), Plugin.IssuePage,
                                             Loc.Localize("Menu Tooltip - Issues", "Open the issue page in your browser.")),
                                         new PopupMenu.PopupMenuItemSelectable(
                                             Loc.Localize("Menu Entry - KoFi", "Ko-Fi Tip"), Plugin.Kofi,
                                             Loc.Localize("Menu Tooltip - KoFi", "Open the kofi page in your browser.")),
                                         new PopupMenu.PopupMenuItemSeparator(),
                                         new PopupMenu.PopupMenuItemSelectable(
                                             Loc.Localize("Menu Entry - Sync", "Sync"), plugin.Sync,
                                             Loc.Localize("Menu Tooltip - Sync", "Reload all stored data from hard drive and refresh the cache."))
                                     ]);
    }

    public static void NoData()
    {
        ImGuiHelpers.ScaledDummy(10.0f);
        WrappedError(Loc.Localize("Error - No Data", "No data found for this character's FC\nPlease visit your Company Workshop and access Submersible Management at the Voyage Control Panel."));
    }

    public static void WrappedError(string text)
    {
        WrappedTextWithColor(ImGuiColors.DalamudOrange, text);
    }

    public static void WrappedTextWithColor(Vector4 color, string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextWrapped(text);
        ImGui.PopStyleColor();
    }

    public static void WrappedText(string text)
    {
        ImGui.PushTextWrapPos();
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
    }

    public static string GenerateVoyageText(Submarine sub, bool useTime = false)
    {
        var time = Loc.Localize("Terms - No Voyage", "No Voyage");
        if (sub.IsOnVoyage())
        {
            time = Loc.Localize("Terms - Done", "Done");

            var returnTime = sub.LeftoverTime();
            if (returnTime.TotalSeconds > 0)
                time = $"{(useTime ? Utils.ToTime(returnTime) : sub.ReturnTime.ToLocalTime())}";
        }

        return time;
    }

    public static void CenterText(string text, float indent = 0.0f)
    {
        indent *= ImGuiHelpers.GlobalScale;
        ImGui.SameLine(((ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X) * 0.5f) + indent);
        ImGui.TextUnformatted(text);
    }

    public static void MainMenuIcon()
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

    public static void DrawArrowsDictionary(ref uint selected, uint[] keys, int id = 0)
    {
        var idx = Array.IndexOf(keys, selected);
        DrawArrows(ref idx, keys.Length, id);
        selected = keys[idx];
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

    public static void DrawScaledIcon(uint iconId, Vector2 iconSize)
    {
        iconSize *= ImGuiHelpers.GlobalScale;
        var texture = Plugin.Texture.GetFromGameIcon(iconId).GetWrapOrDefault();
        if (texture == null)
        {
            ImGui.Text($"Unknown icon {iconId}");
            return;
        }

        ImGui.Image(texture.ImGuiHandle, iconSize);
    }

    public static bool Button(string id, FontAwesomeIcon icon, bool disabled)
    {
        var clicked = false;
        if (disabled)
        {
            ImGui.BeginDisabled();
            ImGuiComponents.IconButton(id, icon);
            ImGui.EndDisabled();
        }
        else
        {
            if (ImGuiComponents.IconButton(id, icon))
                clicked = true;
        }

        return clicked;
    }

    public static bool ColorPickerWithReset(string name, ref Vector4 current, Vector4 reset, float spacing)
    {
        var changed = ImGui.ColorEdit4($"##{name}ColorPicker", ref current, ImGuiColorEditFlags.NoInputs);
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(name);
        ImGui.SameLine(spacing);
        if (ImGui.Button($"Reset##{name}Reset"))
        {
            current = reset;
            changed = true;
        }

        return changed;
    }

    public static void IconHeader(uint icon, Vector2 iconSize, string text, Vector4 textColor)
    {
        DrawScaledIcon(icon, iconSize);
        ImGui.SameLine();

        var textY = ImGui.CalcTextSize(text).Y;
        var cursorY = ImGui.GetCursorPosY();
        ImGui.SetCursorPosY(cursorY + iconSize.Y - textY);
        ImGui.TextColored(textColor, text);

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5.0f);
    }
}
