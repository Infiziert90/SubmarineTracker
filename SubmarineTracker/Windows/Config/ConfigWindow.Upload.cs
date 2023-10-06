namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void Upload()
    {
        if (ImGui.BeginTabItem("Upload"))
        {
            var changed = false;
            ImGuiHelpers.ScaledDummy(5.0f);

            Helper.WrappedText(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Upload - Information 1", "Anonymously provide data about submarines.\nThis data can't be tied to you in any way and everyone benefits!"));

            Helper.WrappedText(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Upload - What", "What data?"));
            ImGuiHelpers.ScaledIndent(10.0f);
            Helper.WrappedText(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Upload - Point 1", "Loot received"));
            Helper.WrappedText(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Upload - Point 2", "Sector unlocks"));
            Helper.WrappedText(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Upload - Point 3", "Exploration procs"));
            ImGuiHelpers.ScaledIndent(-10.0f);

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5.0f);

            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Upload Permission", "Upload Permission"), ref Configuration.UploadPermission);

            if (changed)
                Configuration.Save();

            ImGui.EndTabItem();
        }
    }
}
