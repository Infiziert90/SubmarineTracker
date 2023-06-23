using System.IO;
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

    private int PossibleBuilds = 0;
    private int Progress = 0;
    private DurationCache CachedRouteList = new();
    private readonly Dictionary<string, List<string>> UnusedCache = new();

    private string DurationName = string.Empty;

    private bool LevelingTab()
    {
        if (ImGui.BeginTabItem("Leveling"))
        {
            ImGuiHelpers.ScaledDummy(10.0f);

            ImGui.TextColored(ImGuiColors.HealerGreen, $"Build: {CurrentBuild.FullIdentifier()}");
            ImGui.TextColored(ImGuiColors.HealerGreen, $"Target: {TargetRank}");
            ImGui.SameLine();
            ImGui.SliderInt("##targetRank", ref TargetRank, 15, 85);
            if (Progress > 0)
                ImGui.TextColored(ImGuiColors.HealerGreen, $"Progress: {Progress} / {PossibleBuilds}");

            ImGuiHelpers.ScaledDummy(10.0f);
            if (ImGui.Button("Calculate for Build"))
            {
                Task.Run(DoThingsOffThread);
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

            ImGui.EndTabItem();
            return true;
        }

        return false;
    }

    public void DoThingsOffThread()
    {
        FinishedLevelingBuilds.Clear();
        // ConcurrentRouteList.Clear();
        IgnoreUnlocks = false;

        try
        {
            PluginLog.Debug("Loading cached leveling data.");

            var importPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "routeList.json");
            var jsonString = File.ReadAllText(importPath);
            CachedRouteList = JsonConvert.DeserializeObject<DurationCache>(jsonString) ?? new();;
        }
        catch (Exception e)
        {
            PluginLog.Error("Loading cached leveling data failed.");
            PluginLog.Error(e.Message);
        }

        // Add durations limit if they not exist
        DurationName = DateUtil.GetDurationLimitName(Configuration.DurationLimit);
        CachedRouteList.Caches.TryAdd(DurationName, new BuildCache(new Dictionary<string, RouteCache>()));

        CurrentLowestTime = 9999;
        PossibleBuilds = 0;
        Progress = 0;

        Progress += 1;
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
                         if (build.GetSubmarineBuild.HighestRankPart() < TargetRank)
                         {
                             PossibleBuilds += 1;
                             routeBuilds.Add(build);
                         }
                     }
                 }
             }
         }

        var lastIdentifier = string.Empty;
        //var routeBuilds = GenerateBuildTree(new Build.RouteBuild(Items.SharkClassHull, Items.SharkClassStern, Items.UnkiuClassBow, Items.WhaleClassBridge));
        // var routeBuilds = GenerateBuildTree(CurrentBuild);
        // var lastIdentifier = routeBuilds.Last().FullIdentifier();
        // PossibleBuilds = routeBuilds.Count;
        //
        // PluginLog.Information("Testing Builds:");
        // foreach (var build in routeBuilds)
        // {
        //     PluginLog.Information(build.FullIdentifier());
        // }
        // PluginLog.Information("________________");

        foreach (var build in routeBuilds)
            CalculateLevelingBuild(build, build.FullIdentifier() == lastIdentifier, lastIdentifier);


         var voyages = FinishedLevelingBuilds.Min(pair => pair.Key);
         PluginLog.Information($"-----------------");
         PluginLog.Information($"Rank 1 -> {TargetRank}");
         PluginLog.Information($"Redeployment {DurationName}");
         PluginLog.Information($"Voyages: {voyages} ({voyages * DateUtil.DurationToTime(Configuration.DurationLimit).TotalHours / 24.0} days)");
         foreach (var build in FinishedLevelingBuilds[voyages])
         {
             PluginLog.Information(build.GetSubmarineBuild.FullIdentifier());
         }
         PluginLog.Information($"-----------------");
         Progress -= 1;

         var l = JsonConvert.SerializeObject(CachedRouteList, new JsonSerializerSettings { Formatting = Formatting.Indented,});

         var path = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "routeList.json");
         PluginLog.Information($"Writing routeList json");
         PluginLog.Information(path);
         File.WriteAllText(path, l);
    }

    public void CalculateLevelingBuild(Build.RouteBuild build, bool isLastBuild, string lastBuildIdentifier)
    {
        var routeList = CachedRouteList.Caches[DurationName];
        routeList.Builds.TryAdd(build.GetSubmarineBuild.FullIdentifier(), new RouteCache(new Dictionary<int, Journey>()));
        if (UnusedCache.TryGetValue(lastBuildIdentifier+DurationName, out var cached) && cached.Exists(s => s == build.FullIdentifier()))
        {
            PluginLog.Information($"Skipping build because previously unused");
            Progress += 1;
            return;
        }

        if (routeList.Builds.TryGetValue(build.FullIdentifier(), out var existingVoyage))
        {
            if (existingVoyage.Voyages.Any())
            {
                var targetRankVoyage = existingVoyage.Voyages.FirstOrDefault(pair => pair.Value.RankReached == TargetRank);
                if (!targetRankVoyage.Equals(new KeyValuePair<int, Journey>()) && targetRankVoyage.Key > CurrentLowestTime)
                {
                    PluginLog.Information($"Skipping build because previously too slow");
                    Progress += 1;
                    return;
                }
            }
        }

        var copiedBuild = new Build.RouteBuild(build)
        {
            Hull = Items.SharkClassHull.GetPartId(),
            Stern = Items.SharkClassStern.GetPartId(),
            Bow = Items.SharkClassBow.GetPartId(),
            Bridge = Items.SharkClassBridge.GetPartId()
        };

        var possibleOldBuilds = GenerateBuildTree(build);
        var lastUsed = copiedBuild.FullIdentifier();

        var i = 1;
        var journey = 0;
        long leftover = 0;
        while (i < TargetRank)
        {
            foreach (var (rank, idx) in build.GetSubmarineBuild.GetPartRanks().Select((val, idx) => (val, idx)))
            {
                if (i >= rank)
                {
                    switch (idx)
                    {
                        // Hull.Rank, Stern.Rank, Bow.Rank, Bridge.Rank
                        case 0: copiedBuild.Hull = build.Hull; break;
                        case 1: copiedBuild.Stern = build.Stern; break;
                        case 2: copiedBuild.Bow = build.Bow; break;
                        case 3: copiedBuild.Bridge = build.Bridge; break;
                    }
                }
                else if (rank == 50)
                {
                    var partId = (idx) switch
                    {
                        // Hull.Rank, Stern.Rank, Bow.Rank, Bridge.Rank
                        0 => build.Hull - 20,
                        1 => build.Stern - 20,
                        2 => build.Bow - 20,
                        3 => build.Bridge - 20,
                    };

                    var part = PartSheet.GetRow((uint)partId)!;
                    if (i >= part.Rank)
                        switch (idx)
                        {
                            // Hull.Rank, Stern.Rank, Bow.Rank, Bridge.Rank
                            case 0: copiedBuild.Hull = (int) part.RowId; break;
                            case 1: copiedBuild.Stern = (int) part.RowId; break;
                            case 2: copiedBuild.Bow = (int) part.RowId; break;
                            case 3: copiedBuild.Bridge = (int) part.RowId; break;
                        }
                }
            }

            if (copiedBuild.GetSubmarineBuild.Speed < 20 || copiedBuild.GetSubmarineBuild.Range < 20)
            {
                PluginLog.Information($"{copiedBuild.FullIdentifier()} impossible with too low speed | range");
                Progress += 1;
                return;
            }

            copiedBuild.Rank = i;
            copiedBuild.Map = 0;

            var gainedExp = 0L;
            var bestPath = Array.Empty<uint>();
            var usedBuild = copiedBuild.FullIdentifier();
            if (routeList.Builds.TryGetValue(usedBuild, out var value) && value.Voyages.TryGetValue(journey, out var cachedProgress))
            {
                i = cachedProgress.RankReached;
                leftover = cachedProgress.Leftover;
                gainedExp = cachedProgress.RouteExp;
                bestPath = cachedProgress.Route;
                journey += 1;

                lastUsed = usedBuild;
            }
            else
            {
                var totalExp = leftover;
                if (i < 50)
                {
                    bestPath = FindBestPath(copiedBuild);

                    foreach (var sector in bestPath)
                    {
                        var sheetSector = ExplorationSheet.GetRow(sector)!;
                        var bonus = CalculateBonusExp(PredictBonusExp(sector, copiedBuild.GetSubmarineBuild).Item1, sheetSector.ExpReward);
                        gainedExp += bonus;
                        totalExp += bonus;
                    }
                }
                else if (i is > 50 and < 70)
                {
                    copiedBuild.Map = 1;
                    bestPath = FindBestPath(copiedBuild);

                    foreach (var sector in bestPath)
                    {
                        var sheetSector = ExplorationSheet.GetRow(sector)!;
                        var bonus = CalculateBonusExp(PredictBonusExp(sector, copiedBuild.GetSubmarineBuild).Item1, sheetSector.ExpReward);
                        gainedExp += bonus;
                        totalExp += bonus;
                    }
                }
                else
                {
                    long expMap1 = 0;
                    long expMap2 = 0;

                    copiedBuild.Map = 1;

                    bestPath = FindBestPath(copiedBuild);

                    foreach (var sector in bestPath)
                    {
                        var sheetSector = ExplorationSheet.GetRow(sector)!;
                        var bonus = CalculateBonusExp(PredictBonusExp(sector, copiedBuild.GetSubmarineBuild).Item1, sheetSector.ExpReward);
                        expMap1 += bonus;
                    }

                    var orgBestPath = bestPath.ToArray();

                    copiedBuild.Map = 2;
                    bestPath = FindBestPath(copiedBuild);

                    foreach (var sector in bestPath)
                    {
                        var sheetSector = ExplorationSheet.GetRow(sector)!;
                        var bonus = CalculateBonusExp(PredictBonusExp(sector, copiedBuild.GetSubmarineBuild).Item1, sheetSector.ExpReward);
                        expMap2 += bonus;
                    }

                    if (expMap1 > expMap2)
                        bestPath = orgBestPath.ToArray();

                    // protection against subs that never go on voyage
                    if (expMap1 == 0 && expMap2 == 0)
                    {
                        Progress += 1;
                        return;
                    }

                    totalExp += expMap1 > expMap2 ? expMap1 : expMap2;
                    gainedExp += expMap1 > expMap2 ? expMap1 : expMap2;
                }

                while (totalExp > 0)
                {
                    var currentRank = RankSheet[i];
                    if (totalExp > currentRank.ExpToNext)
                    {
                        i += 1;
                        totalExp -= currentRank.ExpToNext;
                    }
                    else
                    {
                        leftover = totalExp;
                        totalExp = 0;
                    }
                }

                foreach (var oldBuild in possibleOldBuilds)
                {
                    if (routeList.Builds.TryGetValue(oldBuild.FullIdentifier(), out var oldVersion))
                    {
                        if (oldVersion.Voyages.TryGetValue(journey, out var oldBuildVoyage))
                        {
                            if (oldBuildVoyage.RankReached > i || (oldBuildVoyage.RankReached == i && oldBuildVoyage.Leftover >= leftover))
                            {
                                i = oldBuildVoyage.RankReached;
                                leftover = oldBuildVoyage.Leftover;
                                bestPath = oldBuildVoyage.Route;
                                usedBuild = oldBuild.FullIdentifier();
                            }
                        }
                    }
                }
                lastUsed = usedBuild;

                // we used our final build, so throw away older ones
                if (lastUsed == copiedBuild.FullIdentifier())
                {
                    possibleOldBuilds.Clear();
                    routeList.Builds[lastUsed].Voyages.TryAdd(journey, new Journey(i, leftover, gainedExp, bestPath));
                }

                journey += 1;
            }

            if (isLastBuild)
            {
                var sameRoutes = new List<uint[]> {bestPath};
                var previousJourney = routeList.Builds[usedBuild].Voyages[journey-1];
                if (previousJourney.RouteExp == gainedExp)
                    if (previousJourney.Route != bestPath)
                        sameRoutes.Add(previousJourney.Route);

                var startPoint = Voyage.FindVoyageStartPoint(bestPath.First());
                PluginLog.Information($"Used Build {usedBuild}");
                PluginLog.Information($"Pre-Rank {copiedBuild.Rank}");
                if (sameRoutes.Count == 1)
                    PluginLog.Information($"Voyage {journey}: {Utils.MapToThreeLetter(ExplorationSheet.GetRow(startPoint)!.Map.Row)} {string.Join(" -> ", bestPath.Select(p => Utils.NumToLetter(p - startPoint)))}");
                else
                {
                    PluginLog.Information("Multiple routes with same exp:");
                    foreach (var route in sameRoutes)
                        PluginLog.Information($"{journey} --- {Utils.MapToThreeLetter(ExplorationSheet.GetRow(startPoint)!.Map.Row)} {string.Join(" -> ", route.Select(p => Utils.NumToLetter(p - startPoint)))}");
                }
                PluginLog.Information($"Exp gained {gainedExp}");
                PluginLog.Information($"After-Rank {i}");
                PluginLog.Information($"Leftover: {leftover}");
                PluginLog.Information($"-----------------");
            }
        }

        // Build was never used, so we can skip it
        if (lastUsed != build.FullIdentifier())
        {
            if (lastBuildIdentifier != string.Empty)
            {
                UnusedCache.TryAdd(lastBuildIdentifier + DurationName, new List<string>());
                UnusedCache[lastBuildIdentifier + DurationName].Add(build.FullIdentifier());
            }

            PluginLog.Information($"{build.FullIdentifier()} wasn't used");
            Progress += 1;
            return;
        }

        FinishedLevelingBuilds.TryAdd(journey, new List<Build.RouteBuild>());
        FinishedLevelingBuilds[journey].Add(build);

        Progress += 1;
        CurrentLowestTime = FinishedLevelingBuilds.Min(pair => pair.Key);
    }

    public List<Build.RouteBuild> GenerateBuildTree(Build.RouteBuild startingBuild)
    {
        var sharkBuild = new Build.RouteBuild();
        var fullStartingBuild = startingBuild.GetSubmarineBuild;

        var routeBuilds = new List<Build.RouteBuild>();
        for (var hull = 0; hull < PartsCount; hull++)
        {
            for (var stern = 0; stern < PartsCount; stern++)
            {
                for (var bow = 0; bow < PartsCount; bow++)
                {
                    for (var bridge = 0; bridge < PartsCount; bridge++)
                    {
                        var sHull = (int) fullStartingBuild.Hull.RowId;
                        if (hull < 5 && fullStartingBuild.Hull.Rank == 50)
                            sHull = startingBuild.Hull - 20;

                        var sStern = (int) fullStartingBuild.Stern.RowId;
                        if (stern < 5 && fullStartingBuild.Stern.Rank == 50)
                            sStern = startingBuild.Stern - 20;

                        var sBow = (int) fullStartingBuild.Bow.RowId;
                        if (bow < 5 && fullStartingBuild.Bow.Rank == 50)
                            sBow = startingBuild.Bow - 20;

                        var sBridge = (int) fullStartingBuild.Bridge.RowId;
                        if (bridge < 5 && fullStartingBuild.Bridge.Rank == 50)
                            sBridge = startingBuild.Bridge - 20;

                        var hullPart = (hull * 4) + 3;
                        var sternPart = (stern * 4) + 4;
                        var bowPart = (bow * 4) + 1;
                        var bridgePart = (bridge * 4) + 2;

                        var build = new Build.RouteBuild(
                            1,
                            sHull == hullPart ? sHull : sharkBuild.Hull,
                            sStern == sternPart ? sStern : sharkBuild.Stern,
                            sBow == bowPart ? sBow : sharkBuild.Bow,
                            sBridge == bridgePart ? sBridge : sharkBuild.Bridge
                            );

                        if (!routeBuilds.Exists(b => b.FullIdentifier() == build.FullIdentifier()))
                            routeBuilds.Add(new Build.RouteBuild(build));
                    }
                }
            }
        }

        return routeBuilds;
    }

    public record Journey(int RankReached, long Leftover, long RouteExp, uint[] Route);
    public record RouteCache(Dictionary<int, Journey> Voyages);
    public record BuildCache(Dictionary<string, RouteCache> Builds);
    public class DurationCache
    {
        public Dictionary<string, BuildCache> Caches = new();
    }
}
