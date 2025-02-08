using Dalamud.Utility;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Main;

public partial class MainWindow
{
    private static void DetailedSub(Submarine sub)
    {
        ImGuiHelpers.ScaledDummy(5.0f);

        using var indent = ImRaii.PushIndent(10.0f);
        using var table = ImRaii.Table($"##submarineOverview##{sub.Name}", 2);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("##key", 0, 0.2f);
        ImGui.TableSetupColumn("##value");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(Language.TermsRank);
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{sub.Rank}");

        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(Language.TermsBuild);
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{sub.Build.FullIdentifier()}");

        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(Language.TermsRepair);
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{sub.Build.RepairCosts}");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{sub.HullCondition:F}% | {sub.SternCondition:F}% | {sub.BowCondition:F}% | {sub.BridgeCondition:F}%");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(Language.MainWindowOverviewBreaksAfter.Format(sub.CalculateUntilRepair()));

        ImGui.TableNextRow();

        if (sub.ValidExpRange())
        {
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Language.TermsExp);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{sub.CExp} / {sub.NExp}");
            ImGui.SameLine();
            ImGui.TextUnformatted($"{(double) sub.CExp / sub.NExp * 100.0:##0.00}%");

            var predictedExp = sub.PredictExpGrowth();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Language.TermsPredicted);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{Language.TermsRank} {predictedExp.Rank} ({predictedExp.Exp:##0.00}%)");
        }

        if (sub.IsOnVoyage())
        {
            AddTableSpacing();

            var time = Language.TermsDone;
            var returnTime = sub.ReturnTime - DateTime.Now.ToUniversalTime();
            if (returnTime.TotalSeconds > 0)
                time = $"{(int)returnTime.TotalHours:#00}:{returnTime:mm}:{returnTime:ss} {Language.Termshours}";

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Language.TermsTime);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(time);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Language.TermsDate);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{sub.ReturnTime.ToLocalTime()}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Language.TermsMap);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Voyage.SectorToMapName(sub.Points[0]));

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Language.TermsRoute);
            ImGui.TableNextColumn();
            ImGui.TextWrapped($"{Utils.SectorsToPath(" -> ", sub.Points)}");
        }
        AddTableSpacing();

        if (!Plugin.Configuration.ShowExtendedPartsList)
            return;

        AddTableSpacing();

        ImGui.TableNextColumn();
        Helper.DrawScaledIcon(sub.HullIconId, IconSize);
        ImGui.TableNextColumn();
        Helper.TextColored(ImGuiColors.ParsedGold, sub.HullName);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        Helper.DrawScaledIcon(sub.SternIconId, IconSize);
        ImGui.TableNextColumn();
        Helper.TextColored(ImGuiColors.ParsedGold, sub.SternName);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        Helper.DrawScaledIcon(sub.BowIconId, IconSize);
        ImGui.TableNextColumn();
        Helper.TextColored(ImGuiColors.ParsedGold, sub.BowName);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        Helper.DrawScaledIcon(sub.BridgeIconId, IconSize);
        ImGui.TableNextColumn();
        Helper.TextColored(ImGuiColors.ParsedGold, sub.BridgeName);
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
