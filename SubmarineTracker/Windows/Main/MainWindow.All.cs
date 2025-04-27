using Dalamud.Utility;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Main;

public partial class MainWindow
{
    private void All()
    {
        var widthCheck = Plugin.Configuration.ShowRouteInAll && ImGui.GetWindowSize().X < SizeConstraints!.Value.MinimumSize.X + (300.0f * ImGuiHelpers.GlobalScale);

        using var allTable = ImRaii.Table("##allTable", widthCheck ? 1 : 2);
        if (!allTable.Success)
            return;

        foreach (var id in Plugin.GetFCOrderWithoutHidden())
        {
            ImGui.TableNextColumn();
            var fc = Plugin.DatabaseCache.GetFreeCompanies()[id];
            var secondRow = ImGui.GetContentRegionAvail().X / (widthCheck ? 6.0f : Plugin.Configuration.ShowDateInAll ? 3.2f : 2.8f);
            var thirdRow = ImGui.GetContentRegionAvail().X / (widthCheck ? 3.7f : Plugin.Configuration.ShowDateInAll ? 1.9f : 1.6f);

            Helper.TextColored(ImGuiColors.DalamudViolet, $"{Plugin.NameConverter.GetName(fc)}:");
            foreach (var (sub, idx) in Plugin.DatabaseCache.GetSubmarines(id).WithIndex())
            {
                using var indent = ImRaii.PushIndent(10.0f);
                var begin = ImGui.GetCursorScreenPos();

                Helper.TextColored(ImGuiColors.HealerGreen, $"{idx + 1}. ");
                ImGui.SameLine();

                var condition = sub.PredictDurability() > 0;
                var color = condition ? ImGuiColors.TankBlue : ImGuiColors.DalamudYellow;
                Helper.TextColored(color, $"{Language.TermsRank} {sub.Rank}");
                ImGui.SameLine(secondRow);
                Helper.TextColored(color, $"({sub.Build.FullIdentifier()})");

                ImGui.SameLine(thirdRow);

                var route = "";
                var time = $" {Language.TermsNoVoyage} ";
                if (sub.IsOnVoyage())
                {
                    route = Utils.SectorsToPath(" -> ", sub.Points);

                    time = $" {Language.TermsDone} ";
                    var returnTime = sub.ReturnTime - DateTime.Now.ToUniversalTime();
                    if (returnTime.TotalSeconds > 0)
                        time = !Plugin.Configuration.ShowDateInAll ? $" {Utils.ToTime(returnTime)} " : $" {sub.ReturnTime.ToLocalTime()}";
                }

                var fullText = $"[ {time}{(Plugin.Configuration.ShowRouteInAll ? $"   {route}" : "")} ]";
                Helper.TextColored(ImGuiColors.ParsedOrange, fullText);

                var textSize = ImGui.CalcTextSize(fullText);
                var end = new Vector2(begin.X + textSize.X + thirdRow, begin.Y + textSize.Y + ImGui.GetStyle().ItemSpacing.Y);
                if (ImGui.IsMouseHoveringRect(begin, end))
                {
                    var tooltip = condition ? "" : $"{Language.ReturnOverlayTooltipRepairNeeded}\n";
                    tooltip += $"{Language.TermsRank} {sub.Rank}    ({sub.Build.FullIdentifier()})\n";

                    var predictedExp = sub.PredictExpGrowth();
                    tooltip += $"{Language.TermsRoute}: {route}\n";
                    tooltip += $"{Language.TermsEXPAfter}: {predictedExp.Rank} ({predictedExp.Exp:##0.00}%)\n";
                    tooltip += $"{Language.TermsRepair}: {Language.MainWindowTooltipRepair.Format(sub.Build.RepairCosts, sub.CalculateUntilRepair())}";

                    Helper.Tooltip(tooltip);
                }
            }

            ImGuiHelpers.ScaledDummy(5.0f);
        }
    }
}
