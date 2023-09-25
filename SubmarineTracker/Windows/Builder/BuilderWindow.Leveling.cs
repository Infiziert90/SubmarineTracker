using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using Newtonsoft.Json;
using SubmarineTracker.Data;
using static SubmarineTracker.Data.Sectors;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    private const string MaximizeHelp = "This will prioritize maximum Exp over best Exp\n" +
                                        "Example\n" +
                                        "-- 48H Limit --\n" +
                                        "Route 1: 38:30h 500 Exp/Min = 1,15mil\n" +
                                        "Route 2: 47:58h 450 Exp/Min = 1,29mil\n" +
                                        "Route 1 is preferred for best Exp/Min\n" +
                                        "Route 2 is preferred for maximizing Exp/Limit in under 48h";

    private int TargetRank = 85;

    private int SwapAfter = 1;
    private bool IgnoreBuild;

    private int PossibleBuilds;
    private bool Processing;
    private int Progress;
    private int ProgressRank;
    private DurationCache CachedRouteList = new();

    private CancellationTokenSource CancelSource = new();

    private string DurationName = string.Empty;
    private bool IgnoreShark;
    private bool IgnoreUnmodded;

    private DateTime StartTime;
    private DateTime ProgressStartTime;

    private Thread Thread = null!;

    private Dictionary<int, Journey> LastCalc = new();
    private (string Limit, bool IgnoreBuild, bool IgnoreUnlocks, bool MaximizeDurationLimit) LastOptions = ("", false, false, false);

    private bool AllowedChanged;
    private List<SubmarineExplorationPretty> AllowedSectors = new();
    private uint[] Unlocked = Array.Empty<uint>();

    private static string MiscFolder = null!;
    private static void InitializeLeveling() => MiscFolder = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, "Misc");

    private bool LevelingTab()
    {
        if (ImGui.BeginTabItem("Leveling"))
        {
            var avail = ImGui.GetContentRegionAvail().X;
            var width = avail / 2;

            ImGui.TextColored(ImGuiColors.HealerGreen, $"Build: {(!IgnoreBuild ? $"{CurrentBuild} ({CurrentBuild.Rank})" : "All")}");
            ImGui.SetNextItemWidth(width);
            ImGui.SliderInt("##targetRank", ref TargetRank, 15, (int)RankSheet.Last().RowId, "Target Rank %d");
            ImGuiComponents.HelpMarker("The rank this leveling calculation should try to reach, but can overshot");
            ImGui.SetNextItemWidth(width);
            ImGui.SliderInt("##swapAfter", ref SwapAfter, 1, 10, "Swap After %d");
            ImGuiComponents.HelpMarker("Swaps parts after X voyages if optimal");
            if (Processing)
            {
                ImGui.TextColored(ImGuiColors.DalamudViolet, "Progress:");
                ImGui.Indent(10.0f);
                if (IgnoreBuild)
                    Helper.WrappedError("Warning this will take a long time and you'll experience game slowdown");
                ImGui.TextColored(ImGuiColors.HealerGreen, $"At Rank: {ProgressRank}");
                ImGui.TextColored(ImGuiColors.HealerGreen, $"Progress for current: {Progress} / {PossibleBuilds}");
                ImGui.TextColored(ImGuiColors.HealerGreen, $"Time elapsed: {DateTime.Now - StartTime}");
                ImGui.TextColored(ImGuiColors.HealerGreen, $"Time elapsed current calculation: {DateTime.Now - ProgressStartTime}");
                ImGui.Unindent(10.0f);
            }

            ImGuiHelpers.ScaledDummy(10.0f);
            if (ImGui.Button($"Calculate for {(!IgnoreBuild ? "Build" : "All")}"))
            {
                // Reset here to prevent an empty calculation from happening
                MustInclude.Clear();

                CancelSource.Cancel();
                Thread?.Join();
                CancelSource = new CancellationTokenSource();
                StartTime = DateTime.Now;
                ProgressStartTime = DateTime.Now;
                Thread = new Thread(DoThingsOffThread);
                Thread.SetApartmentState(ApartmentState.MTA);
                Thread.Start();
            }

            ImGui.SameLine();

            if (ImGui.Button($"Stop calculate for {(!IgnoreBuild ? "Build" : "All")}"))
            {
                CancelSource.Cancel();
                Thread?.Join();
            }

            ImGuiHelpers.ScaledDummy(10.0f);

            width = avail / 3;
            var length = ImGui.CalcTextSize("Duration Limit").X + 25.0f;

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Options:");
            ImGui.Indent(10.0f);
            ImGui.Checkbox("Ignore Build", ref IgnoreBuild);
            ImGuiComponents.HelpMarker("This will calculate every single possible build\n" +
                                       "Warning: This will take a long time and you'll experience game slowdown");
            ImGui.Checkbox("Ignore unlocks", ref IgnoreUnlocks);
            ImGui.Checkbox("Use Avg EXP Bonus", ref AvgBonus);
            ImGuiComponents.HelpMarker("This calculation normally takes only guaranteed retrieval bonus into account.\n" +
                                       "With this option it will take the avg of possible bonus");
            if (Configuration.DurationLimit != DurationLimit.None)
            {
                ImGui.Checkbox("Maximize duration limit", ref Configuration.MaximizeDuration);
                ImGuiComponents.HelpMarker(MaximizeHelp);
            }

            ImGui.Unindent(10.0f);

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudViolet, "Duration Limit");
            ImGui.Indent(10.0f);
            ImGui.SetNextItemWidth(width);
            if (ImGui.BeginCombo($"##durationLimitCombo", Configuration.DurationLimit.GetName()))
            {
                foreach (var durationLimit in (DurationLimit[])Enum.GetValues(typeof(DurationLimit)))
                {
                    if (ImGui.Selectable(durationLimit.GetName()))
                    {
                        Configuration.DurationLimit = durationLimit;
                        Configuration.Save();

                        OptionsChanged = true;
                    }
                }

                ImGui.EndCombo();
            }

            if (Configuration.DurationLimit == DurationLimit.Custom)
            {
                ImGui.SetNextItemWidth(width / 5f);
                if (ImGui.InputInt("##CustomHourInput", ref Configuration.CustomHour, 0))
                {
                    Configuration.CustomHour = Math.Clamp(Configuration.CustomHour, 1, 123);
                    Configuration.Save();
                }
                ImGui.SameLine();
                ImGui.TextUnformatted(":");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(width / 5f);
                if (ImGui.InputInt("##CustomMinInput", ref Configuration.CustomMinute, 0))
                {
                    Configuration.CustomMinute = Math.Clamp(Configuration.CustomMinute, 0, 59);
                    Configuration.Save();
                }
                ImGui.SameLine();
                ImGui.TextUnformatted("hours & minutes");
            }
            ImGui.Unindent(10.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "--Experimental--");
            ImGui.Indent(10.0f);
            ImGui.Checkbox($"Ignore Shark Parts", ref IgnoreShark);
            ImGuiComponents.HelpMarker("Leveling expects that a shark part exists, so it takes that as prio over worse parts" +
                                       "\nThis option disables the behaviour" +
                                       "\nNOTE: Your Submarine must be Rank of the highest Rank Part, or this will endlessly loop." +
                                       "\ne.g SSUW must be Rank higher or equal to 25" +
                                       "\nImportant: This can lead to errors");
            ImGui.Checkbox($"Ignore Unmodded Parts", ref IgnoreUnmodded);
            ImGuiComponents.HelpMarker("Leveling expects that an unmodded part exists, so it takes that as prio over modded" +
                                       "\nThis option disables the behaviour" +
                                       "\nImportant: This can lead to errors");
            ImGui.Unindent(10.0f);
            ImGui.TextColored(ImGuiColors.DalamudViolet, $"Allowed Sectors: {AllowedSectors.Count}");

            if (AllowedChanged)
            {
                AllowedChanged = false;
                ExcelSheetSelector.FilteredSearchSheet = null!;
            }

            var listHeight = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(30.0f * ImGuiHelpers.GlobalScale, listHeight));
            ImGui.PopFont();
            ExcelSheetSelector.ExcelSheetPopupOptions<SubmarineExplorationPretty> explorationPopupOptions = new()
            {
                FormatRow = e => $"{NumToLetter(e.RowId - Voyage.FindVoyageStart(e.RowId))}. {UpperCaseStr(e.Destination)} (Rank {e.RankReq})",
                FilteredSheet = ExplorationSheet.Where(r => r.RankReq > 0).Where(r => !r.StartingPoint).Where(r => !AllowedSectors.Contains(r))
            };

            if (ExcelSheetSelector.ExcelSheetPopup("LevelingMustIncludeAddPopup", out var row, explorationPopupOptions))
            {
                var point = ExplorationSheet.GetRow(row)!;
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

            if (ImGui.Button("Clear"))
            {
                AllowedChanged = true;
                AllowedSectors.Clear();
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            if (LastCalc.Any())
            {
                var lastIdx = LastCalc.Last().Key;
                var modifier = new Box.Modifier { FPadding = new Vector4(7 * ImGuiHelpers.GlobalScale), FBorderColor = ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudGrey)) };
                ImGui.TextColored(ImGuiColors.DalamudViolet, "Last Calculation:");
                Box.SimpleBox(modifier, () =>
                {
                    ImGui.TextUnformatted($"Start Rank: {CurrentBuild.Rank} ({CurrentBuild})");
                    ImGui.TextUnformatted($"Final Rank: {LastCalc[lastIdx].RankReached} ({LastCalc[lastIdx].Build})");
                    ImGui.TextUnformatted($"Voyages: {lastIdx} ({GetStringFromTimespan(GetTimesFromJourneys(LastCalc.Values))})");
                    ImGui.TextUnformatted($"EXP total: {LastCalc.Values.Sum(x => x.RouteExp):N0}");
                    ImGui.TextUnformatted($"Leftover EXP: {LastCalc[lastIdx].Leftover:N0}");
                });
                ImGui.SameLine();
                ImGuiHelpers.ScaledDummy(20, 0);
                ImGui.SameLine();
                Box.SimpleBox(modifier, () =>
                {
                    ImGui.TextUnformatted($"Limit: {LastOptions.Limit}");
                    ImGui.TextUnformatted($"Avg Bonus: {AvgBonus}");
                    ImGui.TextUnformatted($"Ignore Build: {LastOptions.IgnoreBuild}");
                    ImGui.TextUnformatted($"Ignore Unlocks: {LastOptions.IgnoreUnlocks}");
                    ImGui.TextUnformatted($"Maximize Limit: {LastOptions.MaximizeDurationLimit}");
                });
                ImGuiHelpers.ScaledDummy(0, 20);
                ImGui.Indent(10);
                ImGui.TextColored(ImGuiColors.DalamudViolet, "List:");
                ImGui.Unindent(10);
                BoxList.RenderList(LastCalc, modifier, 1f, pair =>
                {
                    var (i, (_, rankReached, leftover, routeExp, points, build)) = pair;
                    var startPoint = Voyage.FindVoyageStart(points[0]);
                    ImGui.TextColored(ImGuiColors.HealerGreen, $"Build: {build}");
                    ImGui.TextColored(ImGuiColors.HealerGreen, $"Voyage {i}: {MapToThreeLetter(ExplorationSheet.GetRow(startPoint)!.Map.Row)} {string.Join(" -> ", points.Select(p => NumToLetter(p - startPoint)))}");
                    ImGui.TextColored(ImGuiColors.HealerGreen, $"Exp Gained: {routeExp:N0}");
                    ImGui.TextColored(ImGuiColors.HealerGreen, $"Rank Reached: {rankReached} - {GetRemaindExp(rankReached, leftover):P}%");
                });
            }

            ImGui.EndTabItem();
            return true;
        }

        return false;
    }

    public string GetStringFromTimespan(TimeSpan span) => $"{span.Days}d {span.Hours}h {span.Minutes}m {span.Seconds}s";

    public float GetRemaindExp(int rank, uint exp)
    {
        var expToNext = RankSheet[rank - 1].ExpToNext;
        return rank == RankSheet.Count ? 0 : exp / (float)expToNext;
    }

    public TimeSpan GetTimesFromJourneys(IEnumerable<Journey> journeys) =>
        journeys.Select(t =>
        {
            var startPoint = Voyage.FindVoyageStart(t.Route[0]);
            var build = (Build.RouteBuild) t.Build;
            build.Rank = t.OldRank;

            return TimeSpan.FromSeconds(Voyage.CalculateDuration(t.Route.Append(startPoint).Select(f => ExplorationSheet.GetRow(f)!), build));
        }).Aggregate(TimeSpan.Zero, (current, timeSpan) => current + timeSpan);

    public void DoThingsOffThread()
    {
        Processing = true;

        Directory.CreateDirectory(MiscFolder);
        var filePath = Path.Combine(MiscFolder, "routeList.json");
        try
        {
            PluginLog.Debug("Loading cached leveling data.");
            CachedRouteList = JsonConvert.DeserializeObject<DurationCache>(File.ReadAllText(filePath)) ?? new DurationCache();
        }
        catch (FileNotFoundException)
        {
            PluginLog.Warning("Cache file not found.");
        }
        catch (Exception e)
        {
            PluginLog.Error("Loading cached leveling data failed.");
            PluginLog.Error(e.Message);
        }

        // Add durations limit if they not exist
        DurationName = Configuration.DurationLimit.GetName() + (IgnoreBuild ? "" : " - " + CurrentBuild);

        PossibleBuilds = 0;
        Progress = 0;

        Progress += 1;
        var outTree = BuildRoute();

        if (CancelSource.IsCancellationRequested || outTree == null)
        {
            Processing = false;
            return;
        }

        LastCalc = outTree;

        LastOptions = (Configuration.DurationLimit.GetName(), IgnoreBuild, IgnoreUnlocks, Configuration.MaximizeDuration);
        var l = JsonConvert.SerializeObject(CachedRouteList, new JsonSerializerSettings { Formatting = Formatting.Indented, });

        PluginLog.Debug($"Writing routeList json");
        PluginLog.Debug(filePath);
        File.WriteAllText(filePath, l);

        Processing = false;
    }

    private Dictionary<int, Journey>? BuildRoute()
    {
        var routeBuilds = BuildParts();

        var outTree = new Dictionary<int, Journey>();
        var count = 1;
        var lastBuild = (new Build.RouteBuild(), 0);
        if (Submarines.KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
        {
            Unlocked = fcSub.UnlockedSectors.Where(pair => pair.Value).Select(pair => pair.Key).ToArray();
            var hasAllowed = AllowedSectors.Any();
            var mapBreaks = ExplorationSheet
                        .Where(f => ExplorationSheet.Where(t => t.StartingPoint).Select(t => t.RowId + 1).Contains(f.RowId))
                        .Where(r => (hasAllowed && AllowedSectors.Contains(r)) || IgnoreUnlocks || Unlocked.Contains(r.RowId))
                        .ToDictionary(t => t.RankReq, t => (int)t.Map.Row);

            ProgressRank = CurrentBuild.Rank;

            var lastBuildRouteRank = 0;
            var lastMap = 0;

            while (ProgressRank < TargetRank)
            {
                var (_, bestJourney) = outTree.LastOrDefault();
                var leftover = bestJourney?.Leftover ?? 0;
                var curBuild = lastBuild.Item1;


                if (lastBuildRouteRank != ProgressRank)
                {
                    lastBuildRouteRank = ProgressRank;
                    var builds = routeBuilds.Where(t =>
                    {
                        var build = t.GetSubmarineBuild;
                        build.UpdateRank(ProgressRank);
                        return build.HighestRankPart() <= ProgressRank && build is { Speed: >= 20, Range: >= 20 };
                    }).Select(t => new Build.RouteBuild(ProgressRank, t)).ToArray();

                    // if (builds.Contains(CurrentBuild) && !IgnoreBuild)
                    // {
                    //     builds = builds.Where(t => t == CurrentBuild).ToArray();
                    // }

                    var possibleMaps = mapBreaks.Where(t => t.Key <= ProgressRank).Select(t => t.Value - 1).Where(t => t >= lastMap).ToArray();

                    PossibleBuilds = builds.Length * possibleMaps.Length;
                    Progress = 0;

                    bestJourney ??= new Journey(curBuild.Rank, ProgressRank, 0, 0, new uint[] { 0 }, curBuild.ToString());

                    foreach (var build in builds)
                    {
                        if (CancelSource.IsCancellationRequested)
                            break;

                        var routeBuild = build;
                        var taskJourneys = new List<Task<Journey>>();

                        foreach (var possibleMap in possibleMaps)
                            taskJourneys.Add(Task.Run(() => GetJourney(routeBuild, possibleMap)));

                        // ReSharper disable once CoVariantArrayConversion
                        try
                        {
                            Task.WaitAll(taskJourneys.ToArray(), CancelSource.Token);
                        }
                        catch
                        {
                            CancelSource.Cancel();
                            PluginLog.Error("Failed operation when waiting for tasks");
                            break;
                        }

                        if (CancelSource.IsCancellationRequested)
                            break;

                        if (!taskJourneys.Any())
                        {
                            PluginLog.Error($"No journeys returned, cancelling current build!");
                            return null;
                        }

                        var best = taskJourneys.Select(t => t.Result).OrderBy(t => t.RouteExp).Last();
                        var (_, _, _, exp, path, currentBuild) = best;

                        // we can still continue if this would be false, we also want to check if allowed list is set
                        if (path.Any() && !hasAllowed)
                            lastMap = (int)ExplorationSheet.GetRow(path.First())!.Map.Row - 2;

                        if (bestJourney.RouteExp < exp || (bestJourney.RouteExp == exp && currentBuild == lastBuild.Item1.ToString()))
                        {
                            if ((!curBuild.SameBuildWithoutRank(routeBuild) && lastBuild.Item2 >= SwapAfter) || (routeBuild.SameBuildWithoutRank(CurrentBuild) && !IgnoreBuild) || outTree.Count == 0)
                            {
                                curBuild = routeBuild;
                                bestJourney = new Journey(routeBuild.Rank, ProgressRank, exp, exp, path, routeBuild.ToString());
                            }
                            else if (curBuild.SameBuildWithoutRank(routeBuild))
                            {
                                bestJourney = new Journey(routeBuild.Rank, ProgressRank, exp, exp, path, routeBuild.ToString());
                            }
                        }
                    }
                }

                if (!lastBuild.Item1.SameBuildWithoutRank(curBuild))
                    lastBuild.Item2 = 0;
                lastBuild.Item1 = curBuild;
                lastBuild.Item2++;

                if (CancelSource.IsCancellationRequested)
                    break;

                var newLeftover = leftover + bestJourney!.RouteExp;

                if (RankSheet[ProgressRank - 1].ExpToNext <= newLeftover)
                {
                    leftover = newLeftover - RankSheet[ProgressRank - 1].ExpToNext;
                    ProgressRank++;
                    if (ProgressRank > RankSheet.Count)
                    {
                        ProgressRank--;
                        leftover = 0;
                    }

                    while (RankSheet[ProgressRank - 1].ExpToNext <= leftover)
                    {
                        leftover -= RankSheet[ProgressRank - 1].ExpToNext;
                        ProgressRank++;
                        if (ProgressRank > RankSheet.Count)
                        {
                            ProgressRank--;
                            leftover = 0;
                            break;
                        }
                    }

                    ProgressStartTime = DateTime.Now;
                }
                else
                {
                    leftover += bestJourney.RouteExp;
                }

                bestJourney = bestJourney with { RankReached = ProgressRank, Leftover = leftover };

                outTree.Add(count++, bestJourney);

                if (ProgressRank == 0 || CancelSource.IsCancellationRequested)
                    break;
            }
        }

        if (CancelSource.IsCancellationRequested)
            return null!;

        if (CachedRouteList.Caches.ContainsKey(DurationName))
        {
            CachedRouteList.Caches[DurationName] = new RouteCache(outTree);
        }
        else
        {
            CachedRouteList.Caches.Add(DurationName, new RouteCache(outTree));
        }

        return outTree;
    }

    private Journey GetJourney(Build.RouteBuild routeBuild, int possibleMap)
    {
        routeBuild.Map = possibleMap;


        var allowedSectors = AllowedSectors.Select(s => s.RowId).ToArray();
        var path = Voyage.FindBestPath(routeBuild, Unlocked, Array.Empty<uint>(), allowedSectors, AvgBonus);
        var exp = CalculateExpForSectors(path.Select(ExplorationSheet.GetRow).ToArray()!, routeBuild.GetSubmarineBuild, AvgBonus);

        Progress++;
        return new Journey(routeBuild.Rank, ProgressRank, exp, exp, path, routeBuild.ToString());
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
    public record RouteCache(Dictionary<int, Journey> Voyages);
    public class DurationCache
    {
        public Dictionary<string, RouteCache> Caches = new();
    }
}
