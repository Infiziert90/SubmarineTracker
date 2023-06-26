using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using Newtonsoft.Json;
using SubmarineTracker.Data;
using static SubmarineTracker.Data.Sectors;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
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
    private bool MaximizeDuration;

    private DateTime StartTime;
    private DateTime ProgressStartTime;

    private Thread Thread = null!;

    private Dictionary<int, Journey> LastCalc = new();
    private (string Limit, bool IgnoreBuild, bool IgnoreUnlocks, bool MaximizeDurationLimit) LastOptions = ("", false, false, false);

    private bool LevelingTab()
    {
        if (ImGui.BeginTabItem("Leveling"))
        {
            var avail = ImGui.GetContentRegionAvail().X;
            var width = avail / 2;

            ImGui.TextColored(ImGuiColors.HealerGreen, $"Build: {(!IgnoreBuild ? CurrentBuild : "All")}");
            ImGui.TextColored(ImGuiColors.HealerGreen, $"Target Rank: {TargetRank}");
            ImGui.SetNextItemWidth(width);
            ImGui.SliderInt("##targetRank", ref TargetRank, 15, (int)RankSheet.Last().RowId);
            ImGui.TextColored(ImGuiColors.HealerGreen, $"Swap if optimal after {SwapAfter} voyages");
            ImGui.SetNextItemWidth(width);
            ImGui.SliderInt("##swapAfter", ref SwapAfter, 1, 10);
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
            if (Configuration.DurationLimit != DurationLimit.None)
            {
                ImGui.Checkbox("Maximize duration limit", ref MaximizeDuration);
                ImGuiComponents.HelpMarker("This will prioritize maximum Exp over best Exp\n" +
                                           "e.g 48H Limit -\n" +
                                           "Route 1: 38:30h 500 Exp/Min = 1,15mil\n" +
                                           "Route 2: 47:58h 450 Exp/Min = 1,29mil\n" +
                                           "Route 1 is preferred for best Exp/Min\n" +
                                           "Route 2 is preferred for maximizing Exp/Limit in under 48h");

            }

            ImGui.Unindent(10.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Duration Limit");
            ImGui.SameLine(length);
            ImGui.SetNextItemWidth(width);
            if (ImGui.BeginCombo($"##durationLimitCombo", DateUtil.GetDurationLimitName(Configuration.DurationLimit)))
            {
                foreach (var durationLimit in (DurationLimit[])Enum.GetValues(typeof(DurationLimit)))
                {
                    if (ImGui.Selectable(DateUtil.GetDurationLimitName(durationLimit)))
                    {
                        Configuration.DurationLimit = durationLimit;
                        Configuration.Save();

                        OptionsChanged = true;
                    }
                }

                ImGui.EndCombo();
            }

            if (LastCalc.Any())
            {
                var lastIdx = LastCalc.Last().Key;
                var modifier = new Box.Modifier { FPadding = new Vector4(7 * ImGuiHelpers.GlobalScale), FBorderColor = ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudGrey)) };
                ImGui.TextColored(ImGuiColors.DalamudViolet, "Last Calculation:");
                Box.SimpleBox(modifier, () =>
                {
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
                    var (i, (rankReached, leftover, routeExp, points, build)) = pair;
                    var startPoint = Voyage.FindVoyageStartPoint(points[0]);
                    ImGui.TextColored(ImGuiColors.HealerGreen, $"Build: {build}");
                    ImGui.TextColored(ImGuiColors.HealerGreen, $"Voyage {i}: {Utils.MapToThreeLetter(ExplorationSheet.GetRow(startPoint)!.Map.Row)} {string.Join(" -> ", points.Select(p => Utils.NumToLetter(p - startPoint)))}");
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
            var startPoint = Voyage.FindVoyageStartPoint(t.Route[0]);
            return TimeSpan.FromSeconds(Voyage.CalculateDuration(t.Route.Append(startPoint).Select(f => ExplorationSheet.GetRow(f)!).ToArray(), (Build.RouteBuild)t.Build));
        }).Aggregate(TimeSpan.Zero, (current, timeSpan) => current + timeSpan);

    public void DoThingsOffThread()
    {
        Processing = true;

        var filePath = Path.Combine(Plugin.PluginInterface.GetPluginConfigDirectory(), "routeList.json");

        try
        {
            PluginLog.Debug("Loading cached leveling data.");

            var jsonString = File.ReadAllText(filePath);
            CachedRouteList = JsonConvert.DeserializeObject<DurationCache>(jsonString) ?? new();
        }
        catch (Exception e)
        {
            PluginLog.Error("Loading cached leveling data failed.");
            PluginLog.Error(e.Message);
        }

        // Add durations limit if they not exist
        DurationName = DateUtil.GetDurationLimitName(Configuration.DurationLimit) + (IgnoreBuild ? "" : " - " + CurrentBuild);

        PossibleBuilds = 0;
        Progress = 0;

        Progress += 1;
        var outTree = BuildRoute();

        if (CancelSource.IsCancellationRequested)
        {
            Processing = false;
            return;
        }

        LastCalc = outTree;

        LastOptions = (DateUtil.GetDurationLimitName(Configuration.DurationLimit), IgnoreBuild, IgnoreUnlocks, MaximizeDuration);
        var l = JsonConvert.SerializeObject(CachedRouteList, new JsonSerializerSettings { Formatting = Formatting.Indented, });

        PluginLog.Debug($"Writing routeList json");
        PluginLog.Debug(filePath);
        File.WriteAllText(filePath, l);

        Processing = false;
    }

    private Dictionary<int, Journey> BuildRoute()
    {
        var routeBuilds = BuildParts();

        var outTree = new Dictionary<int, Journey>();
        var count = 1;
        var lastBuild = (new Build.RouteBuild(), 0);
        if (Submarines.KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
        {
            var mapBreaks = ExplorationSheet
                            .Where(f => ExplorationSheet.Where(t => t.StartingPoint).Select(t => t.RowId + 1).Contains(f.RowId))
                            .Where(r => IgnoreUnlocks || fcSub.UnlockedSectors[r.RowId])
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
                        return build.HighestRankPart() <= ProgressRank && build.Speed >= 20 && build.Range >= 20;
                    }).Select(t => new Build.RouteBuild(ProgressRank, t)).ToArray();

                    if (builds.Contains(CurrentBuild) && !IgnoreBuild)
                    {
                        builds = builds.Where(t => t == CurrentBuild).ToArray();
                    }

                    var possibleMaps = mapBreaks.Where(t => t.Key <= ProgressRank).Select(t => t.Value - 1).Where(t => t >= lastMap).ToArray();

                    PossibleBuilds = builds.Length * possibleMaps.Length;
                    Progress = 0;

                    bestJourney ??= new Journey(ProgressRank, 0, 0, new uint[] { 0 }, curBuild.ToString());

                    foreach (var build in builds)
                    {
                        if (CancelSource.IsCancellationRequested)
                            break;
                        var routeBuild = build;
                        var taskJourneys = new List<Task<Journey>>();

                        foreach (var possibleMap in possibleMaps)
                        {
                            taskJourneys.Add(Task.Run(() => GetJourney(routeBuild, possibleMap)));
                        }

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

                        var best = taskJourneys.Select(t => t.Result).OrderBy(t => t.RouteExp).Last();
                        var (_, _, exp, path, _) = best;

                        lastMap = (int)ExplorationSheet.GetRow(path.First())!.Map.Row - 2;

                        if (bestJourney.RouteExp < exp)
                        {
                            if ((!curBuild.SameBuildWithoutRank(routeBuild) && lastBuild.Item2 >= SwapAfter) || (routeBuild.SameBuildWithoutRank(CurrentBuild) && !IgnoreBuild) || outTree.Count == 0)
                            {
                                curBuild = routeBuild;
                                bestJourney = new Journey(ProgressRank, exp, exp, path, routeBuild.ToString());
                            }
                            else if (curBuild.SameBuildWithoutRank(routeBuild))
                            {
                                bestJourney = new Journey(ProgressRank, exp, exp, path, routeBuild.ToString());
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
        var path = FindBestPath(routeBuild);

        var exp = 0u;
        foreach (var sector in path)
        {
            var sheetSector = ExplorationSheet.GetRow(sector)!;
            var bonus = CalculateBonusExp(PredictBonusExp(sector, routeBuild.GetSubmarineBuild).Guaranteed, sheetSector.ExpReward);
            exp += bonus;
        }

        Progress++;

        return new Journey(ProgressRank, exp, exp, path, "");
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
                        if (build.GetSubmarineBuild.HighestRankPart() < TargetRank && (build.IsSubComponent(CurrentBuild) || IgnoreBuild))
                        {
                            routeBuilds.Add(build);
                        }
                    }
                }
            }
        }

        return routeBuilds;
    }

    public record Journey(int RankReached, uint Leftover, uint RouteExp, uint[] Route, string Build);
    public record RouteCache(Dictionary<int, Journey> Voyages);
    public class DurationCache
    {
        public Dictionary<string, RouteCache> Caches = new();
    }
}
