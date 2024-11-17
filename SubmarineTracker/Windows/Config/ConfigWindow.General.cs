using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void General()
    {
        using var tabItem = ImRaii.TabItem($"{Loc.Localize("Config Tab - General", "General")}##General");
        if (!tabItem.Success)
            return;

        var changed = false;
        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - DtrBar", "Server Bar:"));
        changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - ServerBar Enabled", "Show Returning Sub"), ref Plugin.Configuration.ShowDtrEntry);
        changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - ServerBar Numbers", "Show On Route And Done Numbers"), ref Plugin.Configuration.DtrShowOverlayNumbers);

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Naming", "Naming Convention:"));

        Helper.WrappedTextWithColor(ImGuiColors.HealerGreen, Loc.Localize("Config Tab Naming - Example", "Example: "));
        ImGui.SameLine();
        ImGui.TextUnformatted($"{Plugin.Configuration.NameOption.GetExample()}");

        ImGui.SetNextItemWidth(200.0f * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo("##NameOptionsCombo", Plugin.Configuration.NameOption.GetName()))
        {
            if (combo.Success)
            {
                foreach (var nameOption in (NameOptions[]) Enum.GetValues(typeof(NameOptions)))
                {
                    if (ImGui.Selectable($"{nameOption.GetName()} ({nameOption.GetExample()})", Plugin.Configuration.NameOption == nameOption))
                    {
                        Plugin.Configuration.NameOption = nameOption;
                        Plugin.Configuration.Save();
                    }
                }
            }
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Uploads", "Uploads:"));

        changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Upload Permission", "Upload Permission"), ref Plugin.Configuration.UploadPermission);

        Helper.WrappedTextWithColor(ImGuiColors.HealerGreen, Loc.Localize("Config Tab Upload - Information 1", "Anonymously provide data about submarines.\nThis data can't be tied to you in any way and everyone benefits!"));
        Helper.WrappedTextWithColor(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Upload - What", "What data?"));

        ImGuiHelpers.ScaledIndent(10.0f);
        Helper.WrappedTextWithColor(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Upload - Point 1", "Loot received"));
        Helper.WrappedTextWithColor(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Upload - Point 2", "Sector unlocks"));
        Helper.WrappedTextWithColor(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Upload - Point 3", "Exploration procs"));
        Helper.WrappedTextWithColor(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Upload - Point 4", "Submarine stats"));
        ImGuiHelpers.ScaledIndent(-10.0f);

        if (changed)
            Plugin.Configuration.Save();
    }
}
