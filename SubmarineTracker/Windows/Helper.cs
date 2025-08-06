using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows;

public static class Helper
{
    public const float SeparatorPadding = 1.0f;
    public static float GetSeparatorPaddingHeight => SeparatorPadding * ImGuiHelpers.GlobalScale;

    private static PopupMenu SettingsMenu = null!;

    public static readonly Vector4 TransparentBackground = new(0.0f, 0.0f, 0.0f, 0.8f);
    public static readonly Vector4 CustomFullyDone = new(0.12549f, 0.74902f, 0.33333f, 0.6f);
    public static readonly Vector4 CustomPartlyDone = new(1.0f, 0.81569f, 0.27451f, 0.6f);
    public static readonly Vector4 CustomOnRoute = new(0.85882f, 0.22745f, 0.20392f, 0.6f);

    public static void Initialize(Plugin plugin)
    {
        SettingsMenu = new PopupMenu("##configMenu", PopupMenu.PopupMenuButtons.LeftRight,
                                     [
                                         new PopupMenu.PopupMenuItemSelectable(Language.WindowNameTracker, plugin.OpenTracker, Language.MenuTooltipTracker),
                                         new PopupMenu.PopupMenuItemSelectable(Language.WindowNameBuilder, plugin.OpenBuilder, Language.MenuTooltipBuilder),
                                         new PopupMenu.PopupMenuItemSelectable(Language.WindowNameLoot, plugin.OpenLoot, Language.MenuTooltipLoot),
                                         new PopupMenu.PopupMenuItemSelectable(Language.WindowNameHelpy, plugin.OpenHelpy, Language.MenuTooltipHelpy),
                                         new PopupMenu.PopupMenuItemSelectable(Language.WindowNameOverlay, plugin.OpenOverlay, Language.MenuTooltipOverlay),
                                         new PopupMenu.PopupMenuItemSelectable(Language.WindowNameConfig, plugin.OpenConfig, Language.MenuTooltipConfig),
                                         new PopupMenu.PopupMenuItemSeparator(),
                                         new PopupMenu.PopupMenuItemSelectable(Language.MenuEntryDiscordThread, Plugin.DiscordSupport, Language.MenuTooltipDiscordThread),
                                         new PopupMenu.PopupMenuItemSelectable(Language.MenuEntryLocalization, Plugin.DiscordSupport, Language.MenuTooltipLocalization),
                                         new PopupMenu.PopupMenuItemSelectable(Language.MenuEntryIssues, Plugin.IssuePage, Language.MenuTooltipIssues),
                                         new PopupMenu.PopupMenuItemSelectable(Language.MenuEntryKoFi, Plugin.Kofi, Language.MenuTooltipKoFi),
                                         new PopupMenu.PopupMenuItemSeparator(),
                                         new PopupMenu.PopupMenuItemSelectable(Language.MenuEntrySync, plugin.Sync, Language.MenuTooltipSync)
                                     ]);
    }

