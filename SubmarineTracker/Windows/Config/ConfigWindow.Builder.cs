using Dalamud.Interface.Components;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void Builder()
    {
        if (ImGui.BeginTabItem("Builder"))
        {
            ImGuiHelpers.ScaledDummy(5.0f);
            var changed = false;

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Options:");
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox("Show Only Current FC", ref Configuration.ShowOnlyCurrentFC);
            changed |= ImGui.Checkbox("Auto Select Current Submarine", ref Configuration.AutoSelectCurrent);
            ImGuiComponents.HelpMarker("Automatically selects the current selected submarine in the games voyage interface.");
            if (Configuration.AutoSelectCurrent)
            {
                ImGuiHelpers.ScaledIndent(10.0f);
                changed |= ImGui.Checkbox("Show Next Overlay", ref Configuration.ShowNextOverlay);
                ImGuiComponents.HelpMarker("Overlay attached to the voyage selection interface" +
                                           "\n" +
                                           "\nSuggest the next sector to unlock or explore" +
                                           "\nVisible if:" +
                                           "\n  a) The map has unlocks left, or" +
                                           "\n  b) A sector must be explored to unlock the next map");
                changed |= ImGui.Checkbox("Show Unlock Overlay", ref Configuration.ShowUnlockOverlay);
                ImGuiComponents.HelpMarker("Overlay attached to the voyage selection interface" +
                                           "\n" +
                                           "\nShows all unlocks that are still open" +
                                           "\nVisible if:" +
                                           "\n  a) The map has unlocks left");
                changed |= ImGui.Checkbox("Show Route Overlay", ref Configuration.ShowRouteOverlay);
                ImGuiComponents.HelpMarker("Overlay attached to the voyage selection interface." +
                                           "\n" +
                                           "\nSuggest the best route to take" +
                                           "\nEmpty if:" +
                                           "\n  a) Highest Rank threshold has been passed" +
                                           "\n  b) MustInclude is empty");
                changed |= ImGui.SliderInt("Highest Rank", ref Configuration.HighestLevel, 1, (int) Plugin.BuilderWindow.RankSheet.Last().RowId, "Rank %d");
                ImGuiComponents.HelpMarker("No route suggestions above this rank.");
                changed |= ImGui.Checkbox("Auto Include Main Sector", ref Configuration.MainRouteAutoInclude);
                ImGuiComponents.HelpMarker("Auto includes the next main route sector, if there is one.");
                ImGuiHelpers.ScaledIndent(-10.0f);
            }
            ImGui.Unindent(10.0f);

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Saved Builds:");
            if (ImGui.BeginTable("##DeleteBuildsTable", 2))
            {
                ImGui.TableSetupColumn("Build");
                ImGui.TableSetupColumn("##Del", 0, 0.1f);

                ImGui.TableHeadersRow();

                var deletion = string.Empty;
                foreach (var (key, build) in Configuration.SavedBuilds)
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
                    Configuration.SavedBuilds.Remove(deletion);
                    Configuration.Save();
                }

                ImGui.EndTable();
            }

            if (changed)
            {
                Plugin.BuilderWindow.VoyageInterfaceSelection = 0;
                Configuration.Save();
            }

            ImGui.EndTabItem();
        }
    }
}
