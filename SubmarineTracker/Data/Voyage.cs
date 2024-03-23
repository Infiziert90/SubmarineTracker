using System.Collections.Concurrent;
using System.Collections.Frozen;
using Lumina.Excel;
using static SubmarineTracker.Data.Build;

namespace SubmarineTracker.Data;

public static class Voyage
{
    public const int FixedVoyageTime = 43200; // 12h

    private static ExcelSheet<SubExplPretty> ExplorationSheet;

    private static uint[] ReversedMaps;
    private static FrozenDictionary<uint, SubExplPretty> SectorToPretty;

    static Voyage()
    {
        ExplorationSheet = Plugin.Data.GetExcelSheet<SubExplPretty>()!;
        ReversedMaps = ExplorationSheet.Where(s => s.StartingPoint).Select(s => s.RowId).Reverse().ToArray();

        SectorToPretty = ExplorationSheet.ToFrozenDictionary(s => s.RowId);
    }

    public struct BestRoute(uint distance, uint[]? path)
    {
        public readonly uint Distance = distance;
        public readonly uint[] Path = path ?? [];
        public readonly SubExplPretty[] PathPretty = path?.Select(s => SectorToPretty[s]).ToArray() ?? [];

        public static BestRoute Empty() => new(0, []);
    }

    public static uint FindVoyageStart(uint sector)
    {
        // This works because we reversed the list of start points
        return ReversedMaps.FirstOrDefault(m => sector > m);
    }

    public static uint FindMapFromSector(uint sector) => SectorToPretty[FindVoyageStart(sector)].Map.Row;
    public static SubExplPretty FindVoyageStartPretty(uint sector) => SectorToPretty[FindVoyageStart(sector)];

    #region Optimizer
    public static uint CalculateDuration(SubExplPretty[] sectors, float speed)
    {
        if (sectors.Length is 0 or > 5)
            return 0;

        var start = FindVoyageStartPretty(sectors[0].RowId);
        if (sectors.Length == 1)
            return start.CalcTime(sectors[0], speed) + FixedVoyageTime;

        var durations = start.CalcTime(sectors[0], speed);
        for (var i = 1; i < sectors.Length; i++)
            durations += sectors[i - 1].CalcTime(sectors[i], speed);

        return durations + FixedVoyageTime;
    }

    public static Route[] FindAllRoutes(uint map)
    {
        var valid = ExplorationSheet.Where(r => r.Map.Row == map && !r.StartingPoint).Select(p => p.RowId).ToArray();

        var startPoint = FindVoyageStartPretty(valid[0]);
        var paths = valid.Select(t => new[] { startPoint.RowId, t } ).ToHashSet(new Utils.ArrayComparer());

        var i = 1;
        while (i++ < 5)
        {
            foreach (var path in paths.ToArray())
                foreach (var validPoint in valid.Where(t => !path.Contains(t)))
                    paths.Add(path.Append(validPoint).ToArray());
        }

        return paths.AsParallel()
                   .Select(t => t.Skip(1).Select(p => SectorToPretty[p]).ToArray())
                   .Select(CalculateDistance)
                   .Select(t => new Route
                   {
                       Distance = t.Distance,
                       Sectors = t.Path.Select(s => s.RowId).ToArray(),
                   })
                   .ToArray();
    }

    public static BestRoute FindBestRoute(RouteBuild build, uint[] unlocked, uint[] mustInclude, uint[] allowed, bool ignoreUnlocks, bool avgExpBonus)
    {
        var valid = ExplorationSheet
                .Where(r => r.Map.Row == build.Map + 1 && !r.StartingPoint && r.RankReq <= build.Rank)
                .Where(r => allowed.Length != 0 ? allowed.Contains(r.RowId) : ignoreUnlocks || unlocked.Contains(r.RowId))
                .Select(r => r.RowId)
                .ToArray();

        var subBuild = build.GetSubmarineBuild;
        var bestPath = Importer.CalculatedData.Maps[build.Map + 1]
                             .AsParallel()
                             .Where(t => t.Distance <= subBuild.Range)              // distance sort
                             .Where(p => valid.ContainsAllItems(p.Sectors))         // only valid routes
                             .Where(p => p.Sectors.ContainsAllItems(mustInclude))   // must include
                             .Select(t =>
                             {
                                 var sectors = t.Sectors.Select(p => SectorToPretty[p]).ToArray();
                                 return (
                                     Path: t.Sectors,
                                     Distance: t.Distance,
                                     Duration: CalculateDuration(sectors, subBuild.Speed),
                                     Exp: Sectors.CalculateExpForSectors(sectors, subBuild, avgExpBonus)
                                 );
                             })
                             .Where(t => t.Duration < Plugin.Configuration.DurationLimit.ToSeconds())
                             .OrderByDescending(t => Plugin.Configuration.MaximizeDuration ? t.Exp : t.Exp / (t.Duration / 60))
                             .ThenByDescending(t => t.Duration)
                             .FirstOrDefault();

        return new BestRoute(bestPath.Distance, bestPath.Path);
    }

    private static readonly ConcurrentDictionary<uint, uint> Distances = new();

    public static BestRoute FindCalculatedRoute(uint[] sectors)
    {
        if (sectors.Length == 0)
            return BestRoute.Empty();

        var map = (int) FindMapFromSector(sectors[0]);
        return Importer.HashedRoutes[map].TryGetValue(Utils.GetUniqueHash(sectors), out var route)
                   ? new BestRoute(route.Distance, route.Sectors)
                   : BestRoute.Empty();
    }

    public static (uint Distance, SubExplPretty[] Path) CalculateDistance(IEnumerable<uint> sectors) =>
        CalculateDistance(sectors.Select(ExplorationSheet.GetRow).ToArray()!);

    public static (uint Distance, SubExplPretty[] Path) CalculateDistance(SubExplPretty[] sectors)
    {
        var solution = (0u, Array.Empty<SubExplPretty>());
        if (sectors.Length is 0 or > 5)
            return solution;

        var start = FindVoyageStartPretty(sectors[0].RowId);
        if (sectors.Length == 1)
            return (start.GetDistance(sectors[0]) + sectors[0].SurveyDistance, [sectors[0]]);

        var route = sectors.Select(p => p.RowId).ToArray();
        foreach (var sector in sectors)
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

        var final = (Distance: uint.MaxValue, Path: Array.Empty<uint>());
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
    #endregion

    #region CommonRoutes
    public record CommonRoute(int Map, params uint[] Route);
    public static readonly Dictionary<string, CommonRoute> Common = new()
    {
        { "Fight Club - OJ", new CommonRoute(0, 15, 10) },
        { "Fight Club - JORZ", new CommonRoute(0, 10, 15, 18, 26) },
        { "Fight Club - MROJZ", new CommonRoute(0, 13, 18, 15, 10, 26) },
        { "Fight Club - JOZ", new CommonRoute(0, 10, 15, 26) },
        { "Fight Club - MROJ", new CommonRoute(0, 13, 18, 15, 10) },
        { "Fight Club - MOJZ", new CommonRoute(0, 13, 15, 10, 26) },

        { "CryPTO - AB", new CommonRoute(1, 32, 33) },
        { "CryPTO - BACD", new CommonRoute(1, 33, 32, 34, 35) },
        { "CryPTO - BACMN", new CommonRoute(1, 33, 32, 34, 44, 45) },
        { "Infi - BACMQ", new CommonRoute(1, 33, 32, 34, 44, 48) },
    };
    #endregion
}
