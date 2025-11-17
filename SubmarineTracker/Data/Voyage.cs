using System.Collections.Concurrent;
using System.Collections.Frozen;
using Lumina.Excel.Sheets;
using static SubmarineTracker.Data.Build;

namespace SubmarineTracker.Data;

public static class Voyage
{
    public const int FixedVoyageTime = 43200; // 12h

    public static readonly string[] MapNames;
    private static readonly uint[] ReversedMaps;
    private static readonly FrozenDictionary<uint, SubmarineExploration> MapToStartSector;
    public static readonly SubmarineExploration[] PossiblePoints;
    public static readonly FrozenDictionary<uint, SubmarineExploration> SectorToSheet;

    static Voyage()
    {
        SectorToSheet = Sheets.ExplorationSheet.ToFrozenDictionary(s => s.RowId, s => s);

        var allMaps = Sheets.ExplorationSheet.Where(s => s.StartingPoint).ToArray();
        ReversedMaps = allMaps.Select(s => s.RowId).Reverse().ToArray();
        MapToStartSector = allMaps.ToFrozenDictionary(s => s.Map.RowId, s => s);

        MapNames = Sheets.MapSheet.Skip(1).Select(r => r.Name.ExtractText()).ToArray();
        PossiblePoints = Sheets.ExplorationSheet.Where(r => r.ExpReward > 0).ToArray();
    }

    public static SubmarineExploration[] ToExplorationArray(IEnumerable<uint> sectors)
    {
        return sectors.Select(s => SectorToSheet[s]).ToArray();
    }

    public struct BestRoute(uint distance, uint[]? path)
    {
        public readonly uint Distance = distance;
        public readonly uint[] Path = path ?? [];
        public readonly SubmarineExploration[] PathPretty = path?.Select(s => SectorToSheet[s]).ToArray() ?? [];

        public static BestRoute Empty => new(0, []);
    }

    public static SubmarineExploration FindStartFromMap(uint map)
    {
        // RowId 0 is not a valid map
        if (map == 0)
            throw new ArgumentException("Invalid map!");

        return MapToStartSector[map];
    }

    public static uint FindVoyageStart(uint sector)
    {
        // This works because we reversed the list of start points
        return ReversedMaps.FirstOrDefault(m => sector >= m);
    }

    public static uint FindMapFromSector(uint sector) => SectorToSheet[FindVoyageStart(sector)].Map.RowId;
    public static SubmarineExploration FindVoyageStartPretty(uint sector) => SectorToSheet[FindVoyageStart(sector)];

    public static string SectorToName(uint sector) => SectorToSheet[sector].ToName();
    public static string SectorToMapName(uint sector) => Utils.UpperCaseStr(FindVoyageStartPretty(sector).Map.Value.Name);
    public static string SectorToMapShort(uint sector) => Utils.MapToShort(FindMapFromSector(sector));
    public static string SectorToMapThreeLetter(uint sector) => Utils.MapToThreeLetter(FindMapFromSector(sector));

    #region Optimizer
    public static uint CalculateDuration(SubmarineExploration[] sectors, float speed)
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

    public static BestRoute FindBestRoute(RouteBuild build, uint[] unlocked, uint[] mustInclude, uint[] allowed, bool ignoreUnlocks, bool avgExpBonus)
    {
        var valid = Sheets.ExplorationSheet
                .Where(r => r.Map.RowId == build.MapRowId && !r.StartingPoint && r.RankReq <= build.Rank)
                .Where(r => allowed.Length != 0 ? allowed.Contains(r.RowId) : ignoreUnlocks || unlocked.Contains(r.RowId))
                .Select(r => r.RowId)
                .ToArray();

        var subBuild = build.GetSubmarineBuild;
        var bestPath = Importer.CalculatedData.Maps[(int) build.MapRowId]
                               .AsParallel()
                               .Where(t => t.Distance <= subBuild.Range)            // distance sort
                               .Where(p => valid.ContainsAllItems(p.Sectors))       // only valid routes
                               .Where(p => p.Sectors.ContainsAllItems(mustInclude)) // must include
                               .Select(t =>
                               {
                                   var sectors = ToExplorationArray(t.Sectors);
                                   return (
                                              Path: t.Sectors,
                                              Distance: t.Distance,
                                              Duration: CalculateDuration(sectors, subBuild.Speed),
                                              Exp: Sectors.CalculateExpForSectors(sectors, subBuild, avgExpBonus)
                                          );
                               })
                               .Where(t => t.Duration < Plugin.Configuration.DurationLimit.ToSeconds())
                               .OrderByDescending(t => Plugin.Configuration.MaximizeDuration ? t.Exp : t.Exp / (t.Duration / 60))
                               .ThenBy(t => t.Duration)
                               .FirstOrDefault();

        return new BestRoute(bestPath.Distance, bestPath.Path);
    }

    public static BestRoute FindCalculatedRoute(uint[] sectors)
    {
        if (sectors.Length == 0)
            return BestRoute.Empty;

        var map = (int) FindMapFromSector(sectors[0]);
        return Importer.HashedRoutes[map].TryGetValue(Utils.GetUniqueHash(sectors), out var route)
                   ? new BestRoute(route.Distance, route.Sectors)
                   : BestRoute.Empty;
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

    #region ExportFunction
    public static Route[] FindAllRoutes(uint map)
    {
        var valid = Sheets.ExplorationSheet.Where(r => r.Map.RowId == map && !r.StartingPoint).Select(p => p.RowId).ToArray();

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
            .Select(t => ToExplorationArray(t.Skip(1)))
            .Select(CalculateDistance)
            .Select(t => new Route
            {
                Distance = t.Distance,
                Sectors = t.Path.Select(s => s.RowId).ToArray(),
            })
            .ToArray();
    }

    private static readonly ConcurrentDictionary<uint, uint> Distances = new();
    private static (uint Distance, SubmarineExploration[] Path) CalculateDistance(SubmarineExploration[] sectors)
    {
        var solution = (0u, Array.Empty<SubmarineExploration>());
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

                Distances.TryAdd(Utils.GetUniqueId(sector.RowId, row), sector.GetDistance(SectorToSheet[row]));
            }
        }

        var final = (Distance: uint.MaxValue, Path: Array.Empty<uint>());
        foreach (var path in Utils.Permutations.GetAllPermutation(route))
        {
            var distance = Distances[Utils.GetUniqueId(path[0],start.RowId)] + SectorToSheet[path[0]].SurveyDistance;
            for (var i = 0; i < path.Length - 1; i++)
                distance += Distances[Utils.GetUniqueId(path[i], path[i + 1])] + SectorToSheet[path[i + 1]].SurveyDistance;

            if (distance < final.Distance)
                final = (distance, path);
        }

        return (final.Distance, ToExplorationArray(final.Path));
    }
    #endregion
}
