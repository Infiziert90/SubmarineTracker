﻿using Dalamud.Interface;
using Dalamud.Interface.Components;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void Builder()
    {
        using var tabItem = ImRaii.TabItem($"{Loc.Localize("Builder Tab - Builder", "Builder")}##Builder");
        if (!tabItem.Success)
            return;

        ImGuiHelpers.ScaledDummy(5.0f);
        var changed = false;

        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Options", "Options:"));
        ImGuiHelpers.ScaledIndent(10.0f);
        changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Current FC", "Show Only Current FC"), ref Plugin.Configuration.ShowOnlyCurrentFC);
        changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Auto Selection", "Auto Select Current Submarine"), ref Plugin.Configuration.AutoSelectCurrent);
        ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Auto Selection", "Automatically selects the current selected submarine in the games voyage interface."));
        if (Plugin.Configuration.AutoSelectCurrent)
        {
            ImGuiHelpers.ScaledIndent(10.0f);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Next Overlay", "Show Next Overlay"), ref Plugin.Configuration.ShowNextOverlay);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Next Overlay", "Overlay attached to the voyage selection interface" +
                                               "\n" +
                                               "\nSuggest the next sector to unlock or explore" +
                                               "\nVisible if:" +
                                               "\n  a) The map has unlocks left, or" +
                                               "\n  b) A sector must be explored to unlock the next map"));
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Unlock Overlay", "Show Unlock Overlay"), ref Plugin.Configuration.ShowUnlockOverlay);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Unlock Overlay", "Overlay attached to the voyage selection interface" +
                                               "\n" +
                                               "\nShows all unlocks that are still open" +
                                               "\nVisible if:" +
                                               "\n  a) The map has unlocks left"));
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Route Overlay", "Show Route Overlay"), ref Plugin.Configuration.ShowRouteOverlay);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Route Overlay", "Overlay attached to the voyage selection interface." +
                                               "\n" +
                                               "\nSuggest the best route to take" +
                                               "\nEmpty if:" +
                                               "\n  a) Highest Rank threshold has been passed" +
                                               "\n  b) MustInclude is empty"));
            changed |= ImGui.SliderInt(Loc.Localize("Config Tab Slider - Rank", "Highest Rank"), ref Plugin.Configuration.HighestLevel, 1, (int) Sheets.LastRank, $"{Loc.Localize("Terms - Rank", "Rank")} %d");
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Rank", "No route suggestions above this rank."));
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Include Main", "Auto Include Main Sector"), ref Plugin.Configuration.MainRouteAutoInclude);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Include Main", "Auto includes the next main route sector, if there is one."));
            ImGuiHelpers.ScaledIndent(-10.0f);
        }
        ImGuiHelpers.ScaledIndent(-10.0f);

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Saved Builds", "Saved Builds:"));
        using var table = ImRaii.Table("##DeleteBuildsTable", 2);
        if (table.Success)
        {
            ImGui.TableSetupColumn(Loc.Localize("Terms - Build", "Build"));
            ImGui.TableSetupColumn("##Del", 0, 0.1f);

            ImGui.TableHeadersRow();

            var deletion = string.Empty;
            foreach (var (key, build) in Plugin.Configuration.SavedBuilds)
            {
                ImGui.TableNextColumn();
                var text = Utils.FormattedRouteBuild(key, build).Split("\n");
                ImGuiHelpers.SafeTextWrapped(text.First());
                ImGui.TextColored(ImGuiColors.ParsedOrange, text.Last());

                ImGui.TableNextColumn();
                if (ImGuiComponents.IconButton(key, FontAwesomeIcon.Trash))
                    deletion = key;

                ImGui.TableNextRow();
            }

            if (deletion != string.Empty)
            {
                Plugin.Configuration.SavedBuilds.Remove(deletion);
                Plugin.Configuration.Save();
            }
        }

        if (changed)
        {
            Plugin.BuilderWindow.VoyageInterfaceSelection = 0;
            Plugin.Configuration.Save();
        }
    }
}
