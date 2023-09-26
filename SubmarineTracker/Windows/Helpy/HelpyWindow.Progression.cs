using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Helpy;

public partial class HelpyWindow
{
    private List<(uint, Unlocks.UnlockedFrom)> UnlockPath = null!;

    private void InitProgression()
    {
        UnlockPath = Unlocks.FindUnlockPath(Unlocks.SectorToUnlock.Last(s => s.Value.Sector != 9876).Key);
        UnlockPath.Reverse();
    }

    private void ProgressionTab(Submarines.FcSubmarines fcSub)
    {
        if (ImGui.BeginTabItem($"{Loc.Localize("Helpy Tab - Progression", "Progression")}##Progression"))
        {
            if (ImGui.BeginTabBar("##progressionTabBar"))
            {
                AllSlotsTab(fcSub);

                LastSectorTab(fcSub);

                InfoTab();

                ImGui.EndTabBar();
            }

            ImGui.EndTabItem();
        }
    }

    private void AllSlotsTab(Submarines.FcSubmarines fcSub)
    {
        if (!ImGui.BeginTabItem($"{Loc.Localize("Progression Tab - Submarine Path", "4 Submarines")}##SubmarinesPath"))
            return;

        var textHeight = ImGui.CalcTextSize("XXX").Y * 3.5f; // 3.5 items padding
        var unlockPath = Unlocks.FindUnlockPath(20);
        unlockPath.Reverse();

        var mod = new Box.Modifier();
        mod.Padding(10 * ImGuiHelpers.GlobalScale);
        var bColor = ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudGrey));
        mod.BorderColor(bColor);

        ImGuiHelpers.ScaledDummy(10.0f);
        BoxList.RenderList(unlockPath, mod, 2f, tuple =>
        {
            var (point, unlockedFrom) = tuple;
            var explorationPoint = ExplorationSheet.GetRow(point)!;
            var startPoint = Voyage.FindVoyageStart(explorationPoint.RowId);

            var letter = Utils.NumToLetter(explorationPoint.RowId - startPoint);
            var dest = Utils.UpperCaseStr(explorationPoint.Destination);
            var rank = explorationPoint.RankReq;
            var special = unlockedFrom.Sub ? $"{Loc.Localize("Terms - Rank", "Rank")} {rank} <{Loc.Localize("Progression Tab Tooltip - Unlocks Slot", "Unlocks slot")}>" : $"{Loc.Localize("Terms - Rank", "Rank")} {rank}";

            fcSub.UnlockedSectors.TryGetValue(point, out var hasUnlocked);
            fcSub.ExploredSectors.TryGetValue(point, out var hasExplored);
            var color = hasUnlocked
                            ? hasExplored ? ImGuiColors.HealerGreen : ImGuiColors.DalamudViolet
                            : ImGuiColors.DalamudRed;

            ImGui.BeginChild($"##{point}", new Vector2(150.0f * ImGuiHelpers.GlobalScale, textHeight));
            ImGui.PushTextWrapPos();
            ImGui.TextColored(color, $"{letter}. {dest}");
            ImGui.PopTextWrapPos();
            ImGui.TextColored(ImGuiColors.TankBlue, special);
            ImGui.EndChild();
        });

        ImGui.EndTabItem();
    }

    private void LastSectorTab(Submarines.FcSubmarines fcSub)
    {
        if (!ImGui.BeginTabItem($"{Loc.Localize("Progression Tab - Last Sector", "Last Sector")}##LastSector"))
            return;

        var textHeight = ImGui.CalcTextSize("XXX").Y * 3.5f; // 3.5 items padding

        var mod = new Box.Modifier();
        mod.Padding(10 * ImGuiHelpers.GlobalScale);
        var bColor = ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudGrey));
        mod.BorderColor(bColor);

        ImGuiHelpers.ScaledDummy(10.0f);
        BoxList.RenderList(UnlockPath, mod, 2f, tuple =>
        {
            var (point, unlockedFrom) = tuple;
            var explorationPoint = ExplorationSheet.GetRow(point)!;
            var startPoint = Voyage.FindVoyageStart(explorationPoint.RowId);

            var letter = Utils.NumToLetter(explorationPoint.RowId - startPoint);
            var dest = Utils.UpperCaseStr(explorationPoint.Destination);
            var rank = explorationPoint.RankReq;
            var special = unlockedFrom.Sub
                              ? $"{Loc.Localize("Terms - Rank", "Rank")} {rank} <{Loc.Localize("Progression Tab Tooltip - Unlocks Slot", "Unlocks slot")}>"
                                : unlockedFrom.Map ? $"{Loc.Localize("Terms - Rank", "Rank")} {rank} <{Loc.Localize("Progression Tab Tooltip - Unlocks Map", "Unlocks map")}>"
                                  : $"{Loc.Localize("Terms - Rank", "Rank")} {rank}";

            fcSub.UnlockedSectors.TryGetValue(point, out var hasUnlocked);
            fcSub.ExploredSectors.TryGetValue(point, out var hasExplored);
            var color = hasUnlocked
                            ? hasExplored ? ImGuiColors.HealerGreen : ImGuiColors.DalamudViolet
                            : ImGuiColors.DalamudRed;

            ImGui.BeginChild($"##{point}", new Vector2(150.0f * ImGuiHelpers.GlobalScale, textHeight));
            ImGui.PushTextWrapPos();
            ImGui.TextColored(color, $"{letter}. {dest}");
            ImGui.PopTextWrapPos();
            ImGui.TextColored(ImGuiColors.TankBlue, special);
            ImGui.EndChild();
        });

        ImGui.EndTabItem();
    }

    private static void InfoTab()
    {
        if (!ImGui.BeginTabItem($"{Loc.Localize("Helpy Tab - Info", "Info")}##Info"))
            return;

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Helpy Tab Info - Sector Unlock", "How to unlock new Sectors?"));
        ImGui.TextWrapped(Loc.Localize("Helpy Tab Info - Sector Unlock Text", "Unlocking new sectors is RNG.\nBy increasing the speed and frequency of voyages, you will have more chances to discover a new sector."));

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Helpy Tab Info - Map Unlock", "How to unlock new maps / slots?"));
        ImGui.TextWrapped(Loc.Localize("Helpy Tab Info - Map Unlock Text", "You must visit the unlocked sector at least once.\nNote: You won't see more submarines until you filled the previous slot.\ne.g You won't see slot 3 if slot 2 is empty"));

        ImGuiHelpers.ScaledDummy(5.0f);

        var spacing = ImGui.CalcTextSize("Violet").X + 20.0f;
        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Helpy Tab Info - Colors", "Colors:"));
        ImGui.TextColored(ImGuiColors.HealerGreen,Loc.Localize("Colors - Green", "Green"));
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Loc.Localize("Helpy Tab Info - Green Text", "Unlocked and visited"));
        ImGui.TextColored(ImGuiColors.DalamudViolet,Loc.Localize("Colors - Violet", "Violet"));
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Loc.Localize("Helpy Tab Info - Violet Text", "Unlocked but not visited"));
        ImGui.TextColored(ImGuiColors.DalamudRed,Loc.Localize("Colors - Red", "Red"));
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted(Loc.Localize("Helpy Tab Info - Red Text", "Not unlocked"));

        ImGui.EndTabItem();
    }
}
