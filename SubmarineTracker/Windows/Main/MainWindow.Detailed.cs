using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Main;

public partial class MainWindow
{
    private static void DetailedSub(Submarines.Submarine sub)
    {
        ImGuiHelpers.ScaledDummy(5.0f);

        using var indent = ImRaii.PushIndent(10.0f);
        using var table = ImRaii.Table($"##submarineOverview##{sub.Name}", 2);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("##key", 0, 0.2f);
        ImGui.TableSetupColumn("##value");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(Loc.Localize("Terms - Rank", "Rank"));
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{sub.Rank}");

        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(Loc.Localize("Terms - Build", "Build"));
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{sub.Build.FullIdentifier()}");

        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(Loc.Localize("Terms - Repair", "Repair"));
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{sub.Build.RepairCosts}");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{sub.HullCondition:F}% | {sub.SternCondition:F}% | {sub.BowCondition:F}% | {sub.BridgeCondition:F}%");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(Loc.Localize("Main Window Overview - Breaks After", "Breaks after {0} voyages").Format(sub.CalculateUntilRepair()));

        ImGui.TableNextRow();

        if (sub.ValidExpRange())
        {
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Loc.Localize("Terms - Exp", "Exp"));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{sub.CExp} / {sub.NExp}");
            ImGui.SameLine();
            ImGui.TextUnformatted($"{(double)sub.CExp / sub.NExp * 100.0:##0.00}%");

            var predictedExp = sub.PredictExpGrowth();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Loc.Localize("Terms - Predicted", "Predicted"));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{Loc.Localize("Terms - Rank", "Rank")} {predictedExp.Rank} ({predictedExp.Exp:##0.00}%)");
        }

        if (sub.IsOnVoyage())
        {
            AddTableSpacing();

            var time = Loc.Localize("Terms - Done", "Done");
            var returnTime = sub.ReturnTime - DateTime.Now.ToUniversalTime();
            if (returnTime.TotalSeconds > 0)
                time = $"{(int)returnTime.TotalHours:#00}:{returnTime:mm}:{returnTime:ss} {Loc.Localize("Terms - hours", "hours")}";

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Loc.Localize("Terms - Time", "Time"));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(time);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Loc.Localize("Terms - Date", "Date"));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{sub.ReturnTime.ToLocalTime()}");

            var startPoint = Voyage.FindVoyageStart(sub.Points.First());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Loc.Localize("Terms - Map", "Map"));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Utils.SectorToMap(startPoint));

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Loc.Localize("Terms - Route", "Route"));
            ImGui.TableNextColumn();
            ImGui.TextWrapped($"{string.Join(" -> ", sub.Points.Select(p => Utils.NumToLetter(p - startPoint)))}");
        }
        AddTableSpacing();

        if (!Plugin.Configuration.ShowExtendedPartsList)
            return;

        AddTableSpacing();

        ImGui.TableNextColumn();
        Helper.DrawScaledIcon(sub.HullIconId, IconSize);
        ImGui.TableNextColumn();
        ImGui.TextColored(ImGuiColors.ParsedGold, sub.HullName);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        Helper.DrawScaledIcon(sub.SternIconId, IconSize);
        ImGui.TableNextColumn();
        ImGui.TextColored(ImGuiColors.ParsedGold, sub.SternName);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        Helper.DrawScaledIcon(sub.BowIconId, IconSize);
        ImGui.TableNextColumn();
        ImGui.TextColored(ImGuiColors.ParsedGold, sub.BowName);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        Helper.DrawScaledIcon(sub.BridgeIconId, IconSize);
        ImGui.TableNextColumn();
        ImGui.TextColored(ImGuiColors.ParsedGold, sub.BridgeName);
        ImGui.TableNextRow();
    }

    private static void AddTableSpacing()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGuiHelpers.ScaledDummy(10.0f);
        ImGui.TableNextRow();
    }
}
