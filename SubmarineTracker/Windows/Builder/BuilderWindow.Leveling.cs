using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using Newtonsoft.Json;
using SubmarineTracker.Data;
using static SubmarineTracker.Data.SectorBreakpoints;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    public static readonly Dictionary<int, List<Build.RouteBuild>> FinishedLevelingBuilds = new();

    private int TargetRank = 85;
    private int CurrentLowestTime = 9999;

    private int SwapAfter = 1;
    private bool IgnoreBuild;

    private int PossibleBuilds;
    private bool Processing;
    private int Progress;
    private int ProgressRank;
    private DurationCache CachedRouteList = new();
    private readonly Dictionary<string, List<string>> UnusedCache = new();

    private CancellationTokenSource CancelSource = new();

    private string DurationName = string.Empty;
    private bool MaximizeDuration;

    private DateTime StartTime;
    private DateTime ProgressStartTime;

    private bool LevelingTab()
    {
        if (ImGui.BeginTabItem("Leveling"))
        {
            ImGuiHelpers.ScaledDummy(10.0f);

            ImGui.TextColored(ImGuiColors.HealerGreen, $"Build: {CurrentBuild}");
            ImGui.Checkbox("Ignore Build", ref IgnoreBuild);
            ImGui.TextColored(ImGuiColors.HealerGreen, $"Target: {TargetRank}");
            ImGui.SameLine();
            ImGui.SliderInt("##targetRank", ref TargetRank, 15, 110);
            ImGui.TextColored(ImGuiColors.HealerGreen, "Swap if optimal after");
            ImGui.SameLine();
            ImGui.SliderInt("##swapAfter", ref SwapAfter, 1, 10);
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.HealerGreen, "Voyages");
            if (Processing)
            {
                ImGui.TextColored(ImGuiColors.HealerGreen, $"At Rank: {ProgressRank}");
                ImGui.TextColored(ImGuiColors.HealerGreen, $"Progress for current Rank: {Progress} / {PossibleBuilds}");
                ImGui.TextColored(ImGuiColors.HealerGreen, $"Time Elapsed: {DateTime.Now - StartTime}");
                ImGui.TextColored(ImGuiColors.HealerGreen, $"Time Elapsed for current Rank: {DateTime.Now - ProgressStartTime}");
            }

            ImGuiHelpers.ScaledDummy(10.0f);
            if (ImGui.Button("Calculate for Build"))
            {
                CancelSource.Cancel();
                CancelSource = new CancellationTokenSource();
                StartTime = DateTime.Now;
                ProgressStartTime = DateTime.Now;
                Task.Run(DoThingsOffThread, CancelSource.Token);
            }
            if (ImGui.Button("Stop calculate for Build"))
            {
                CancelSource.Cancel();
            }

            ImGuiHelpers.ScaledDummy(10.0f);

            var width = ImGui.GetContentRegionAvail().X / 3;
            var length = ImGui.CalcTextSize("Duration Limit").X + 25.0f;

            ImGui.Checkbox("Ignore unlocks", ref IgnoreUnlocks);
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

            ImGui.Checkbox("Maximize Duration limit", ref MaximizeDuration);


            ImGui.EndTabItem();
            return true;
        }

        return false;
    }

    public void DoThingsOffThread()
    {
        FinishedLevelingBuilds.Clear();
        Processing = true;

        try
        {
            PluginLog.Debug("Loading cached leveling data.");

            var importPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "routeList.json");
            var jsonString = File.ReadAllText(importPath);
            CachedRouteList = JsonConvert.DeserializeObject<DurationCache>(jsonString) ?? new(); ;
        }
        catch (Exception e)
        {
            PluginLog.Error("Loading cached leveling data failed.");
            PluginLog.Error(e.Message);
        }

        // Add durations limit if they not exist
        DurationName = DateUtil.GetDurationLimitName(Configuration.DurationLimit) + (IgnoreBuild ? "" : " - " + CurrentBuild);

        CurrentLowestTime = 9999;
        PossibleBuilds = 0;
        Progress = 0;

        Progress += 1;
        var outTree = BuildRoute();

        var rank = 1;

        foreach (var (build, route) in outTree)
        {
            PluginLog.Information("=================");
            PluginLog.Information($"Used Build {build}");
            PluginLog.Information($"Rank: {rank}");
            foreach (var (i, (rankReached, leftover, routeExp, points)) in route.Voyages)
            {
                rank = rankReached;
                var startPoint = Voyage.FindVoyageStartPoint(points[0]);
                PluginLog.Information($"Voyage {i}: {Utils.MapToThreeLetter(ExplorationSheet.GetRow(startPoint)!.Map.Row)} {string.Join(" -> ", points.Select(p => Utils.NumToLetter(p - startPoint)))}");
                PluginLog.Information($"Exp gained {routeExp}");
                PluginLog.Information($"After-Rank {rank}");
                PluginLog.Information($"Leftover: {leftover}");
                PluginLog.Information($"-----------------");
            }
        }

        PluginLog.Information($"Time Elapsed: {DateTime.Now - StartTime}");
        
        var l = JsonConvert.SerializeObject(CachedRouteList, new JsonSerializerSettings { Formatting = Formatting.Indented, });

        var filePath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "routeList.json");
        PluginLog.Information($"Writing routeList json");
        PluginLog.Information(filePath);
        File.WriteAllText(filePath, l);
        
        Processing = false;
    }

    private Dictionary<Build.RouteBuild, RouteCache> BuildRoute()
    {
        var routeBuilds = BuildParts();

        var outTree = CachedRouteList.Caches.TryGetValue(DurationName, out var cacheOut) ? cacheOut.Builds.ToDictionary(t => (Build.RouteBuild)t.Key, t => t.Value) : new Dictionary<Build.RouteBuild, RouteCache>();
        var count = 0;
        if (Submarines.KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
        {
            var mapBreaks = ExplorationSheet
                            .Where(f => ExplorationSheet.Where(t => t.StartingPoint).Select(t => t.RowId + 1).Contains(f.RowId))
                            .Where(r => IgnoreUnlocks || fcSub.UnlockedSectors[r.RowId])
                            .ToDictionary(t => t.RankReq, t => (int)t.Map.Row);

            ProgressRank = 1;

            var lastBuildRouteRank = 0;

            while (ProgressRank < TargetRank)
            {
                var (curBuild, journeys) = outTree.LastOrDefault();
                var leftover = journeys?.Voyages.LastOrDefault().Value.Leftover ?? 0;

                var bestJourney = journeys?.Voyages.LastOrDefault().Value;
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

                    PossibleBuilds = builds.Length;
                    Progress = 0;

                    bestJourney ??= new Journey(ProgressRank, 0, 0, new uint[] { 0 });

                    foreach (var build in builds)
                    {
                        if (CancelSource.IsCancellationRequested)
                            break;
                        var routeBuild = build;
                        routeBuild.Map = mapBreaks.Last(t => t.Key <= ProgressRank).Value - 1;
                        var path = FindBestPath(routeBuild);

                        var exp = 0u;
                        foreach (var sector in path)
                        {
                            var sheetSector = ExplorationSheet.GetRow(sector)!;
                            var bonus = CalculateBonusExp(PredictBonusExp(sector, routeBuild.GetSubmarineBuild).Item1, sheetSector.ExpReward);
                            exp += bonus;
                        }

                        if (bestJourney.RouteExp < exp)
                        {
                            if ((!curBuild.SameBuildWithoutRank(routeBuild) && (journeys?.Voyages.Count ?? 0) >= SwapAfter && (!curBuild.SameBuildWithoutRank(CurrentBuild) || IgnoreBuild)) || (routeBuild.SameBuildWithoutRank(CurrentBuild) && !IgnoreBuild))
                            {
                                curBuild = routeBuild;
                            }

                            bestJourney = new Journey(ProgressRank, exp, exp, path);
                        }

                        Progress++;
                    }
                }

                if (RankSheet[ProgressRank - 1].ExpToNext <= leftover + bestJourney!.RouteExp)
                {
                    leftover = bestJourney.RouteExp + leftover - RankSheet[ProgressRank - 1].ExpToNext;
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

                if (outTree.TryGetValue(curBuild, out var cache))
                {
                    if (!cache.Voyages.ContainsKey(count))
                    {
                        cache.Voyages.Add(count++, bestJourney);
                    }
                    else
                    {
                        cache.Voyages[count++] = bestJourney;
                    }
                }
                else
                {
                    outTree.Add(curBuild, new RouteCache(new Dictionary<int, Journey> { { count++, bestJourney } }));
                }

                if (ProgressRank == 0 || CancelSource.IsCancellationRequested)
                    break;
            }
        }

        var json = outTree.GroupBy(t => t.Key.ToString())
                          .ToDictionary(t => t.Key,
                                        t => new RouteCache(t.SelectMany(f => f.Value.Voyages)
                                                             .ToDictionary(f => f.Key, f => f.Value)));

        if (CachedRouteList.Caches.ContainsKey(DurationName))
        {
            CachedRouteList.Caches[DurationName] = new BuildCache(json);
        }
        else
        {
            CachedRouteList.Caches.Add(DurationName, new BuildCache(json));
        }

        return outTree;
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

    public record Journey(int RankReached, uint Leftover, uint RouteExp, uint[] Route);
    public record RouteCache(Dictionary<int, Journey> Voyages);
    public record BuildCache(Dictionary<string, RouteCache> Builds);
    public class DurationCache
    {
        public Dictionary<string, BuildCache> Caches = new();
    }
}
