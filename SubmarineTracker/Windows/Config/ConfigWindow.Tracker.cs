using Dalamud.Interface.Components;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void Tracker()
    {
        using var tabItem = ImRaii.TabItem($"{Language.ConfigTabTracker}##Tracker");
        if (!tabItem.Success)
            return;

        var changed = false;

        ImGuiHelpers.ScaledDummy(5.0f);
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryFCButtons);
        using (ImRaii.PushIndent(10.0f))
        {
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxShowAll, ref Plugin.Configuration.ShowAll);
            ImGuiComponents.HelpMarker(Language.ConfigTabTooltipShowAll);
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxLetMeResize, ref Plugin.Configuration.UserResize);
            ImGuiComponents.HelpMarker(Language.ConfigTabTooltipLetMeResize);
        }

        if (Plugin.Configuration.ShowAll)
        {
            ImGuiHelpers.ScaledDummy(5.0f);
            Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryAllSection);

            using var indent = ImRaii.PushIndent(10.0f);
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxShowRoute, ref Plugin.Configuration.ShowRouteInAll);
            ImGuiComponents.HelpMarker(Language.ConfigTabTooltipShowRoute);
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxShowDate, ref Plugin.Configuration.ShowDateInAll);
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryOverview);
        using (ImRaii.PushIndent(10.0f))
        {
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxRepairStatus, ref Plugin.Configuration.ShowOnlyLowest);
            if (Plugin.Configuration.ShowOnlyLowest)
            {
                using var indent = ImRaii.PushIndent(10.0f);
                changed |= ImGui.Checkbox(Language.ConfigTabCheckboxRepairAfterVoyage, ref Plugin.Configuration.ShowPrediction);
            }

            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxReturnTime, ref Plugin.Configuration.ShowTimeInOverview);
            if (Plugin.Configuration.ShowTimeInOverview)
            {
                using var indent = ImRaii.PushIndent(10.0f);
                changed |= ImGui.Checkbox(Language.ConfigTabCheckboxReturnDate, ref Plugin.Configuration.UseDateTimeInstead);
                changed |= ImGui.Checkbox(Language.ConfigTabCheckboxTimeDateShowBoth, ref Plugin.Configuration.ShowBothOptions);
            }

            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxShowRoute, ref Plugin.Configuration.ShowRouteInOverview);
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryDetailedView);
        using (ImRaii.PushIndent(10.0f))
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxExtendedParts, ref Plugin.Configuration.ShowExtendedPartsList);

        if (changed)
            Plugin.Configuration.Save();
    }
}
