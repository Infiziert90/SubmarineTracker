using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace SubmarineTracker.Data;

public static class Submarines
{
    private static ExcelSheet<Item> ItemSheet = null!;
    private static ExcelSheet<SubmarineRank> RankSheet = null!;
    private static ExcelSheet<SubmarinePart> PartSheet = null!;

    public static void Initialize()
    {
        ItemSheet = Plugin.Data.GetExcelSheet<Item>()!;
        RankSheet = Plugin.Data.GetExcelSheet<SubmarineRank>()!;
        PartSheet = Plugin.Data.GetExcelSheet<SubmarinePart>()!;
    }

    public record FcSubmarines(string Tag, string World, List<Submarine> Submarines)
    {
        public static FcSubmarines Empty => new("", "Unknown", new List<Submarine>());
    }

    public record Submarine(string Name, uint Rank, ushort Hull, ushort Stern, ushort Bow, ushort Bridge)
    {
        [JsonConstructor]
        public Submarine() : this("", 0, 0,0,0,0) { }

        public unsafe Submarine(HousingWorkshopSubmersibleSubData data) : this("", 0, 0,0,0,0)
        {
            Name = MemoryHelper.ReadSeStringNullTerminated(new nint(data.Name)).ToString();
            Rank = data.RankId;
            Hull = data.HullId;
            Stern = data.SternId;
            Bow = data.BowId;
            Bridge = data.BridgeId;
        }

        private string GetPartName(ushort partId) => ItemSheet.GetRow(PartIdToItemId[partId])!.Name.ToString();
        private uint GetIconId(ushort partId) => ItemSheet.GetRow(PartIdToItemId[partId])!.Icon;

        [JsonIgnore]
        #region parts
        public string HullName   => GetPartName(Hull);
        public string SternName  => GetPartName(Stern);
        public string BowName    => GetPartName(Bow);
        public string BridgeName => GetPartName(Bridge);

        public uint HullIconId   => GetIconId(Hull);
        public uint SternIconId  => GetIconId(Stern);
        public uint BowIconId    => GetIconId(Bow);
        public uint BridgeIconId => GetIconId(Bridge);

        public string BuildIdentifier()
        {
            var identifier = $"{ToIdentifier(Hull)}{ToIdentifier(Stern)}{ToIdentifier(Bow)}{ToIdentifier(Bridge)}";

            if (identifier.Count(l => l == '+') == 4)
                identifier = $"{identifier.Replace("+", "")}++";

            return identifier;
        }

        #endregion

        public bool IsValid() => Rank > 0;

        #region equals
        public virtual bool Equals(Submarine? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Name == other.Name && Rank == other.Rank && Hull == other.Hull && Stern == other.Stern && Bow == other.Bow && Bridge == other.Bridge;
        }

        public bool Equals(Submarine x, Submarine y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;

            return x.Name == y.Name && x.Rank == y.Rank && x.Hull == y.Hull &&
                   x.Stern == y.Stern && x.Bow == y.Bow && x.Bridge == y.Bridge;
        }

        public override int GetHashCode() => HashCode.Combine(Name, Rank, Hull, Stern, Bow, Bridge);
        #endregion
    }

    public readonly struct SubmarineBuild
    {
        private readonly SubmarineRank Bonus;
        private readonly SubmarinePart Hull;
        private readonly SubmarinePart Stern;
        private readonly SubmarinePart Bow;
        private readonly SubmarinePart Bridge;

        public SubmarineBuild(int rank, int hull, int stern, int bow, int bridge)
        {
            Bonus = GetRank(rank);
            Hull = GetPart(hull);
            Stern = GetPart(stern);
            Bow = GetPart(bow);
            Bridge = GetPart(bridge);
        }

        public int Surveillance => Bonus.SurveillanceBonus + Hull.Surveillance + Stern.Surveillance + Bow.Surveillance + Bridge.Surveillance;
        public int Retrieval => Bonus.RetrievalBonus + Hull.Retrieval + Stern.Retrieval + Bow.Retrieval + Bridge.Retrieval;
        public int Speed => Bonus.SpeedBonus + Hull.Speed + Stern.Speed + Bow.Speed + Bridge.Speed;
        public int Range => Bonus.RangeBonus + Hull.Range + Stern.Range + Bow.Range + Bridge.Range;
        public int Favor => Bonus.FavorBonus + Hull.Favor + Stern.Favor + Bow.Favor + Bridge.Favor;

        private SubmarineRank GetRank(int rank) => RankSheet.GetRow((uint) rank)!;
        private SubmarinePart GetPart(int partId) => PartSheet.GetRow((uint) partId)!;

        public bool EqualsSubmarine(Submarine other)
        {
            return Bonus.RowId == other.Rank && Hull.RowId == other.Hull && Stern.RowId == other.Stern && Bow.RowId == other.Bow && Bridge.RowId == other.Bridge;
        }
    }

