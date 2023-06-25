using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    private void BuildStats(ref Submarines.Submarine sub)
    {
        if (ImGui.BeginChild("SubStats", new Vector2(0, 0)))
        {
            var build = CurrentBuild.GetSubmarineBuild;

            // Reset to custom build if not equal anymore
            if (sub.IsValid() && !build.EqualsSubmarine(sub))
                CurrentBuild.OriginalSub = 0;

            var startPoint = ExplorationSheet.First(r => r.Map.Row == CurrentBuild.Map + 1);

            var optimizedPoints = CurrentBuild.OptimizedRoute.Prepend(startPoint).ToList();
            var optimizedDuration = Voyage.CalculateDuration(optimizedPoints, build);
            var breakpoints = Sectors.CalculateBreakpoint(CurrentBuild.Sectors);
            var expPerMinute = 0.0;
            var durationLimitExp = 0.0;
            if (optimizedDuration != 0 && CurrentBuild.OptimizedDistance != 0)
            {
                expPerMinute = Sectors.CalculateExpForSectors(CurrentBuild.OptimizedRoute, CurrentBuild.GetSubmarineBuild) / (optimizedDuration / 60.0);
                durationLimitExp = Sectors.CalculateExpForSectors(CurrentBuild.OptimizedRoute, CurrentBuild.GetSubmarineBuild) / (DateUtil.DurationToTime(Configuration.DurationLimit).TotalMinutes);
            }


            var windowWidth = ImGui.GetWindowWidth();
            var secondRow = windowWidth / 5.1f;
            var thirdRow = windowWidth / 2.9f;
            var fourthRow = windowWidth / 2.0f;
            var sixthRow = windowWidth / 1.5f;
            var seventhRow = windowWidth / 1.25f;

            ImGui.Columns(2, "##buildColumn", false);
            ImGui.SetColumnWidth(0, ImGui.CalcTextSize("Optimized Route:").X + (20 * ImGuiHelpers.GlobalScale));
            ImGui.TextUnformatted("Selected Build:");
            ImGui.NextColumn();
            ImGui.TextColored(ImGuiColors.DalamudOrange, $"{CurrentBuild.FullIdentifier()}");

            ImGui.NextColumn();
            ImGui.TextUnformatted("Optimized Route:");
            ImGui.NextColumn();
            SelectedRoute();
            ImGui.Columns(0);

            ImGui.TextUnformatted("Calculated Stats:");

            ImGui.Columns(6, "##statsColumn", false);
            ImGui.TextColored(ImGuiColors.HealerGreen, $"Surveillance");
            ImGui.NextColumn();
            SelectRequiredColor(breakpoints.T2, build.Surveillance, breakpoints.T3);

            ImGui.NextColumn();
            ImGui.TextColored(ImGuiColors.HealerGreen, $"Retrieval");
            ImGui.NextColumn();
            SelectRequiredColor(breakpoints.Normal, build.Retrieval, breakpoints.Optimal);

            ImGui.NextColumn();
            ImGui.TextColored(ImGuiColors.HealerGreen, $"Favor");
            ImGui.NextColumn();
            SelectRequiredColor(breakpoints.Favor, build.Favor);

            ImGui.NextColumn();
            ImGui.TextColored(ImGuiColors.HealerGreen, $"Speed");
            ImGui.NextColumn();
            ImGui.TextColored(ImGuiColors.HealerGreen, $"{build.Speed}");

            ImGui.NextColumn();
            ImGui.TextColored(ImGuiColors.HealerGreen, $"Range");
            ImGui.NextColumn();
            SelectRequiredColor(CurrentBuild.OptimizedDistance, build.Range);

            ImGui.NextColumn();
            ImGui.TextColored(ImGuiColors.HealerGreen, $"Repair");
            ImGui.NextColumn();
            ImGui.TextUnformatted($"{build.RepairCosts}");

            ImGui.NextColumn();
            ImGui.TextColored(ImGuiColors.HealerGreen, $"Duration");
            ImGui.NextColumn();
            ImGui.TextUnformatted($"{ToTime(TimeSpan.FromSeconds(optimizedDuration))}");

            ImGui.NextColumn();
            ImGui.TextColored(ImGuiColors.HealerGreen, $"Exp/Min");
            ImGui.NextColumn();
            ImGui.TextUnformatted($"{expPerMinute:F}");

            if (Configuration.DurationLimit != DurationLimit.None)
            {
                ImGui.NextColumn();
                ImGui.NextColumn();
                ImGui.NextColumn();
                ImGui.TextColored(ImGuiColors.HealerGreen, $"Limit");
                ImGui.NextColumn();
                ImGui.TextUnformatted($"{DateUtil.GetDurationLimitName(Configuration.DurationLimit)}");

                ImGui.NextColumn();
                ImGui.TextColored(ImGuiColors.HealerGreen, $"Exp/Duration");
                ImGui.NextColumn();
                ImGui.TextUnformatted($"{durationLimitExp:F}");
            }
            ImGui.Columns(0);
        }
        ImGui.EndChild();
    }

    public void SelectRequiredColor(int minRequired, int current, int maxRequired = -1)
    {
        if (minRequired == 0)
        {
            ImGui.TextUnformatted($"{current}");
        }
        else if (minRequired > current)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, $"{current} ({minRequired})");
        }
        else if (maxRequired == -1)
        {
            if (minRequired == current)
                ImGui.TextColored(ImGuiColors.HealerGreen, $"{current}");
            else
                ImGui.TextColored(ImGuiColors.ParsedGold, $"{current} ({minRequired})");
        }
        else
        {
            if (maxRequired == current)
                ImGui.TextColored(ImGuiColors.HealerGreen, $"{current}");
            else if (current >= minRequired && current < maxRequired)
                ImGui.TextColored(ImGuiColors.ParsedPink, $"{current} ({maxRequired})");
            else
                ImGui.TextColored(ImGuiColors.ParsedGold, $"{current} ({maxRequired})");
        }
    }

    public void SelectedRoute()
    {
        if (CurrentBuild.OptimizedRoute.Any())
        {
            var startPoint = ExplorationSheet.First(r => r.Map.Row == CurrentBuild.Map + 1);
            ImGui.TextColored(ImGuiColors.DalamudOrange,
                              string.Join(" -> ",
                                          CurrentBuild.OptimizedRoute
                                                      .Where(p => p.RowId > startPoint.RowId)
                                                      .Select(p => NumToLetter(p.RowId - startPoint.RowId))));
        }
        else
        {
            ImGui.TextColored(ImGuiColors.DalamudOrange, "No Selection");
        }
    }
}
