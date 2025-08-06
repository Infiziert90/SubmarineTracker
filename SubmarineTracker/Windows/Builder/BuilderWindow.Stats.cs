using Dalamud.Utility;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    private void BuildStats(ref Submarine sub)
    {
        using var child = ImRaii.Child("SubStats", Vector2.Zero);
        if (!child.Success)
            return;

        var build = CurrentBuild.GetSubmarineBuild;

        // Reset to custom build if not equal anymore
        if (sub.IsValid() && !build.EqualsSubmarine(sub))
            CurrentBuild.OriginalSub = 0;

        var optimizedDuration = Voyage.CalculateDuration(CurrentBuild.OptimizedRoute, build.Speed);
        var breakpoints = Sectors.CalculateBreakpoint(CurrentBuild.Sectors);
        var expPerMinute = 0.0;
        var totalExp = 0u;
        var repairAfter = 0;
        if (optimizedDuration != 0 && CurrentBuild.OptimizedDistance != 0)
        {
            totalExp = Sectors.CalculateExpForSectors(CurrentBuild.OptimizedRoute, CurrentBuild.GetSubmarineBuild, AvgBonus);
            expPerMinute = totalExp / (optimizedDuration / 60.0);
            repairAfter = CurrentBuild.CalculateUntilRepair();
        }

        var tanks = 0u;
        if (Plugin.AllaganToolsConsumer.IsAvailable)
        {
            // build cache if needed
            Storage.BuildStorageCache();
            if (Storage.StorageCache.TryGetValue(Plugin.ClientState.LocalContentId, out var cachedItems) && cachedItems.TryGetValue((uint)Items.Tanks, out var temp))
                tanks = temp.Count;
        }

        using (var table = ImRaii.Table("##buildColumn", 2, ImGuiTableFlags.SizingFixedFit))
        {
            if (table.Success)
            {
                ImGui.TableSetupColumn("##title");
                ImGui.TableSetupColumn("##content");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Language.BuilderStatsCategoryBuild);

                ImGui.TableNextColumn();
                Helper.TextColored(ImGuiColors.DalamudOrange, $"{CurrentBuild} (Rank {CurrentBuild.Rank})");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Language.BuilderStatsCategoryRoute);

                ImGui.TableNextColumn();
                SelectedRoute();
            }
        }

        ImGui.TextUnformatted("Calculated Stats:");

        using (var table = ImRaii.Table("##statsColumn", 6))
        {
            if (table.Success)
            {
                ImGui.TableSetupColumn("##stat1", 0, 0.55f);
                ImGui.TableSetupColumn("##count1", 0, 0.72f);
                ImGui.TableSetupColumn("##stat2", 0, 0.44f);
                ImGui.TableSetupColumn("##count2", 0, 0.5f);
                ImGui.TableSetupColumn("##stat3", 0, 0.4f);
                ImGui.TableSetupColumn("##count3", 0, 0.5f);

                ImGui.TableNextColumn();
                Helper.TextColored(ImGuiColors.HealerGreen, Language.TermsSurveillance);

                ImGui.TableNextColumn();
                SelectRequiredColor(breakpoints.T2, build.Surveillance, breakpoints.T3);

                ImGui.TableNextColumn();
                Helper.TextColored(ImGuiColors.HealerGreen, Language.TermsRetrieval);

                ImGui.TableNextColumn();
                SelectRequiredColor(breakpoints.Normal, build.Retrieval, breakpoints.Optimal);

                ImGui.TableNextColumn();
                Helper.TextColored(ImGuiColors.HealerGreen, Language.TermsFavor);

                ImGui.TableNextColumn();
                SelectRequiredColor(breakpoints.Favor, build.Favor);

                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                Helper.TextColored(ImGuiColors.HealerGreen, Language.TermsSpeed);

                ImGui.TableNextColumn();
                Helper.TextColored(ImGuiColors.HealerGreen, $"{build.Speed}");

                ImGui.TableNextColumn();
                Helper.TextColored(ImGuiColors.HealerGreen, Language.TermsRange);

                ImGui.TableNextColumn();
                SelectRequiredColor((int)CurrentBuild.OptimizedDistance, build.Range);

                ImGui.TableNextColumn();
                Helper.TextColored(ImGuiColors.HealerGreen, Language.TermsFuel);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{CurrentBuild.FuelCost}{(tanks > 0 ? $" / {tanks}" : "")}");

                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                Helper.TextColored(ImGuiColors.HealerGreen, Language.TermsDuration);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{ToTime(TimeSpan.FromSeconds(optimizedDuration))}");

                ImGui.TableNextColumn();
                Helper.TextColored(ImGuiColors.HealerGreen, Language.TermsExp);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{totalExp:N0}{(AvgBonus ? "*"u8 : ""u8)}");

                ImGui.TableNextColumn();
                Helper.TextColored(ImGuiColors.HealerGreen, Language.TermsExpEachMin);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{expPerMinute:F}");

                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                Helper.TextColored(ImGuiColors.HealerGreen, Language.TermsRepair);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Language.BuilderStatsTextRepairAfter.Format(build.RepairCosts, repairAfter));
            }
        }
    }

    public static void SelectRequiredColor(int minRequired, int current, int maxRequired = -1)
    {
        if (minRequired == 0)
        {
            ImGui.TextUnformatted($"{current}");
        }
        else if (minRequired > current)
        {
            Helper.TextColored(ImGuiColors.DalamudRed, $"{current} ({minRequired})");
        }
        else if (maxRequired == -1)
        {
            if (minRequired == current)
                Helper.TextColored(ImGuiColors.HealerGreen, $"{current}");
            else
                Helper.TextColored(ImGuiColors.ParsedGold, $"{current} ({minRequired})");
        }
        else
        {
            if (maxRequired == current)
                Helper.TextColored(ImGuiColors.HealerGreen, $"{current}");
            else if (current >= minRequired && current < maxRequired)
                Helper.TextColored(ImGuiColors.ParsedPink, $"{current} ({maxRequired})");
            else
                Helper.TextColored(ImGuiColors.ParsedGold, $"{current} ({maxRequired})");
        }
    }

    public void SelectedRoute()
    {
        if (CurrentBuild.OptimizedRoute.Length != 0)
            Helper.TextColored(ImGuiColors.DalamudOrange, SectorsToPath(" -> ", CurrentBuild.OptimizedRoute.Select(s => s.RowId).ToList()));
        else
            Helper.TextColored(ImGuiColors.DalamudOrange, Language.BuilderStatsRouteNoSelection);
    }
}
