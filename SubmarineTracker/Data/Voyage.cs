using System.Collections.Concurrent;
using Lumina.Excel;

namespace SubmarineTracker.Data;

public static class Voyage
{
    private static Plugin Plugin = null!;

    private const int FixedVoyageTime = 43200; // 12h

    private static ExcelSheet<SubmarineExplorationPretty> ExplorationSheet = null!;
    private static List<uint> ReversedMaps = null!;
    private static readonly Dictionary<uint, SubmarineExplorationPretty> SectorToPretty = new();

    public static void Initialize(Plugin plugin)
    {
        Plugin = plugin;

        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExplorationPretty>()!;
        ReversedMaps = ExplorationSheet.Where(s => s.StartingPoint).Select(s => s.RowId).Reverse().ToList();

        foreach (var s in ExplorationSheet)
            SectorToPretty[s.RowId] = s;
    }

    public static uint FindMapFromSector(uint sector)
    {
        return ExplorationSheet.GetRow(FindVoyageStart(sector))!.Map.Row;
    }

    public static uint FindVoyageStart(uint sector)
    {
        // This works because we reversed the list of start points
        foreach (var possibleStart in ReversedMaps)
            if (sector > possibleStart)
                return possibleStart;

        return 0;
    }

    #region Optimizer
    public static uint CalculateDuration(IEnumerable<SubmarineExplorationPretty> walkingPoints, Build.SubmarineBuild build)
    {
        var walkWay = walkingPoints.ToArray();
        var start = walkWay.First();

        var points = new List<SubmarineExplorationPretty>();
        foreach (var p in walkWay.Skip(1))
            points.Add(p);

        switch (points.Count)
        {
            case 0:
                return 0;
            case 1:
                {
                    var onlyPoint = points[0];
                    return VoyageTime(start, onlyPoint, (short)build.Speed) + SurveyTime(onlyPoint, (short)build.Speed) + FixedVoyageTime;
                }
            case > 5:
                return 0;
        }

        var allDurations = new List<long>();
        for (var i = 0; i < points.Count; i++)
        {
            var voyage = i == 0
                             ? VoyageTime(start, points[0], (short)build.Speed)
                             : VoyageTime(points[i - 1], points[i], (short)build.Speed);
            var survey = SurveyTime(points[i], (short)build.Speed);
            allDurations.Add(voyage + survey);
        }

        return (uint) allDurations.Sum() + FixedVoyageTime;
    }

    public static uint[] FindBestPath(Build.RouteBuild routeBuild, uint[] unlockedSectors, uint[] mustInclude, uint[]? allowedSectors = null, bool ignoreUnlocks = false)
    {
        SubmarineExplorationPretty[] valid;
        if (allowedSectors != null && allowedSectors.Any())
        {
            valid = ExplorationSheet
                    .Where(r => r.Map.Row == routeBuild.Map + 1 && !r.StartingPoint && r.RankReq <= routeBuild.Rank)
                    .Where(r => allowedSectors.Contains(r.RowId))
                    .ToArray();
        }
        else
        {
            valid = ExplorationSheet
                        .Where(r => r.Map.Row == routeBuild.Map + 1 && !r.StartingPoint && r.RankReq <= routeBuild.Rank)
                        .Where(r => ignoreUnlocks || unlockedSectors.Contains(r.RowId))
                        .ToArray();
        }

        var startPoint = ExplorationSheet.First(r => r.Map.Row == routeBuild.Map + 1);
        var paths = valid.Select(t => new[] { startPoint.RowId, t.RowId } ).ToHashSet(new Utils.ArrayComparer());
        if (mustInclude.Any())
            paths = new [] { mustInclude.Prepend(startPoint.RowId).ToArray() }.ToHashSet(new Utils.ArrayComparer());

        var i = mustInclude.Any() ? mustInclude.Length : 1;
        while (i++ < 5)
        {
            foreach (var path in paths.ToArray())
                foreach (var validPoint in valid.Where(t => !path.Contains(t.RowId)))
                    paths.Add(path.Append(validPoint.RowId).ToArray());
        }

        var allPaths = paths.AsParallel().Select(t => t.Select(f => valid.FirstOrDefault(k => k.RowId == f) ?? startPoint)).ToList();

        if (!allPaths.Any())
            return Array.Empty<uint>();

        var build = routeBuild.GetSubmarineBuild;
        var must = mustInclude.Select(s => ExplorationSheet.GetRow(s)!);
        var optimalDistances = allPaths.AsParallel().Select(CalculateDistance).Where(t => t.Item1 <= build.Range && t.Item2.ContainsAllItems(must)).ToArray();
        if (!optimalDistances.Any())
            return Array.Empty<uint>();

        var bestPath = optimalDistances.AsParallel().Select(tuple =>
            {
                var path = tuple.Item2.Prepend(startPoint).ToArray();

                return new Tuple<uint[], TimeSpan, double>(
                    tuple.Item2.Select(t => t.RowId).ToArray(),
                    TimeSpan.FromSeconds(CalculateDuration(path, build)),
                    Sectors.CalculateExpForSectors(tuple.Item2.ToArray(), build)
                );
            })
          .Where(t => t.Item2 < Plugin.Configuration.DurationLimit.ToTime(Plugin.Configuration.CustomHour, Plugin.Configuration.CustomMinute))
          .OrderByDescending(t => Plugin.Configuration.MaximizeDuration ? t.Item3 : t.Item3 / t.Item2.TotalMinutes)
          .Select(t => t.Item1)
          .FirstOrDefault();

        return bestPath ?? Array.Empty<uint>();
    }

