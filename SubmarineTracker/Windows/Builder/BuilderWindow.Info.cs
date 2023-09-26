namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    private bool InfoTab()
    {
        var open = ImGui.BeginTabItem($"{Loc.Localize("Builder Tab - Info", "Info")}##Info");
        if (open)
        {
            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Builder Tab Info - How Calculated", "How are these breakpoints calculated?"));
            ImGui.TextWrapped(Loc.Localize("Builder Tab Text - How Calculated", "Distance and duration are fixed calculated values. However all other breakpoints are calculated from community provided data.\nSpecial thanks to Mystic Spirit for all there work."));

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Builder Tab Info - Best Exp", "Best Exp?"));
            ImGui.TextWrapped(Loc.Localize("Builder Tab Text - Best Exp", "This tool will assist you in calculating the optimal route that can be taken to level the current build. These calculations are based on experience gained per minute and the unlocked sectors."));

            ImGuiHelpers.ScaledDummy(5.0f);

            var spacing = ImGui.CalcTextSize("Optimal").X + 20.0f;

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Builder Tab Info - Breakpoints", "Breakpoints:"));
            ImGui.TextUnformatted("T2");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted(Loc.Localize("Builder Tab Text - T3", "Surveillance required for a chance to get loot from Tier 2"));
            ImGui.TextUnformatted("T3");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted(Loc.Localize("Builder Tab Text - T3", "Surveillance required for a chance to get loot from Tier 3"));
            ImGui.TextUnformatted(Loc.Localize("Terms - Normal","Normal"));
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted(Loc.Localize("Builder Tab Text - Normal", "Retrieval required for normal retrieval level"));
            ImGui.TextUnformatted(Loc.Localize("Terms - Optimal","Optimal"));
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted(Loc.Localize("Builder Tab Text - Optimal", "Retrieval required for optimal retrieval level"));
            ImGui.TextUnformatted(Loc.Localize("Terms - Favor","Favor"));
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted(Loc.Localize("Builder Tab Text - Favor", "Favor required for chance to get two items from a sector"));

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Helpy Tab Info - Colors", "Colors:"));
            ImGui.TextUnformatted(Loc.Localize("Colors - White", "White"));
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted(Loc.Localize("Builder Tab Info - White Text", "Nothing selected or no data available"));
            ImGui.TextColored(ImGuiColors.ParsedGold,Loc.Localize("Colors - White", "Gold"));
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted(Loc.Localize("Builder Tab Info - Gold Text", "Requirement exceeded"));
            ImGui.TextColored(ImGuiColors.HealerGreen,Loc.Localize("Colors - White", "Green"));
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted(Loc.Localize("Builder Tab Info - Green Text", "T3/Optimal/Favor reached"));
            ImGui.TextColored(ImGuiColors.ParsedPink,Loc.Localize("Colors - White", "Pink"));
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted(Loc.Localize("Builder Tab Info - Pink Text", "T2/Normal reached, followed by T3/Optimal"));
            ImGui.TextColored(ImGuiColors.DalamudRed,Loc.Localize("Colors - White", "Red"));
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted(Loc.Localize("Builder Tab Info - Red Text", "Requirement not fulfilled, followed by requirement"));

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Terms - Route", "Route:"));
            ImGui.TextColored(ImGuiColors.DalamudViolet,Loc.Localize("Colors - Violet", "Violet"));
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted(Loc.Localize("Helpy Tab Info - Violet Text", "Unlocked but not visited"));
            ImGui.TextColored(ImGuiColors.DalamudRed,Loc.Localize("Colors - Red", "Red"));
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted(Loc.Localize("Helpy Tab Info - Red Text", "Not unlocked"));

            ImGui.EndTabItem();
        }

        return open;
    }
}
