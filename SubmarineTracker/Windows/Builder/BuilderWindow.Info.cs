using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    private bool InfoTab()
    {
        using var tabItem = ImRaii.TabItem($"{Language.BuilderTabInfo}##Info");
        if (!tabItem.Success)
            return false;

        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.BuilderTabInfoHowCalculated);
        ImGui.TextWrapped(Language.BuilderTabTextHowCalculated);

        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.BuilderTabInfoBestExp);
        ImGui.TextWrapped(Language.BuilderTabTextBestExp);

        ImGuiHelpers.ScaledDummy(5.0f);

        var spacing = ImGui.CalcTextSize("Optimal").X + 20.0f;

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.BuilderTabInfoBreakpoints);
        ImGui.TextUnformatted("T2");
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Language.BuilderTabTextT2);
        ImGui.TextUnformatted("T3");
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Language.BuilderTabTextT3);
        ImGui.TextUnformatted(Language.TermsNormal);
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Language.BuilderTabTextNormal);
        ImGui.TextUnformatted(Language.TermsOptimal);
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Language.BuilderTabTextOptimal);
        ImGui.TextUnformatted(Language.TermsFavor);
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Language.BuilderTabTextFavor);

        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.HelpyTabInfoColors);
        ImGui.TextUnformatted(Language.ColorsWhite);
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Language.BuilderTabInfoWhiteText);
        Helper.TextColored(ImGuiColors.ParsedGold,Language.ColorsGold);
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Language.BuilderTabInfoGoldText);
        Helper.TextColored(ImGuiColors.HealerGreen,Language.ColorsGreen);
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Language.BuilderTabInfoGreenText);
        Helper.TextColored(ImGuiColors.ParsedPink,Language.ColorsPink);
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Language.BuilderTabInfoPinkText);
        Helper.TextColored(ImGuiColors.DalamudRed,Language.ColorsRed);
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Language.BuilderTabInfoRedText);

        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, $"{Language.TermsRoute}:");
        Helper.TextColored(ImGuiColors.DalamudViolet,Language.ColorsViolet);
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Language.HelpyTabInfoVioletText);
        Helper.TextColored(ImGuiColors.DalamudRed,Language.ColorsRed);
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Language.HelpyTabInfoRedText);

        return true;
    }
}