    private static readonly ConcurrentDictionary<uint, uint> Distances = new();
    public static (uint Distance, SubmarineExplorationPretty[]) CalculateDistance(IEnumerable<SubmarineExplorationPretty> walkingPoints)
    {
        var walkWay = walkingPoints.ToArray();
        var start = walkWay.First();
        walkWay = walkWay.Skip(1).ToArray();

        var route = walkWay.Select(p => p.RowId).ToArray();

        var solution = (0u, Array.Empty<SubmarineExplorationPretty>());
        switch (route.Length)
        {
            case 0:
                return solution;
            case 1:
                return (BestDistance(start, walkWay[0]) + walkWay[0].SurveyDistance, new [] {walkWay[0]});
            // More than 5 points isn't allowed ingame
            case > 5:
                return solution;
        }

        foreach (var sector in walkWay)
        {
            // Add start -> sector
            Distances.TryAdd(Utils.GetUniqueId(sector.RowId, start.RowId), start.GetDistance(sector));

            for (var i = Array.IndexOf(route, sector.RowId); i < route.Length - 1; i++)
            {
                var row = route[i + 1];
                if (sector.RowId == row)
                    continue;

                Distances.TryAdd(Utils.GetUniqueId(sector.RowId, row), sector.GetDistance(SectorToPretty[row]));
            }
        }

        (uint Distance, uint[] Path) final = (uint.MaxValue, Array.Empty<uint>());
        foreach (var path in Utils.Permutations.GetAllPermutation(route))
        {
            var distance = Distances[Utils.GetUniqueId(path[0],start.RowId)] + SectorToPretty[path[0]].SurveyDistance;
            for (var i = 0; i < path.Length - 1; i++)
                distance += Distances[Utils.GetUniqueId(path[i], path[i + 1])] + SectorToPretty[path[i + 1]].SurveyDistance;

            if (distance < final.Distance)
                final = (distance, path);
        }

        return (final.Distance, final.Path.Select(s => ExplorationSheet.GetRow(s)!).ToArray());
    }

    public static uint BestDistance(SubmarineExplorationPretty pointA, SubmarineExplorationPretty pointB)
    {
        return pointA.GetDistance(pointB);
    }

    public static uint VoyageTime(SubmarineExplorationPretty pointA, SubmarineExplorationPretty pointB, short speed)
    {
        return pointA.GetVoyageTime(pointB, speed);
    }

    public static uint SurveyTime(SubmarineExplorationPretty point, short speed)
    {
        return point.GetSurveyTime(speed);
    }
    #endregion
}