    public static bool SubmarinesEqual(List<Submarine> l, List<Submarine> r)
    {
        if (!l.Any() || !r.Any())
            return false;

        foreach (var (subL, subR) in l.Zip(r))
        {
            if (!subL.Equals(subR))
                return false;
        }

        return true;
    }

    public static Dictionary<ulong, FcSubmarines> KnownSubmarines = new();

    public static Dictionary<ushort, uint> PartIdToItemId = new Dictionary<ushort, uint>
    {
        // Shark
        { 1, 21792 }, // Bow
        { 2, 21793 }, // Bridge
        { 3, 21794 }, // Hull
        { 4, 21795 }, // Stern

        // Ubiki
        { 5, 21796 },
        { 6, 21797 },
        { 7, 21798 },
        { 8, 21799 },

        // Whale
        { 9, 22526 },
        { 10, 22527 },
        { 11, 22528 },
        { 12, 22529 },

        // Coelacanth
        { 13, 23903 },
        { 14, 23904 },
        { 15, 23905 },
        { 16, 23906 },

        // Syldra
        { 17, 24344 },
        { 18, 24345 },
        { 19, 24346 },
        { 20, 24347 },

        // Modified same order
        { 21, 24348 },
        { 22, 24349 },
        { 23, 24350 },
        { 24, 24351 },

        { 25, 24352 },
        { 26, 24353 },
        { 27, 24354 },
        { 28, 24355 },

        { 29, 24356 },
        { 30, 24357 },
        { 31, 24358 },
        { 32, 24359 },

        { 33, 24360 },
        { 34, 24361 },
        { 35, 24362 },
        { 36, 24363 },

        { 37, 24364 },
        { 38, 24365 },
        { 39, 24366 },
        { 40, 24367 }
    };

    public static string ToIdentifier(ushort partId)
    {
        return ((partId - 1) / 4) switch
        {
            0 => "S",
            1 => "U",
            2 => "W",
            3 => "C",
            4 => "Y",

            5 => $"{ToIdentifier((ushort)(partId - 20))}+",
            6 => $"{ToIdentifier((ushort)(partId - 20))}+",
            7 => $"{ToIdentifier((ushort)(partId - 20))}+",
            8 => $"{ToIdentifier((ushort)(partId - 20))}+",
            9 => $"{ToIdentifier((ushort)(partId - 20))}+",
            _ => "Unknown"
        };
    }

    public static void LoadCharacters()
    {
        foreach (var file in Plugin.PluginInterface.ConfigDirectory.EnumerateFiles())
        {
            ulong id;
            try
            {
                id = Convert.ToUInt64(Path.GetFileNameWithoutExtension(file.Name));
            }
            catch (Exception e)
            {
                PluginLog.Error($"Found file that isn't convertable. Filename: {file.Name}");
                PluginLog.Error(e.Message);
                continue;
            }

            var config = CharacterConfiguration.Load(id);

            KnownSubmarines.TryAdd(id, FcSubmarines.Empty);
            var playerFc = KnownSubmarines[id];

            if (SubmarinesEqual(playerFc.Submarines, config.Submarines))
                continue;

            KnownSubmarines[id] = new FcSubmarines(config.Tag, config.World, config.Submarines);
        }
    }

    public static void SaveCharacter()
    {
        var id = Plugin.ClientState.LocalContentId;
        if (!KnownSubmarines.TryGetValue(id, out var playerFc))
            return;

        var config = new CharacterConfiguration(id, playerFc.Tag, playerFc.World, playerFc.Submarines);
        config.Save();
    }

    #region RangeOptimizer

