using Dalamud.Utility;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Main;

public partial class MainWindow
{
    private void All()
    {
        var numberText = "1. ";
        var rankText = $"{Language.TermsRank} 999 ";
        var identifierText = "(WWWW++)";
        var extraText = "[";
        if (Plugin.Configuration.ShowDateInAll)
            extraText += " 24/12/2000 23:59:59 ";
        else
            extraText += " 123:59:59 ";

        if (Plugin.Configuration.ShowRouteInAll)
            extraText += " AA->AB->AC->W->Z ";
        extraText += "]";

        var itemSpacing = ImGui.GetStyle().ItemSpacing.X;
        var indentWidth = 10.0f * ImGuiHelpers.GlobalScale;
        var secondRowWidth = ImGui.CalcTextSize(numberText).X + itemSpacing + ImGui.CalcTextSize(rankText).X + itemSpacing;
        var thirdRowWidth = ImGui.CalcTextSize(identifierText).X + itemSpacing;
        var extraTextWidth =  ImGui.CalcTextSize(extraText).X;

        var numberOfRows = (int)(ImGui.GetContentRegionAvail().X / (indentWidth + secondRowWidth + thirdRowWidth + extraTextWidth + 20.0f * ImGuiHelpers.GlobalScale));
        using var allTable = ImRaii.Table("##allTable", numberOfRows);
        if (!allTable.Success)
            return;

        foreach (var id in Plugin.GetFCOrderWithoutHidden())
        {
            ImGui.TableNextColumn();
            var fc = Plugin.DatabaseCache.GetFreeCompanies()[id];

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
                ImGui.SameLine(indentWidth + secondRowWidth);
                Helper.TextColored(color, $"({sub.Build.FullIdentifier()})");

                ImGui.SameLine(indentWidth + secondRowWidth + thirdRowWidth);

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
                var end = new Vector2(begin.X + textSize.X + indentWidth + secondRowWidth + thirdRowWidth, begin.Y + textSize.Y + ImGui.GetStyle().ItemSpacing.Y);
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
