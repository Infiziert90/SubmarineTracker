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

                ImGui.EndTable();

                if (deletion != string.Empty)
                {
                    Configuration.SavedBuilds.Remove(deletion);
                    Configuration.Save();
                }
            }

            if (changed)
                Configuration.Save();

            ImGui.EndTabItem();
        }
    }
}