    public static uint CalculateDistance(List<uint> walkingPoints)
    {
        // spin it up?
        if (HousingManager.GetSubmarineVoyageDistance(32, 33) == 0)
        {
            PluginLog.Error("GetSubmarineVoyageDistance was zero.");
            return 0;
        }

        var sheet = Plugin.Data.GetExcelSheet<SubmarineExploration>()!;
        var start = sheet.GetRow(walkingPoints[0])!;

        var points = new List<SubmarineExploration>();
        foreach (var p in walkingPoints.Skip(1))
            points.Add(sheet.GetRow(p)!);


        // Less than 1 or more than 5 points isn't allowed ingame
        if (points.Count is <= 1 or > 5)
            return 0;

        List<(uint Key, uint Start, Dictionary<uint, uint> Distances)> AllDis = new();
        foreach (var (point, idx) in points.Select((val, i) => (val, i)))
        {
            AllDis.Add((point.RowId, BestDistance(start.RowId, point.RowId), new()));

            foreach (var iPoint in points)
            {
                if (point.RowId == iPoint.RowId)
                    continue;

                AllDis[idx].Distances.Add(iPoint.RowId, BestDistance(point.RowId, iPoint.RowId));
            }
        }

        List<(uint Way, List<uint> Points)> MinimalWays = new List<(uint Way, List<uint> Points)>();
        try
        {
            foreach (var (point, idx) in AllDis.Select((val, i) => (val, i)))
            {
                var otherPoints = AllDis.ToList();
                otherPoints.RemoveAt(idx);

                var others = new Dictionary<uint, Dictionary<uint, uint>>();
                foreach (var p  in otherPoints)
                {
                    var listDis = new Dictionary<uint, uint>();
                    foreach (var dis in p.Distances)
                    {
                        listDis.Add(dis.Key, dis.Value);
                    }

                    others[p.Key] = listDis;
                }

                MinimalWays.Add(PathWalker(point, others));
            }
        }
        catch (Exception e)
        {
            PluginLog.Error(e.Message);
            PluginLog.Error(e.StackTrace);
        }

        var min = MinimalWays.MinBy(m => m.Way);
        var surveyD = min.Points.Sum(d => sheet.GetRow(d)!.SurveyDistance);
        return (uint) (min.Way + surveyD);
    }

    public static (uint Distance, List<uint> Points) PathWalker((uint Key, uint Start, Dictionary<uint, uint> Distances) point, Dictionary<uint, Dictionary<uint, uint>> otherPoints)
    {
        List<(uint Distance, List<uint> Points)> PossibleDistances = new();
        foreach (var pos1 in otherPoints)
        {
            if (point.Key == pos1.Key)
                continue;

            var dis1 = point.Distances[pos1.Key];

            if (otherPoints.Count > 1)
            {
                foreach (var pos2 in otherPoints)
                {
                    if (pos1.Key == pos2.Key || point.Key == pos2.Key)
                        continue;

                    var dis2 = otherPoints[pos1.Key][pos2.Key];

                    if (otherPoints.Count > 2)
                    {
                        foreach (var pos3 in otherPoints)
                        {
                            if (pos1.Key == pos3.Key || pos2.Key == pos3.Key || point.Key == pos3.Key)
                                continue;

                            var dis3 = otherPoints[pos2.Key][pos3.Key];

                            if (otherPoints.Count > 3)
                            {
                                foreach (var pos4 in otherPoints)
                                {
                                    if (pos1.Key == pos4.Key || pos2.Key == pos4.Key || pos3.Key == pos4.Key || point.Key == pos4.Key)
                                        continue;

                                    var dis4 = otherPoints[pos3.Key][pos4.Key];

                                    PossibleDistances.Add((dis1 + dis2 + dis3 + dis4 + point.Start, new() { point.Key, pos1.Key, pos2.Key, pos3.Key, pos4.Key, }));
                                }
                            }
                            else { PossibleDistances.Add((dis1 + dis2 + dis3 + point.Start, new() { point.Key, pos1.Key, pos2.Key, pos3.Key, })); }
                        }
                    }
                    else { PossibleDistances.Add((dis1 + dis2 + point.Start, new() { point.Key, pos1.Key, pos2.Key, })); }
                }
            }
            else { PossibleDistances.Add((dis1 + point.Start, new() { point.Key, pos1.Key, })); }
        }

        var min = PossibleDistances.Min(a => a.Distance);
        return PossibleDistances.Find(a => a.Distance == min);
    }

    public static uint BestDistance(uint pointA, uint pointB)
    {
        return HousingManager.GetSubmarineVoyageDistance((byte) pointA, (byte) pointB);
    }

    #endregion
}
