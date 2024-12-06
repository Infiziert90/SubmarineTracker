using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using SubmarineTracker.Data;

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
    private readonly List<SubmarineExploration> AllowedSectors = [];

    private void InitializeLeveling()
    {
        LevelingAllowedPopupOptions = new ExcelSheetSelector<SubmarineExploration>.ExcelSheetPopupOptions
        {
            FormatRow = e => $"{MapToThreeLetter(e.RowId, true)} - {NumToLetter(e.RowId, true)}. {UpperCaseStr(e.Destination)} (Rank {e.RankReq})",
            FilteredSheet = Sheets.ExplorationSheet.Where(r => r.RankReq > 0).Where(r => !r.StartingPoint).Where(r => !AllowedSectors.Contains(r))
        };
    }

    private bool LevelingTab()
    {
        if (ImGui.BeginTabItem($"{Loc.Localize("Builder Tab - Leveling", "Leveling")}##Leveling"))
        {
            var avail = ImGui.GetContentRegionAvail().X;
            var width = avail / 2;
            var longText = ImGui.CalcTextSize(Loc.Localize("Builder Leveling Button - Stop Calculate", "Stop Calculation")).X + (20.0f * ImGuiHelpers.GlobalScale);

            ImGui.TextColored(ImGuiColors.HealerGreen, $"{Loc.Localize("Terms - Build", "Build")}: {(!IgnoreBuild ? $"{CurrentBuild} ({Loc.Localize("Terms - Rank", "Rank")} {CurrentBuild.Rank})" : $"{Loc.Localize("Terms - All", "All")}")}");
            ImGui.SetNextItemWidth(width);
            ImGui.SliderInt("##targetRank", ref TargetRank, 15, (int)Sheets.LastRank, $"{Loc.Localize("Terms - Target Rank", "Target Rank")} %d");
            ImGuiComponents.HelpMarker(Loc.Localize("Builder Leveling Tooltip - Target", "The rank this calculation must reach, but can overshot."));
            ImGui.SameLine(0, 20.0f * ImGuiHelpers.GlobalScale);
            if (ImGui.Button(Loc.Localize("Builder Leveling Button - Calculate","Calculate"), new Vector2(longText, 0)))
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
            ImGui.SliderInt("##swapAfter", ref SwapAfter, 1, 10, $"{Loc.Localize("Terms - Swap After", "Swap After")} %d");
            ImGuiComponents.HelpMarker(Loc.Localize("Builder Leveling Tooltip - Swap", "Swaps parts after X voyages if optimal."));
            ImGui.SameLine(0, 20.0f * ImGuiHelpers.GlobalScale);
            if (ImGui.Button(Loc.Localize("Builder Leveling Button - Stop Calculate","Stop Calculation"), new Vector2(longText, 0)))
            {
                CancelSource.Cancel();
                Thread?.Join();
            }


            if (Processing)
            {
                ImGui.TextColored(ImGuiColors.DalamudViolet, $"{Loc.Localize("Terms - Progress", "Progress")}:");
                ImGuiHelpers.ScaledIndent(10.0f);
                if (IgnoreBuild)
                    Helper.WrappedError(Loc.Localize("Builder Leveling Warning - Slow", "Warning: This will take a long time and you'll experience game slowdown"));
                ImGui.TextColored(ImGuiColors.HealerGreen, $"{Loc.Localize("Builder Leveling Info - At Rank", "At Rank")}: {ProgressRank}");
                ImGui.TextColored(ImGuiColors.HealerGreen, $"{Loc.Localize("Builder Leveling Info - Progress For", "Progress for current")}: {Progress} / {PossibleBuilds}");
                ImGui.TextColored(ImGuiColors.HealerGreen, $"{Loc.Localize("Builder Leveling Info - Elapsed Total", "Time elapsed")}: {DateTime.Now - StartTime}");
                ImGui.TextColored(ImGuiColors.HealerGreen, $"{Loc.Localize("Builder Leveling Info - Elapsed Current", "Time elapsed current calculation")}: {DateTime.Now - ProgressStartTime}");
                ImGuiHelpers.ScaledIndent(-10.0f);
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            width = avail / 3;
            ImGui.TextColored(ImGuiColors.DalamudViolet, $"{Loc.Localize("Terms - Options", "Options")}:");
            ImGuiHelpers.ScaledIndent(10.0f);
            ImGui.Checkbox(Loc.Localize("Builder Leveling Checkbox - Ignore Build","Ignore Build"), ref IgnoreBuild);
            ImGuiComponents.HelpMarker(Loc.Localize("Builder Leveling Tooltip - Ignore Build","This will calculate every single possible build\nWarning: This will take a long time and you'll experience game slowdown"));
            ImGui.Checkbox(Loc.Localize("Builder Leveling Checkbox - Ignore Unlocks","Ignore Unlocks"), ref IgnoreUnlocks);
            ImGui.Checkbox(Loc.Localize("Builder Leveling Checkbox - Ignore Shark","Ignore Shark Parts"), ref IgnoreShark);
            ImGuiComponents.HelpMarker(Loc.Localize("Builder Leveling Tooltip - Ignore Shark","Leveling expects that a shark part exists, so it takes that as priority over worse parts" +
                                                        "\nThis option disables the behaviour" +
                                                        "\nNOTE: Your Submarine must be Rank of the highest Rank Part, or this will endlessly loop." +
                                                        "\ne.g SSUW must be Rank higher or equal to 25" +
                                                        "\nImportant: This can lead to errors"));
            ImGui.Checkbox(Loc.Localize("Builder Leveling Checkbox - Ignore Unmodified","Ignore Unmodified Parts"), ref IgnoreUnmodded);
            ImGuiComponents.HelpMarker(Loc.Localize("Builder Leveling Tooltip - Ignore Unmodified","Leveling expects that an unmodified part exists, so it takes that as priority over any modified part if better" +
                                                        "\nThis option disables the behaviour" +
                                                        "\nImportant: This can lead to errors"));
            ImGui.Checkbox(Loc.Localize("Builder Leveling Checkbox - Avg Exp","Use Avg Exp Bonus"), ref AvgBonus);
            ImGuiComponents.HelpMarker(Loc.Localize("Builder Leveling Tooltip - Avg Exp","This calculation normally takes only guaranteed retrieval bonus into account.\nWith this option it will take the avg of possible bonus"));
            ImGuiHelpers.ScaledIndent(-10.0f);

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Best EXP Entry - Duration Limit", "Duration Limit"));
            ImGuiHelpers.ScaledIndent(10.0f);
            if (Plugin.Configuration.DurationLimit != DurationLimit.None)
                ImGui.Checkbox(Loc.Localize("Best EXP Checkbox - Maximize Duration", "Maximize Duration"), ref Plugin.Configuration.MaximizeDuration);
            ImGui.SetNextItemWidth(width);
            if (ImGui.BeginCombo($"##durationLimitCombo", Plugin.Configuration.DurationLimit.GetName()))
            {
                foreach (var durationLimit in (DurationLimit[])Enum.GetValues(typeof(DurationLimit)))
                {
                    if (ImGui.Selectable(durationLimit.GetName()))
                    {
                        Plugin.Configuration.DurationLimit = durationLimit;
                        Plugin.Configuration.Save();

                        OptionsChanged = true;
                    }
                }

                ImGui.EndCombo();
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
                ImGui.TextUnformatted(Loc.Localize("Best EXP Entry - Hours and Minutes", "Hours & Minutes"));
            }
            ImGuiHelpers.ScaledIndent(-10.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, $"{Loc.Localize("Terms - Allowed Sectors", "Allowed Sectors")}: {AllowedSectors.Count}");
            ImGuiHelpers.ScaledIndent(10.0f);
            if (AllowedChanged)
            {
                AllowedChanged = false;
                ExcelSheetSelector<SubmarineExploration>.FilteredSearchSheet = null!;
            }

            var listHeight = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(30.0f * ImGuiHelpers.GlobalScale, listHeight));
            ImGui.PopFont();

            if (ExcelSheetSelector<SubmarineExploration>.ExcelSheetPopup("LevelingAllowedSectorsAddPopup", out var row, LevelingAllowedPopupOptions))
            {
                var point = Sheets.ExplorationSheet.GetRow(row)!;
                if (!AllowedSectors.Contains(point))
                {
                    AllowedSectors.Add(point);
                    AllowedChanged = true;
                }
            }

            ImGui.SameLine();

            if (ImGui.BeginListBox("##AllowedSectors", new Vector2(-1, listHeight)))
            {
                foreach (var p in AllowedSectors.ToArray().OrderBy(p => p.RowId))
                {
                    if (ImGui.Selectable($"{MapToThreeLetter(p.RowId, true)} - {NumToLetter(p.RowId, true)}. {UpperCaseStr(p.Destination)}"))
                    {
                        AllowedChanged = true;
                        AllowedSectors.Remove(p);
                    }
                }

                ImGui.EndListBox();
            }
            ImGuiHelpers.ScaledIndent(-10.0f);

            ImGuiHelpers.ScaledDummy(5.0f);

            if (LastCalc.Count != 0)
            {
                var lastIdx = LastCalc.Last().Key;
                var modifier = new Box.Modifier { FPadding = new Vector4(7 * ImGuiHelpers.GlobalScale), FBorderColor = ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudGrey)) };

                ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Builder Leveling Result - Last","Last Calculation:"));
                ImGuiHelpers.ScaledIndent(10.0f);
                Box.SimpleBox(modifier, () =>
                {
                    ImGui.TextUnformatted($"{Loc.Localize("Builder Leveling Result - Start","Start Rank:")} {CurrentBuild.Rank} ({CurrentBuild})");
                    ImGui.TextUnformatted($"{Loc.Localize("Builder Leveling Result - Final","Final Rank:")} {LastCalc[lastIdx].RankReached} ({LastCalc[lastIdx].Build})");
                    ImGui.TextUnformatted($"{Loc.Localize("Builder Leveling Result - Voyages","Voyages:")} {lastIdx} ({GetStringFromTimespan(GetTimesFromJourneys(LastCalc.Values))})");
                    ImGui.TextUnformatted($"{Loc.Localize("Builder Leveling Result - Total","Exp Total:")} {LastCalc.Values.Sum(x => x.RouteExp):N0} ({Loc.Localize("Builder Leveling Result - Leftover","Leftover:")} {LastCalc[lastIdx].Leftover:N0})");
                });

                ImGui.SameLine(0, 30.0f * ImGuiHelpers.GlobalScale);

                Box.SimpleBox(modifier, () =>
                {
                    ImGui.TextUnformatted($"{Loc.Localize("Builder Leveling Result - Limit","Limit:")} {LastOptions.Limit}");
                    ImGui.TextUnformatted($"{Loc.Localize("Builder Leveling Result - Maximize","Maximize Limit:")} {LastOptions.MaximizeDurationLimit}");
                    ImGui.TextUnformatted($"{Loc.Localize("Builder Leveling Result - Avg","Avg Bonus:")} {AvgBonus}");
                    ImGui.TextUnformatted($"{Loc.Localize("Builder Leveling Result - Unlocks","Ignore Unlocks:")} {LastOptions.IgnoreUnlocks}");
                });
                ImGuiHelpers.ScaledIndent(-10.0f);

                ImGuiHelpers.ScaledDummy(5.0f);

                if (ImGui.CollapsingHeader(Loc.Localize("Builder Leveling Result - Voyage List","Voyage List:"), ImGuiTreeNodeFlags.Framed))
                {
                    ImGuiHelpers.ScaledIndent(10.0f);
                    BoxList.RenderList(LastCalc, modifier, 1.0f, pair =>
                    {
                        var (i, (_, rankReached, leftover, routeExp, points, build)) = pair;
                        ImGui.TextColored(ImGuiColors.HealerGreen, $"{Loc.Localize("Builder Leveling Step - Build","Build:")} {build}");
                        ImGui.TextColored(ImGuiColors.HealerGreen, $"{Loc.Localize("Builder Leveling Step - Voyage","Voyage {0}:").Format(i)} {MapToThreeLetter(points[0], true)} {PointsToVoyage(" -> ", points)}");
                        ImGui.TextColored(ImGuiColors.HealerGreen, $"{Loc.Localize("Builder Leveling Step - Gained","Exp Gained:")} {routeExp:N0}");
                        ImGui.TextColored(ImGuiColors.HealerGreen, $"{Loc.Localize("Builder Leveling Step - Reached","Rank Reached:")} {rankReached} - {GetRemaindExp(rankReached, leftover):P}%");
                    });
                    ImGuiHelpers.ScaledIndent(-10.0f);
                }
            }

            ImGui.EndTabItem();
            return true;
        }

        return false;
    }

    public float GetRemaindExp(int rank, uint exp)
    {
        return rank == Sheets.LastRank ? 0 : exp / (float)Sheets.RankSheet.GetRow((uint)rank).ExpToNext;
    }

    public TimeSpan GetTimesFromJourneys(IEnumerable<Journey> journeys) =>
        journeys.Select(t =>
        {
            var build = (Build.RouteBuild) t.Build;
            build.Rank = t.OldRank;

            return TimeSpan.FromSeconds(Voyage.CalculateDuration(t.Route.Select(f => Sheets.ExplorationSheet.GetRow(f)!).ToArray(), build.GetSubmarineBuild.Speed));
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
                              .Where(r => (hasAllowed && AllowedSectors.Contains(r)) || IgnoreUnlocks || unlocked.Contains(r.RowId))
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
                        lastMap = (int) Sheets.ExplorationSheet.GetRow(best.Route.First()).Map.RowId - 2;

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

            (ProgressRank, leftover) = Sectors.CalculateRankUp(ProgressRank, leftover + bestJourney!.RouteExp);

            bestJourney = bestJourney with { RankReached = ProgressRank, Leftover = leftover };
            outTree.Add(count++, bestJourney);

            if (ProgressRank == 0 || CancelSource.IsCancellationRequested)
                break;
        }

        if (CancelSource.IsCancellationRequested)
            return null;

        return outTree;
    }

    private Journey GetJourney(Build.RouteBuild routeBuild, int possibleMap, uint[] unlocked)
    {
        routeBuild.Map = possibleMap;

        var allowedSectors = AllowedSectors.Select(s => s.RowId).ToArray();
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
