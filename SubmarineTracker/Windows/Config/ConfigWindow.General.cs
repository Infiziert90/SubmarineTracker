using SubmarineTracker.Data;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void General()
    {
        using var tabItem = ImRaii.TabItem($"{Language.ConfigTabGeneral}##General");
        if (!tabItem.Success)
            return;

        var changed = false;
        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.AlignTextToFramePadding();
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabServerBar);
        changed |= ImGui.Checkbox(Language.ConfigTabCheckboxServerBarEnabled, ref Plugin.Configuration.ShowDtrEntry);
        if (Plugin.Configuration.ShowDtrEntry)
        {
            using var indent = ImRaii.PushIndent(10.0f);
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxNoSubName, ref Plugin.Configuration.DtrShowSubmarineName);
        }
        changed |= ImGui.Checkbox(Language.ConfigTabCheckboxServerBarNumbers, ref Plugin.Configuration.DtrShowOverlayNumbers);
        changed |= ImGui.Checkbox(Language.ConfigTabCheckboxInventorySlotCount, ref Plugin.Configuration.DtrShowInventorySlots);

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.AlignTextToFramePadding();
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryNaming);

        Helper.WrappedTextWithColor(ImGuiColors.HealerGreen, Language.ConfigTabNamingExample);
        ImGui.SameLine();
        ImGui.TextUnformatted($"{Plugin.Configuration.NameOption.GetExample()}");

        ImGui.SetNextItemWidth(200.0f * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo("##NameOptionsCombo", Plugin.Configuration.NameOption.GetName()))
        {
            if (combo.Success)
            {
                foreach (var nameOption in Enum.GetValues<NameOptions>())
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

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryUploads);

        changed |= ImGui.Checkbox(Language.ConfigTabCheckboxUploadPermission, ref Plugin.Configuration.UploadPermission);

        Helper.WrappedTextWithColor(ImGuiColors.HealerGreen, Language.ConfigTabUploadInformation1);
        Helper.WrappedTextWithColor(ImGuiColors.DalamudViolet, Language.ConfigTabUploadWhat);

        using (ImRaii.PushIndent(10.0f))
        {
            Helper.WrappedTextWithColor(ImGuiColors.DalamudViolet, Language.ConfigTabUploadPoint1);
            Helper.WrappedTextWithColor(ImGuiColors.DalamudViolet, Language.ConfigTabUploadPoint2);
            Helper.WrappedTextWithColor(ImGuiColors.DalamudViolet, Language.ConfigTabUploadPoint3);
            Helper.WrappedTextWithColor(ImGuiColors.DalamudViolet, Language.ConfigTabUploadPoint4);
        }

        if (changed)
            Plugin.Configuration.Save();
    }
}
