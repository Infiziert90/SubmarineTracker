using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    private ExcelSheetSelector<SubmarineExploration>.ExcelSheetPopupOptions LevelingAllowedPopupOptions = null!;
    private CancellationTokenSource CancelSource = new();
    private Thread? Thread;

    private int TargetRank = 85;

    private int SwapAfter = 1;

    private int PossibleBuilds;
    private int Progress;
    private int ProgressRank;

    private bool IgnoreBuild;
    private bool IgnoreShark;
    private bool IgnoreUnmodded;

    private bool Processing;
    private DateTime StartTime;
    private DateTime ProgressStartTime;

    private Dictionary<int, Journey> LastCalc = new();
    private (string Limit, bool IgnoreBuild, bool IgnoreUnlocks, bool MaximizeDurationLimit) LastOptions = ("", false, false, false);

    private bool AllowedChanged;
    private readonly List<uint> AllowedSectors = [];

    private void InitializeLeveling()
    {
        LevelingAllowedPopupOptions = new ExcelSheetSelector<SubmarineExploration>.ExcelSheetPopupOptions
        {
            FormatRow = e => $"{MapToThreeLetter(e.RowId, true)} - {NumToLetter(e.RowId, true)}. {UpperCaseStr(e.Destination)} (Rank {e.RankReq})",
            FilteredSheet = Sheets.ExplorationSheet.Where(r => r is { RankReq: > 0, StartingPoint: false }).Where(r => !AllowedSectors.Contains(r.RowId))
        };
    }

    private bool LevelingTab()
    {
        using var tabItem = ImRaii.TabItem($"{Language.BuilderTabLeveling}##Leveling");
        if (!tabItem.Success)
            return false;

        var avail = ImGui.GetContentRegionAvail().X;
        var width = avail / 2;
        var longText = ImGui.CalcTextSize(Language.BuilderLevelingButtonStopCalculate).X + (20.0f * ImGuiHelpers.GlobalScale);

        Helper.TextColored(ImGuiColors.HealerGreen, $"{Language.TermsBuild}: {(!IgnoreBuild ? $"{CurrentBuild} ({Language.TermsRank} {CurrentBuild.Rank})" : $"{Language.TermsAll}")}");
        ImGui.SetNextItemWidth(width);
        ImGui.SliderInt("##targetRank", ref TargetRank, 15, (int)Sheets.LastRank, $"{Language.TermsTargetRank} %d");
        ImGuiComponents.HelpMarker(Language.BuilderLevelingTooltipTarget);
        ImGui.SameLine(0, 20.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.Button(Language.BuilderLevelingButtonCalculate, new Vector2(longText, 0)))
        {
            CancelSource.Cancel();
            Thread?.Join();
            CancelSource = new CancellationTokenSource();
            StartTime = DateTime.Now;
            ProgressStartTime = DateTime.Now;
            Thread = new Thread(DoThingsOffThread);
            Thread.SetApartmentState(ApartmentState.MTA);
            Thread.Start();
        }

        ImGui.SetNextItemWidth(width);
        ImGui.SliderInt("##swapAfter", ref SwapAfter, 1, 10, $"{Language.TermsSwapAfter} %d");
        ImGuiComponents.HelpMarker(Language.BuilderLevelingTooltipSwap);
        ImGui.SameLine(0, 20.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.Button(Language.BuilderLevelingButtonStopCalculate, new Vector2(longText, 0)))
        {
            CancelSource.Cancel();
            Thread?.Join();
        }


        if (Processing)
        {
            Helper.TextColored(ImGuiColors.DalamudViolet, $"{Language.TermsProgress}:");
            using var indent = ImRaii.PushIndent(10.0f);

            if (IgnoreBuild)
                Helper.WrappedError(Language.BuilderLevelingWarningSlow);
            Helper.TextColored(ImGuiColors.HealerGreen, $"{Language.BuilderLevelingInfoAtRank}: {ProgressRank}");
            Helper.TextColored(ImGuiColors.HealerGreen, $"{Language.BuilderLevelingInfoProgressFor}: {Progress} / {PossibleBuilds}");
            Helper.TextColored(ImGuiColors.HealerGreen, $"{Language.BuilderLevelingInfoElapsedTotal}: {DateTime.Now - StartTime}");
            Helper.TextColored(ImGuiColors.HealerGreen, $"{Language.BuilderLevelingInfoElapsedCurrent}: {DateTime.Now - ProgressStartTime}");
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        width = avail / 3;
        Helper.TextColored(ImGuiColors.DalamudViolet, $"{Language.TermsOptions}:");
        using (ImRaii.PushIndent(10.0f))
        {
            ImGui.Checkbox(Language.BuilderLevelingCheckboxIgnoreBuild, ref IgnoreBuild);
            ImGuiComponents.HelpMarker(Language.BuilderLevelingTooltipIgnoreBuild);
            ImGui.Checkbox(Language.BuilderLevelingCheckboxIgnoreUnlocks, ref IgnoreUnlocks);
            ImGui.Checkbox(Language.BuilderLevelingCheckboxIgnoreShark, ref IgnoreShark);
            ImGuiComponents.HelpMarker(Language.BuilderLevelingTooltipIgnoreShark);
            ImGui.Checkbox(Language.BuilderLevelingCheckboxIgnoreUnmodified, ref IgnoreUnmodded);
            ImGuiComponents.HelpMarker(Language.BuilderLevelingTooltipIgnoreUnmodified);
            ImGui.Checkbox(Language.BuilderLevelingCheckboxAvgExp, ref AvgBonus);
            ImGuiComponents.HelpMarker(Language.BuilderLevelingTooltipAvgExp);
        }

        ImGui.AlignTextToFramePadding();
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.BestEXPEntryDurationLimit);
        using (ImRaii.PushIndent(10.0f))
        {
            if (Plugin.Configuration.DurationLimit != DurationLimit.None)
                ImGui.Checkbox(Language.BestEXPCheckboxMaximizeDuration, ref Plugin.Configuration.MaximizeDuration);

            ImGui.SetNextItemWidth(width);
            using (var combo = ImRaii.Combo("##durationLimitCombo", Plugin.Configuration.DurationLimit.GetName()))
            {
                if (combo.Success)
                {
                    foreach (var durationLimit in Enum.GetValues<DurationLimit>())
                    {
                        if (ImGui.Selectable(durationLimit.GetName()))
                        {
                            Plugin.Configuration.DurationLimit = durationLimit;
                            Plugin.Configuration.Save();

                            OptionsChanged = true;
                        }
                    }
                }
            }

            if (Plugin.Configuration.DurationLimit == DurationLimit.Custom)
            {
                ImGui.SetNextItemWidth(width / 5f);
                if (ImGui.InputInt("##CustomHourInput", ref Plugin.Configuration.CustomHour, 0))
                {
                    Plugin.Configuration.CustomHour = Math.Clamp(Plugin.Configuration.CustomHour, 1, 123);
                    Plugin.Configuration.Save();
                }

                ImGui.SameLine();
                ImGui.TextUnformatted(":");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(width / 5f);
                if (ImGui.InputInt("##CustomMinInput", ref Plugin.Configuration.CustomMinute, 0))
                {
                    Plugin.Configuration.CustomMinute = Math.Clamp(Plugin.Configuration.CustomMinute, 0, 59);
                    Plugin.Configuration.Save();
                }

                ImGui.SameLine();
                ImGui.TextUnformatted(Language.BestEXPEntryHoursandMinutes);
            }
        }

        Helper.TextColored(ImGuiColors.DalamudViolet, $"{Language.TermsAllowedSectors}: {AllowedSectors.Count}");
        using (ImRaii.PushIndent(10.0f))
        {
            if (AllowedChanged)
            {
                AllowedChanged = false;
                ExcelSheetSelector<SubmarineExploration>.FilteredSearchSheet = null!;
            }

            var listHeight = ImGui.GetTextLineHeight() * 6.5f; // 5 items max, we give padding space for 6.5
            using (ImRaii.PushFont(UiBuilder.IconFont))
                ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(30.0f * ImGuiHelpers.GlobalScale, listHeight));

            if (ExcelSheetSelector<SubmarineExploration>.ExcelSheetPopup("LevelingAllowedSectorsAddPopup", out var row, LevelingAllowedPopupOptions))
            {
                var point = Sheets.ExplorationSheet.GetRow(row);
                if (!AllowedSectors.Contains(point.RowId))
                {
                    AllowedSectors.Add(point.RowId);
                    AllowedChanged = true;
                }
            }

            ImGui.SameLine();

            using (var listBox = ImRaii.ListBox("##AllowedSectors", new Vector2(-1, listHeight)))
            {
                if (listBox.Success)
                {
                    foreach (var s in AllowedSectors.ToArray().OrderBy(s => s))
                    {
                        var sector = Sheets.ExplorationSheet.GetRow(s);
                        if (ImGui.Selectable($"{MapToThreeLetter(sector.RowId, true)} - {NumToLetter(sector.RowId, true)}. {UpperCaseStr(sector.Destination)}"))
                        {
                            AllowedChanged = true;
                            AllowedSectors.Remove(sector.RowId);
                        }
                    }
                }
            }
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        if (LastCalc.Count != 0)
        {
            var lastIdx = LastCalc.Last().Key;
            var modifier = new Box.Modifier
            {
                FPadding = new Vector4(7 * ImGuiHelpers.GlobalScale),
                FBorderColor = ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudGrey))
            };

            Helper.TextColored(ImGuiColors.DalamudViolet, Language.BuilderLevelingResultLast);
            using (ImRaii.PushIndent(10.0f))
            {
                Box.SimpleBox(modifier, () =>
                {
                    ImGui.TextUnformatted($"{Language.BuilderLevelingResultStart} {CurrentBuild.Rank} ({CurrentBuild})");
                    ImGui.TextUnformatted($"{Language.BuilderLevelingResultFinal} {LastCalc[lastIdx].RankReached} ({LastCalc[lastIdx].Build})");
                    ImGui.TextUnformatted($"{Language.BuilderLevelingResultVoyages} {lastIdx} ({GetStringFromTimespan(GetTimesFromJourneys(LastCalc.Values))})");
                    ImGui.TextUnformatted($"{Language.BuilderLevelingResultTotal} {LastCalc.Values.Sum(x => x.RouteExp):N0} ({Language.BuilderLevelingResultLeftover} {LastCalc[lastIdx].Leftover:N0})");
                });

                ImGui.SameLine(0, 30.0f * ImGuiHelpers.GlobalScale);

                Box.SimpleBox(modifier, () =>
                {
                    ImGui.TextUnformatted($"{Language.BuilderLevelingResultLimit} {LastOptions.Limit}");
                    ImGui.TextUnformatted($"{Language.BuilderLevelingResultMaximize} {LastOptions.MaximizeDurationLimit}");
                    ImGui.TextUnformatted($"{Language.BuilderLevelingResultAvg} {AvgBonus}");
                    ImGui.TextUnformatted($"{Language.BuilderLevelingResultUnlocks} {LastOptions.IgnoreUnlocks}");
                });
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            if (ImGui.CollapsingHeader(Language.BuilderLevelingResultVoyageList, ImGuiTreeNodeFlags.Framed))
            {
                using var indent = ImRaii.PushIndent(10.0f);
                BoxList.RenderList(LastCalc, modifier, 1.0f, pair =>
                {
                    var (i, (_, rankReached, leftover, routeExp, points, build)) = pair;
                    Helper.TextColored(ImGuiColors.HealerGreen, $"{Language.BuilderLevelingStepBuild} {build}");
                    Helper.TextColored(ImGuiColors.HealerGreen, $"{Language.BuilderLevelingStepVoyage.Format(i)} {MapToThreeLetter(points[0], true)} {SectorsToPath(" -> ", points)}");
                    Helper.TextColored(ImGuiColors.HealerGreen, $"{Language.BuilderLevelingStepGained} {routeExp:N0}");
                    Helper.TextColored(ImGuiColors.HealerGreen, $"{Language.BuilderLevelingStepReached} {rankReached} - {GetRemaindExp(rankReached, leftover):P}%");
                });
            }
        }

        return true;
    }

    public float GetRemaindExp(int rank, uint exp)
    {
        return rank == Sheets.LastRank ? 0 : exp / (float) Sheets.RankSheet.GetRow((uint) rank).ExpToNext;
    }

    public TimeSpan GetTimesFromJourneys(IEnumerable<Journey> journeys) =>
        journeys.Select(t =>
        {
            var build = (Build.RouteBuild) t.Build;

            build.Rank = t.OldRank;
            return TimeSpan.FromSeconds(Voyage.CalculateDuration(Voyage.ToExplorationArray(t.Route), build.GetSubmarineBuild.Speed));
        }).Aggregate(TimeSpan.Zero, (current, timeSpan) => current + timeSpan);

    public void DoThingsOffThread()
    {
        Processing = true;
        PossibleBuilds = 0;
        Progress = 1;

        var outTree = BuildRoute();

        if (CancelSource.IsCancellationRequested || outTree == null)
        {
            Processing = false;
            return;
        }

        LastCalc = outTree;

        LastOptions = (Plugin.Configuration.DurationLimit.GetName(), IgnoreBuild, IgnoreUnlocks, Plugin.Configuration.MaximizeDuration);
        Processing = false;
    }

    private Dictionary<int, Journey>? BuildRoute()
    {
        var routeBuilds = BuildParts();

        var count = 1;
        var outTree = new Dictionary<int, Journey>();
        var lastBuild = (Build: new Build.RouteBuild(), Voyages: 0);
        if (!Plugin.DatabaseCache.GetFreeCompanies().TryGetValue(Plugin.GetFCId, out var fcSub))
            return null;

        var unlocked = fcSub.UnlockedSectors.Where(pair => pair.Value).Select(pair => pair.Key).ToArray();
        var hasAllowed = AllowedSectors.Count != 0;
        var mapBreaks = Sheets.ExplorationSheet
                              .Where(f => Sheets.ExplorationSheet.Where(t => t.StartingPoint).Select(t => t.RowId + 1).Contains(f.RowId))
                              .Where(r => (hasAllowed && AllowedSectors.Contains(r.RowId)) || IgnoreUnlocks || unlocked.Contains(r.RowId))
                              .ToDictionary(t => t.RankReq, t => (int)t.Map.RowId);

        ProgressRank = CurrentBuild.Rank;

        var lastMap = 0;
        var lastBuildRouteRank = 0;
        while (ProgressRank < TargetRank)
        {
            var (_, bestJourney) = outTree.LastOrDefault();
            var leftover = bestJourney?.Leftover ?? 0;
            var curBuild = lastBuild.Build;

            if (lastBuildRouteRank != ProgressRank)
            {
                lastBuildRouteRank = ProgressRank;
                var builds = routeBuilds.Where(t =>
                {
                    var build = t.GetSubmarineBuild.UpdateRank(ProgressRank);
                    return build.HighestRankPart() <= ProgressRank && build is { Speed: >= 20, Range: >= 20 };
                }).Select(t => new Build.RouteBuild(ProgressRank, t)).ToArray();

                var possibleMaps = mapBreaks.Where(t => t.Key <= ProgressRank).Select(t => t.Value - 1).Where(t => t >= lastMap).ToArray();

                Progress = 0;
                PossibleBuilds = builds.Length * possibleMaps.Length;

                bestJourney ??= new Journey(curBuild.Rank, ProgressRank, 0, 0, [0], curBuild.ToString());
                foreach (var build in builds)
                {
                    if (CancelSource.IsCancellationRequested)
                        return null;

                    var routeBuild = build;
                    var taskJourneys = new List<Task<Journey>>();

                    foreach (var possibleMap in possibleMaps)
                        taskJourneys.Add(Task.Run(() => GetJourney(routeBuild, possibleMap, unlocked)));

                    // ReSharper disable once CoVariantArrayConversion
                    try
                    {
                        Task.WaitAll(taskJourneys.ToArray(), CancelSource.Token);
                    }
                    catch
                    {
                        CancelSource.Cancel();
                        Plugin.Log.Error("Failed operation when waiting for tasks!");
                        break;
                    }

                    if (CancelSource.IsCancellationRequested)
                        return null;

                    if (taskJourneys.Count == 0)
                    {
                        Plugin.Log.Error("No journeys returned, cancelling current build!");
                        return null;
                    }

                    // we can still continue if this would be false, we also want to check if allowed list is set
                    var best = taskJourneys.Select(t => t.Result).OrderBy(t => t.RouteExp).Last();
                    if (best.Route.Length != 0 && !hasAllowed)
                        lastMap = (int) Sheets.ExplorationSheet.GetRow(best.Route[0]).Map.RowId - 2;

                    if (bestJourney.RouteExp < best.RouteExp || (bestJourney.RouteExp == best.RouteExp && best.Build == lastBuild.Build.ToString()))
                    {
                        if ((!curBuild.SameBuildWithoutRank(routeBuild) && lastBuild.Voyages >= SwapAfter) || (routeBuild.SameBuildWithoutRank(CurrentBuild) && !IgnoreBuild) || outTree.Count == 0)
                        {
                            curBuild = routeBuild;
                            bestJourney = new Journey(routeBuild.Rank, ProgressRank, best.RouteExp, best.RouteExp, best.Route, routeBuild.ToString());
                        }
                        else if (curBuild.SameBuildWithoutRank(routeBuild))
                        {
                            bestJourney = new Journey(routeBuild.Rank, ProgressRank, best.RouteExp, best.RouteExp, best.Route, routeBuild.ToString());
                        }
                    }
                }
            }

            if (!lastBuild.Build.SameBuildWithoutRank(curBuild))
                lastBuild.Voyages = 0;

            lastBuild.Build = curBuild;
            lastBuild.Voyages++;

            if (CancelSource.IsCancellationRequested)
                return null;

            if (bestJourney!.RouteExp == 0)
            {
                Plugin.Log.Error("Journey returned 0 route exp!");
                return null;
            }

            (ProgressRank, leftover) = Sectors.CalculateRankUp(ProgressRank, leftover + bestJourney.RouteExp);

            bestJourney = bestJourney with { RankReached = ProgressRank, Leftover = leftover };
            outTree.Add(count++, bestJourney);

            if (ProgressRank == 0 || CancelSource.IsCancellationRequested)
                break;
        }

        return CancelSource.IsCancellationRequested ? null : outTree;
    }

    private Journey GetJourney(Build.RouteBuild routeBuild, int possibleMap, uint[] unlocked)
    {
        routeBuild.Map = possibleMap;

        var allowedSectors = AllowedSectors.ToArray();
        var path = Voyage.FindBestRoute(routeBuild, unlocked, [], allowedSectors, IgnoreUnlocks, AvgBonus);
        var exp = Sectors.CalculateExpForSectors(path.PathPretty, routeBuild.GetSubmarineBuild, AvgBonus);

        Progress++;
        return new Journey(routeBuild.Rank, ProgressRank, exp, exp, path.Path, routeBuild.ToString());
    }

    private List<Build.RouteBuild> BuildParts()
    {
        var routeBuilds = new List<Build.RouteBuild>();
        for (var hull = 0; hull < PartsCount; hull++)
        {
            for (var stern = 0; stern < PartsCount; stern++)
            {
                for (var bow = 0; bow < PartsCount; bow++)
                {
                    for (var bridge = 0; bridge < PartsCount; bridge++)
                    {
                        var build = new Build.RouteBuild(1, (hull * 4) + 3, (stern * 4) + 4, (bow * 4) + 1, (bridge * 4) + 2);
                        if (build.GetSubmarineBuild.HighestRankPart() < TargetRank && (build.IsValidSubBuild(CurrentBuild, IgnoreShark, IgnoreUnmodded) || IgnoreBuild))
                            routeBuilds.Add(build);
                    }
                }
            }
        }

        return routeBuilds;
    }

    public record Journey(int OldRank, int RankReached, uint Leftover, uint RouteExp, uint[] Route, string Build);
}