    /// <summary>
    /// An unformatted version for ImGui.TextColored
    /// </summary>
    /// <param name="color">color to be used</param>
    /// <param name="text">text to display</param>
    public static void TextColored(Vector4 color, string text)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.TextUnformatted(text);
    }

    /// <summary>
    /// An unformatted version for ImGui.Tooltip
    /// </summary>
    /// <param name="tooltip">tooltip to display</param>
    public static void Tooltip(string tooltip)
    {
        using (ImRaii.Tooltip())
        using (ImRaii.TextWrapPos(ImGui.GetFontSize() * 35.0f))
            ImGui.TextUnformatted(tooltip);
    }

    /// <summary>
    /// An unformatted version for ImGui.TextWrapped
    /// </summary>
    /// <param name="text">text to display</param>
    public static void TextWrapped(string text)
    {
        using (ImRaii.TextWrapPos(0.0f))
            ImGui.TextUnformatted(text);
    }

    /// <summary>
    /// An unformatted version for ImGui.TextWrapped with color
    /// </summary>
    /// <param name="color">color to be used</param>
    /// <param name="text">text to display</param>
    public static void WrappedTextWithColor(Vector4 color, string text)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            TextWrapped(text);
    }

    /// <summary>
    /// An unformatted version for ImGui.BulletText
    /// </summary>
    /// <param name="text">text to display</param>
    public static void BulletText(string text)
    {
        ImGui.Bullet();
        ImGui.SameLine();
        ImGui.TextUnformatted(text);
    }

    public static void NoData()
    {
        ImGuiHelpers.ScaledDummy(10.0f);
        WrappedError(Language.ErrorNoData);
    }

    public static void WrappedError(string text)
    {
        WrappedTextWithColor(ImGuiColors.DalamudOrange, text);
    }

    public static string GenerateVoyageText(Submarine sub, bool useTime = false)
    {
        var time = Language.TermsNoVoyage;
        if (sub.IsOnVoyage())
        {
            time = Language.TermsDone;

            var returnTime = sub.LeftoverTime();
            if (returnTime.TotalSeconds > 0)
                time = $"{(useTime ? Utils.ToTime(returnTime) : sub.ReturnTime.ToLocalTime())}";
        }

        return time;
    }

    public static void CenterText(string text, float indent = 0.0f, Vector4? color = null)
    {
        using var pushedColor = ImRaii.PushColor(ImGuiCol.Text, color ?? Vector4.Zero, color.HasValue);

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
            Tooltip(tooltip);

        return clicked;
    }

    public static void DrawComboWithArrows(string label, ref int selected, ref string[] comboArray, int id = 0)
    {
        using (ImRaii.ItemWidth((ImGui.GetWindowWidth() / 2) - ImGui.GetStyle().ItemSpacing.X))
            ImGui.Combo(label, ref selected, comboArray, comboArray.Length);

        DrawArrows(ref selected, comboArray.Length, id);
    }

    public static void DrawArrowsDictionary(ref uint selected, uint[] keys, int id = 0)
    {
        var idx = Array.IndexOf(keys, selected);
        DrawArrows(ref idx, keys.Length, id);
        selected = keys[idx];
    }

    public static bool DrawArrows(ref int selected, int length, int id = 0)
    {
        var changed = false;

        // Prevents changing values from triggering EndDisable
        var isMin = selected == 0;
        var isMax = selected + 1 == length;

        ImGui.SameLine();
        using (ImRaii.Disabled(isMin))
        {
            if (ImGuiComponents.IconButton(id, FontAwesomeIcon.ArrowLeft))
            {
                selected--;
                changed = true;
            }
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(isMax))
        {
            if (ImGuiComponents.IconButton(id + 1, FontAwesomeIcon.ArrowRight))
            {
                selected++;
                changed = true;
            }
        }

        return changed;
    }

    public static void DrawScaledIcon(uint iconId, Vector2 iconSize)
    {
        iconSize *= ImGuiHelpers.GlobalScale;
        var texture = Plugin.Texture.GetFromGameIcon(iconId).GetWrapOrDefault();
        if (texture == null)
        {
            ImGui.TextUnformatted($"Unknown icon {iconId}");
            return;
        }

        ImGui.Image(texture.Handle, iconSize);
    }

    public static bool Button(string id, FontAwesomeIcon icon, bool disabled = false)
    {
        using (ImRaii.PushId(id))
        using (ImRaii.Disabled(disabled))
        using (Plugin.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
            return ImGui.Button(icon.ToIconString());
    }

    public static bool Button(FontAwesomeIcon icon, Vector2? size = null)
    {
        size ??= Vector2.Zero;
        using (ImRaii.PushFont(UiBuilder.IconFont))
            return ImGui.Button(icon.ToIconString(), size.Value);
    }

    public static void UrlButton(string id, FontAwesomeIcon icon, string url, string tooltip)
    {
        if (Button(id, icon))
            Dalamud.Utility.Util.OpenLink(url);

        if (ImGui.IsItemHovered())
            Tooltip(tooltip);
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
        TextColored(textColor, text);

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5.0f);
    }

    public static void ClippedCombo<T>(string label, ref int selected, T[] items, Func<T, string> toString)
    {
        var height = ImGui.GetTextLineHeightWithSpacing();

        using var combo = ImRaii.Combo(label, toString(items[selected]));
        if (!combo.Success)
            return;

        using var clipper = new ListClipper(items.Length, itemHeight: height);
        foreach (var idx in clipper.Rows)
            if (ImGui.Selectable(toString(items[idx]), idx == selected))
                selected = idx;
    }
}
