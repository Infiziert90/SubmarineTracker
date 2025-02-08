using Dalamud.Interface.Components;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void Overlay()
    {
        using var tabItem = ImRaii.TabItem($"{Language.ConfigTabOverlay}##Overlay");
        if (!tabItem.Success)
            return;

        var changed = false;
        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryBehavior);
        using (ImRaii.PushIndent(10.0f))
        {
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxOpenOnStartup, ref Plugin.Configuration.OverlayStartUp);
            ImGuiComponents.HelpMarker(Language.ConfigTabTooltipOpenOnStartup);
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxOnReturn, ref Plugin.Configuration.OverlayAlwaysOpen);
            ImGuiComponents.HelpMarker(Language.ConfigTabTooltipOnReturn);
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxAutomaticallyClose, ref Plugin.Configuration.OverlayHoldClosed);
            ImGuiComponents.HelpMarker(Language.ConfigTabTooltipAutomaticallyClose);
        }


        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryGeneral);
        using (ImRaii.PushIndent(10.0f))
        {
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxShowRank, ref Plugin.Configuration.OverlayShowRank);
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxShowBuild, ref Plugin.Configuration.OverlayShowBuild);
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxAsDate, ref Plugin.Configuration.OverlayShowDate);
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxTitleTime, ref Plugin.Configuration.OverlayTitleTime);
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxFirstReturnTime, ref Plugin.Configuration.OverlayFirstReturn);
            ImGuiComponents.HelpMarker(Language.ConfigTabTooltipFirstReturnTime);
            if (ImGui.Checkbox(Language.ConfigTabCheckboxSortTimeLowest, ref Plugin.Configuration.OverlaySort))
            {
                changed = true;
                Plugin.Configuration.OverlaySortReverse = false;
            }
            ImGuiComponents.HelpMarker(Language.ConfigTabTooltipSortTimeLowest);

            if (Plugin.Configuration.OverlaySort)
            {
                using var indent = ImRaii.PushIndent(10.0f);
                changed |= ImGui.Checkbox(Language.ConfigTabCheckboxReverseSort, ref Plugin.Configuration.OverlaySortReverse);
            }

            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxOnlyReturned, ref Plugin.Configuration.OverlayOnlyReturned);
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxNoHidden, ref Plugin.Configuration.OverlayNoHidden);
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryColors);
        var spacing = 150 * ImGuiHelpers.GlobalScale;
        using (ImRaii.PushIndent(10.0f))
        {
            changed |= Helper.ColorPickerWithReset(Language.ConfigTabColorsAllDone, ref Plugin.Configuration.OverlayAllDone, Helper.CustomFullyDone, spacing);
            changed |= Helper.ColorPickerWithReset(Language.ConfigTabColorsPartlyDone, ref Plugin.Configuration.OverlayPartlyDone, Helper.CustomPartlyDone,spacing);
            changed |= Helper.ColorPickerWithReset(Language.ConfigTabColorsNoneDone, ref Plugin.Configuration.OverlayNoneDone, Helper.CustomOnRoute, spacing);
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryWindow);
        using (ImRaii.PushIndent(10.0f))
        {
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxLockSize, ref Plugin.Configuration.OverlayLockSize);
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxLockPosition, ref Plugin.Configuration.OverlayLockLocation);
        }

        if (changed)
            Plugin.Configuration.Save();
    }
}
