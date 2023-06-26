using Lumina.Excel;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Helpy;

public partial class HelpyWindow
{
    public static ExcelSheet<SubmarineExplorationPretty> ExplorationSheet = null!;

    private void ProgressionTab(Submarines.FcSubmarines fcSub)
    {
        if (ImGui.BeginTabItem("Progression"))
        {
            if (ImGui.BeginTabBar("##progressionTabBar"))
            {
                if (ImGui.BeginTabItem("4 Submarines"))
                {
                    AllSlotsTab(fcSub);

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Last Sector"))
                {
                    LastSectorTab(fcSub);

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Info"))
                {
                    InfoTab();
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();

            ImGui.EndTabItem();
        }
    }

    private void AllSlotsTab(Submarines.FcSubmarines fcSub)
    {
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
            var startPoint = Voyage.FindVoyageStartPoint(explorationPoint.RowId);

            var letter = Utils.NumToLetter(explorationPoint.RowId - startPoint);
            var dest = Utils.UpperCaseStr(explorationPoint.Destination);
            var rank = explorationPoint.RankReq;
            var special = unlockedFrom.Sub ? $"Rank {rank} <Unlocks slot>" : $"Rank {rank}";
            
            fcSub.UnlockedSectors.TryGetValue(point, out var hasUnlocked);
            fcSub.ExploredSectors.TryGetValue(point, out var hasExplored);
            var color = hasUnlocked
                            ? hasExplored ? ImGuiColors.HealerGreen : ImGuiColors.DalamudViolet
                            : ImGuiColors.DalamudRed;

            ImGui.BeginChild($"##{point}", new Vector2(150.0f * ImGuiHelpers.GlobalScale, textHeight * ImGuiHelpers.GlobalScale));
            ImGui.PushTextWrapPos();
            ImGui.TextColored(color, $"{letter}. {dest}");
            ImGui.PopTextWrapPos();
            ImGui.TextColored(ImGuiColors.TankBlue, special);
            ImGui.EndChild();
        });
    }

    private void LastSectorTab(Submarines.FcSubmarines fcSub)
    {
        var textHeight = ImGui.CalcTextSize("XXX").Y * 3.5f; // 3.5 items padding
        var unlockPath = Unlocks.FindUnlockPath(Unlocks.PointToUnlockPoint.Last(s => s.Value.Point != 9876).Key);
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
            var startPoint = Voyage.FindVoyageStartPoint(explorationPoint.RowId);

            var letter = Utils.NumToLetter(explorationPoint.RowId - startPoint);
            var dest = Utils.UpperCaseStr(explorationPoint.Destination);
            var rank = explorationPoint.RankReq;
            var special = unlockedFrom.Sub ? $"Rank {rank} <Unlocks slot>" : unlockedFrom.Map ? $"Rank {rank} <Unlocks map>" : $"Rank {rank}";
            
            fcSub.UnlockedSectors.TryGetValue(point, out var hasUnlocked);
            fcSub.ExploredSectors.TryGetValue(point, out var hasExplored);
            var color = hasUnlocked
                            ? hasExplored ? ImGuiColors.HealerGreen : ImGuiColors.DalamudViolet
                            : ImGuiColors.DalamudRed;

            ImGui.BeginChild($"##{point}", new Vector2(150.0f * ImGuiHelpers.GlobalScale, textHeight * ImGuiHelpers.GlobalScale));
            ImGui.PushTextWrapPos();
            ImGui.TextColored(color, $"{letter}. {dest}");
            ImGui.PopTextWrapPos();
            ImGui.TextColored(ImGuiColors.TankBlue, special);
            ImGui.EndChild();
        });
    }

    private static void InfoTab()
    {
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
    }

    private static int CalculateNumberPerLine() => (int) (ImGui.GetWindowWidth() / (215.0f * ImGuiHelpers.GlobalScale));
}
