using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Main;

public partial class MainWindow
{
    private void All()
    {
        Plugin.EnsureFCOrderSafety();
        var widthCheck = Plugin.Configuration.ShowRouteInAll && ImGui.GetWindowSize().X < SizeConstraints!.Value.MinimumSize.X + (300.0f * ImGuiHelpers.GlobalScale);

        using var allTable = ImRaii.Table("##allTable", widthCheck ? 1 : 2);
        if (!allTable.Success)
            return;

        foreach (var id in Plugin.Configuration.FCIdOrder)
        {
            ImGui.TableNextColumn();
            var fc = Plugin.DatabaseCache.GetFreeCompanies()[id];
            var secondRow = ImGui.GetContentRegionAvail().X / (widthCheck ? 6.0f : Plugin.Configuration.ShowDateInAll ? 3.2f : 2.8f);
            var thirdRow = ImGui.GetContentRegionAvail().X / (widthCheck ? 3.7f : Plugin.Configuration.ShowDateInAll ? 1.9f : 1.6f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, $"{Plugin.NameConverter.GetName(fc)}:");
            foreach (var (sub, idx) in Plugin.DatabaseCache.GetSubmarines(id).WithIndex())
            {
                using var indent = ImRaii.PushIndent(10.0f);
                var begin = ImGui.GetCursorScreenPos();

                ImGui.TextColored(ImGuiColors.HealerGreen, $"{idx + 1}. ");
                ImGui.SameLine();

                var condition = sub.PredictDurability() > 0;
                var color = condition ? ImGuiColors.TankBlue : ImGuiColors.DalamudYellow;
                ImGui.TextColored(color, $"{Loc.Localize("Terms - Rank", "Rank")} {sub.Rank}");
                ImGui.SameLine(secondRow);
                ImGui.TextColored(color, $"({sub.Build.FullIdentifier()})");

                ImGui.SameLine(thirdRow);

                var route = "";
                var time = $" {Loc.Localize("Terms - No Voyage", "No Voyage")} ";
                if (sub.IsOnVoyage())
                {
                    var startPoint = Voyage.FindVoyageStart(sub.Points.First());
                    route = $"{string.Join(" -> ", sub.Points.Select(p => Utils.NumToLetter(p - startPoint)))}";

                    time = $" {Loc.Localize("Terms - Done", "Done")} ";
                    var returnTime = sub.ReturnTime - DateTime.Now.ToUniversalTime();
                    if (returnTime.TotalSeconds > 0)
                        time = !Plugin.Configuration.ShowDateInAll ? $" {Utils.ToTime(returnTime)} " : $" {sub.ReturnTime.ToLocalTime()}";
                }

                var fullText = $"[ {time}{(Plugin.Configuration.ShowRouteInAll ? $"   {route}" : "")} ]";
                ImGui.TextColored(ImGuiColors.ParsedOrange, fullText);

                var textSize = ImGui.CalcTextSize(fullText);
                var end = new Vector2(begin.X + textSize.X + thirdRow, begin.Y + textSize.Y + 4.0f);
                if (ImGui.IsMouseHoveringRect(begin, end))
                {
                    var tooltip = condition ? "" : $"{Loc.Localize("Return Overlay Tooltip - Repair Needed", "This submarine needs repair on return.")}\n";
                    tooltip +=
                        $"{Loc.Localize("Terms - Rank", "Rank")} {sub.Rank}    ({sub.Build.FullIdentifier()})\n";

                    var predictedExp = sub.PredictExpGrowth();
                    tooltip += $"{Loc.Localize("Terms - Route", "Route")}: {route}\n";
                    tooltip += $"{Loc.Localize("Terms - EXP After", "After")}: {predictedExp.Rank} ({predictedExp.Exp:##0.00}%%)\n";
                    tooltip += $"{Loc.Localize("Terms - Repair", "Repair")}: {Loc.Localize("Main Window Tooltip - Repair", "{0} kits after {1} voyages").Format(sub.Build.RepairCosts, sub.CalculateUntilRepair())}";

                    ImGui.SetTooltip(tooltip);
                }
            }

            ImGuiHelpers.ScaledDummy(5.0f);
        }
    }
}
