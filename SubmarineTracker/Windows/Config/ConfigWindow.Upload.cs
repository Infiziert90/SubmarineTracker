namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void Upload()
    {
        if (ImGui.BeginTabItem("Upload"))
        {
            var changed = false;
            ImGuiHelpers.ScaledDummy(5.0f);

            Helper.WrappedText(ImGuiColors.DalamudViolet, "Anonymously provide data about submarines. This data can't be tied to you in any way and everyone benefits!");

            Helper.WrappedText(ImGuiColors.DalamudViolet, "What data?");
            ImGuiHelpers.ScaledIndent(10.0f);
            Helper.WrappedText(ImGuiColors.DalamudViolet, "Loot received");
            Helper.WrappedText(ImGuiColors.DalamudViolet, "Sector unlocks");
            Helper.WrappedText(ImGuiColors.DalamudViolet, "Exploration procs");
            ImGuiHelpers.ScaledIndent(-10.0f);

            Helper.WrappedText(ImGuiColors.DalamudViolet, "Has there been any uploads?");
            ImGuiHelpers.ScaledIndent(10.0f);
            Helper.WrappedText(ImGuiColors.DalamudViolet, $"{(Configuration.UploadCounter > 0 ? $"Yes, a total of {Configuration.UploadCounter} uploads" : "No")}");
            ImGuiHelpers.ScaledIndent(-10.0f);
            Helper.WrappedText(ImGuiColors.DalamudViolet, $"(The upload function hasn't been implemented yet)");

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5.0f);

            changed |= ImGui.Checkbox("Upload Permission", ref Configuration.UploadPermission);

            if (changed)
                Configuration.Save();

            ImGui.EndTabItem();
        }
    }
}
