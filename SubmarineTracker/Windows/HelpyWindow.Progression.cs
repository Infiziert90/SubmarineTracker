using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;
using Lumina.Excel;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows;

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
        foreach (var (point, idx) in unlockPath.Select((val, i) => (val, i)))
        {
            var explorationPoint = ExplorationSheet.GetRow(point.Item1)!;
            var startPoint = Submarines.FindVoyageStartPoint(explorationPoint.RowId);

            var letter = Utils.NumToLetter(explorationPoint.RowId - startPoint);
            var dest = Utils.UpperCaseStr(explorationPoint.Destination);
            var special = point.Item2.Sub ? "Unlocks slot" : "";

            Box.SimpleBox(mod, () =>
            {
                fcSub.UnlockedSectors.TryGetValue(point.Item1, out var hasUnlocked);
                fcSub.ExploredSectors.TryGetValue(point.Item1, out var hasExplored);
                var color = hasUnlocked
                                ? hasExplored ? ImGuiColors.HealerGreen : ImGuiColors.DalamudViolet
                                : ImGuiColors.DalamudRed;

                ImGui.BeginChild($"##{point.Item1}", new Vector2(150.0f * ImGuiHelpers.GlobalScale, textHeight * ImGuiHelpers.GlobalScale));
                ImGui.PushTextWrapPos();
                ImGui.TextColored(color, $"({letter}) {dest}");
                ImGui.PopTextWrapPos();
                ImGui.TextColored(ImGuiColors.TankBlue, special);
                ImGui.EndChild();
            });

            if (unlockPath.Count > idx + 1)
            {
                ImGui.SameLine();
                var drawList = ImGui.GetWindowDrawList();
                var topPadding = mod.FPadding.X;
                var insideHeight = textHeight * ImGuiHelpers.GlobalScale;

                var p = ImGui.GetCursorScreenPos();
                ImGuiHelpers.ScaledDummy(30.0f, 0);

                drawList.AddTriangle(new Vector2(p.X, p.Y + topPadding),
                                     new Vector2(p.X + (30.0f * ImGuiHelpers.GlobalScale),
                                                 p.Y + topPadding + (insideHeight / 2)),
                                     new Vector2(p.X, p.Y + topPadding + insideHeight),
                                     bColor);

                var numPerLine = CalculateNumberPerLine();
                if (idx == 0 || idx % numPerLine != numPerLine - 1)
                    ImGui.SameLine();
                else
                    ImGuiHelpers.ScaledDummy(0, 20.0f);
            }
        }
    }

    private void LastSectorTab(Submarines.FcSubmarines fcSub)
    {
        var textHeight = ImGui.CalcTextSize("XXX").Y * 3.5f; // 3.5 items padding
        var unlockPath = Unlocks.FindUnlockPath(Unlocks.PointToUnlockPoint.Last().Key);
        unlockPath.Reverse();

        var mod = new Box.Modifier();
        mod.Padding(10 * ImGuiHelpers.GlobalScale);
        var bColor = ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudGrey));
        mod.BorderColor(bColor);

        ImGuiHelpers.ScaledDummy(10.0f);
        foreach (var (point, idx) in unlockPath.Select((val, i) => (val, i)))
        {
            var explorationPoint = ExplorationSheet.GetRow(point.Item1)!;
            var startPoint = Submarines.FindVoyageStartPoint(explorationPoint.RowId);

            var letter = Utils.NumToLetter(explorationPoint.RowId - startPoint);
            var dest = Utils.UpperCaseStr(explorationPoint.Destination);
            var special = point.Item2.Sub ? "Unlocks slot" : point.Item2.Map ? "Unlocks new map" : "";

            Box.SimpleBox(mod, () =>
            {
                fcSub.UnlockedSectors.TryGetValue(point.Item1, out var hasUnlocked);
                fcSub.ExploredSectors.TryGetValue(point.Item1, out var hasExplored);
                var color = hasUnlocked
                                ? hasExplored ? ImGuiColors.HealerGreen : ImGuiColors.DalamudViolet
                                : ImGuiColors.DalamudRed;

                ImGui.BeginChild($"##{point.Item1}", new Vector2(150.0f * ImGuiHelpers.GlobalScale, textHeight * ImGuiHelpers.GlobalScale));
                ImGui.PushTextWrapPos();
                ImGui.TextColored(color, $"({letter}) {dest}");
                ImGui.PopTextWrapPos();
                ImGui.TextColored(ImGuiColors.TankBlue, special);
                ImGui.EndChild();
            });

            if (unlockPath.Count > idx + 1)
            {
                ImGui.SameLine();
                var drawList = ImGui.GetWindowDrawList();
                var topPadding = mod.FPadding.X;
                var insideHeight = textHeight * ImGuiHelpers.GlobalScale;

                var p = ImGui.GetCursorScreenPos();
                ImGuiHelpers.ScaledDummy(30.0f, 0);

                drawList.AddTriangle(new Vector2(p.X, p.Y + topPadding),
                                     new Vector2(p.X + (30.0f * ImGuiHelpers.GlobalScale),
                                                 p.Y + topPadding + (insideHeight / 2)),
                                     new Vector2(p.X, p.Y + topPadding + insideHeight),
                                     bColor);

                var numPerLine = CalculateNumberPerLine();
                if (idx == 0 || idx % numPerLine != numPerLine - 1)
                    ImGui.SameLine();
                else
                    ImGuiHelpers.ScaledDummy(0, 20.0f);
            }
        }
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
