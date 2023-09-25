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
        if (ImGui.BeginTabItem("Progression"))
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
        if (!ImGui.BeginTabItem("4 Submarines"))
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
            var special = unlockedFrom.Sub ? $"Rank {rank} <Unlocks slot>" : $"Rank {rank}";

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
        if (!ImGui.BeginTabItem("Last Sector"))
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
            var special = unlockedFrom.Sub ? $"Rank {rank} <Unlocks slot>" : unlockedFrom.Map ? $"Rank {rank} <Unlocks map>" : $"Rank {rank}";

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
        if (!ImGui.BeginTabItem("Info"))
            return;

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextColored(ImGuiColors.DalamudViolet, "How to unlock new Sectors?");
        ImGui.TextWrapped("Unlocking new sectors is RNG.\nBy increasing the speed and frequency of voyages, you will have more chances to discover a new sector.");

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextColored(ImGuiColors.DalamudViolet, "How to unlock new maps / slots?");
        ImGui.TextWrapped("You must visit the unlocked sector at least once.\nNote: You won't see more submarines until you filled the previous slot.\ne.g You won't see slot 3 if slot 2 is empty");

        ImGuiHelpers.ScaledDummy(5.0f);

        var spacing = ImGui.CalcTextSize("Violet").X + 20.0f;
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Colors:");
        ImGui.TextColored(ImGuiColors.HealerGreen,"Green");
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted("Unlocked and visited");
        ImGui.TextColored(ImGuiColors.DalamudViolet,"Violet");
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted("Unlocked but not visited");
        ImGui.TextColored(ImGuiColors.DalamudRed,"Red");
        ImGui.SameLine(spacing);
        ImGui.TextUnformatted("Not unlocked");

        ImGui.EndTabItem();
    }
}
