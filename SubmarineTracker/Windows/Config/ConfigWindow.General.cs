using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void General()
    {
        if (ImGui.BeginTabItem($"{Loc.Localize("Config Tab - General", "General")}##General"))
        {
            var changed = false;
            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Naming", "Naming Convention:"));

            Helper.WrappedText(ImGuiColors.HealerGreen, Loc.Localize("Config Tab Naming - Example", "Example: "));
            ImGui.SameLine();
            ImGui.TextUnformatted($"{Plugin.Configuration.NameOption.GetExample()}");

            ImGui.SetNextItemWidth(200.0f * ImGuiHelpers.GlobalScale);
            if (ImGui.BeginCombo($"##NameOptionsCombo", Plugin.Configuration.NameOption.GetName()))
            {
                foreach (var nameOption in (NameOptions[]) Enum.GetValues(typeof(NameOptions)))
                {
                    if (ImGui.Selectable($"{nameOption.GetName()} ({nameOption.GetExample()})"))
                    {
                        Plugin.Configuration.NameOption = nameOption;
                        Plugin.Configuration.Save();
                    }
                }

                ImGui.EndCombo();
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Uploads", "Uploads:"));

            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Upload Permission", "Upload Permission"), ref Plugin.Configuration.UploadPermission);

            Helper.WrappedText(ImGuiColors.HealerGreen, Loc.Localize("Config Tab Upload - Information 1", "Anonymously provide data about submarines.\nThis data can't be tied to you in any way and everyone benefits!"));
            Helper.WrappedText(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Upload - What", "What data?"));

            ImGuiHelpers.ScaledIndent(10.0f);
            Helper.WrappedText(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Upload - Point 1", "Loot received"));
            Helper.WrappedText(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Upload - Point 2", "Sector unlocks"));
            Helper.WrappedText(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Upload - Point 3", "Exploration procs"));
            Helper.WrappedText(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Upload - Point 4", "Submarine stats"));
            ImGuiHelpers.ScaledIndent(-10.0f);

            if (changed)
                Plugin.Configuration.Save();

            ImGui.EndTabItem();
        }
    }
}
