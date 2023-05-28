using System.IO;
using System.Runtime.InteropServices;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game;
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
    private static ExcelSheet<SubmarineExploration> ExplorationSheet = null!;

    private static List<SubmarineExploration> PossiblePoints = new();

    private const int FixedVoyageTime = 43200; // 12h

    public static void Initialize()
    {
        ItemSheet = Plugin.Data.GetExcelSheet<Item>()!;
        RankSheet = Plugin.Data.GetExcelSheet<SubmarineRank>()!;
        PartSheet = Plugin.Data.GetExcelSheet<SubmarinePart>()!;
        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExploration>()!;

        PossiblePoints = ExplorationSheet.Where(r => r.ExpReward > 0).ToList();
    }

    public class FcSubmarines
    {
        public string CharacterName = "";
        public string Tag = null!;
        public string World = null!;
        public List<Submarine> Submarines = null!;

        public Dictionary<uint, SubmarineLoot> SubLoot = new();
        public Dictionary<uint, bool> UnlockedSectors = new();
        public Dictionary<uint, bool> ExploredSectors = new();

        [JsonConstructor]
        public FcSubmarines() { }

        public FcSubmarines(string characterName, string tag, string world, List<Submarine> submarines, Dictionary<uint, SubmarineLoot> loot, List<Tuple<uint, bool, bool>> points)
        {
            CharacterName = characterName;
            Tag = tag;
            World = world;
            Submarines = submarines;
            SubLoot = loot;
            foreach (var (point, unlocked, explored) in points)
            {
                UnlockedSectors[point] = unlocked;
                ExploredSectors[point] = explored;
            }
        }

        public static FcSubmarines Empty => new("", "", "Unknown", new List<Submarine>(), new Dictionary<uint, SubmarineLoot>(), new List<Tuple<uint, bool, bool>>());

        public void AddSubLoot(uint key, uint returnTime, Span<HousingWorkshopSubmarineGathered> data)
        {
            SubLoot.TryAdd(key, new SubmarineLoot());

            var sub = SubLoot[key];
            sub.Add(returnTime, data);
        }

        public void GetUnlockedAndExploredSectors()
        {
            foreach (var submarineExploration in ExplorationSheet)
            {
                UnlockedSectors[submarineExploration.RowId] = HousingManager.IsSubmarineExplorationUnlocked((byte)submarineExploration.RowId);
                ExploredSectors[submarineExploration.RowId] = HousingManager.IsSubmarineExplorationExplored((byte)submarineExploration.RowId);
            }
        }

        #region Loot
        [JsonIgnore] public bool Refresh = true;
        [JsonIgnore] public Dictionary<uint, Dictionary<Item, int>> AllLoot = new();
        [JsonIgnore] public Dictionary<DateTime, Dictionary<Item, int>> TimeLoot = new();

        public void RebuildStats()
        {
            if (Refresh)
                Refresh = false;
            else
                return;


            AllLoot.Clear();
            foreach (var point in PossiblePoints)
            {
                var possibleLoot = SubLoot.Values.SelectMany(val => val.LootForPoint(point.RowId)).ToList();
                foreach (var pointLoot in possibleLoot)
                {
                    var lootList = AllLoot.GetOrCreate(point.RowId);
                    if (!lootList.TryAdd(pointLoot.PrimaryItem, pointLoot.PrimaryCount))
                        lootList[pointLoot.PrimaryItem] += pointLoot.PrimaryCount;

                    if (!pointLoot.ValidAdditional)
                        continue;

                    if (!lootList.TryAdd(pointLoot.AdditionalItem, pointLoot.AdditionalCount))
                        lootList[pointLoot.AdditionalItem] += pointLoot.AdditionalCount;
                }
            }

            TimeLoot.Clear();
            foreach (var point in PossiblePoints)
            {
                var possibleLoot = SubLoot.Values.SelectMany(val => val.LootForPointWithTime(point.RowId)).ToList();
                foreach (var (date, loot) in possibleLoot)
                {
                    var timeList = TimeLoot.GetOrCreate(date);
                    if (!timeList.TryAdd(loot.PrimaryItem, loot.PrimaryCount))
                        timeList[loot.PrimaryItem] += loot.PrimaryCount;

                    if (!loot.ValidAdditional)
                        continue;

                    if (!timeList.TryAdd(loot.AdditionalItem, loot.AdditionalCount))
                        timeList[loot.AdditionalItem] += loot.AdditionalCount;
                }
            }
        }

        #endregion
    }

    public record LootWithDate(DateTime Date, DetailedLoot Loot);

    public class SubmarineLoot
    {
        public Dictionary<uint, List<DetailedLoot>> Loot = new();

        [JsonConstructor]
        public SubmarineLoot() { }

        public void Add(uint returnTime, Span<HousingWorkshopSubmarineGathered> data)
        {
            if (data[0].ItemIdPrimary == 0)
                return;

            if (!Loot.TryAdd(returnTime, new List<DetailedLoot>()))
                return;

            foreach (var val in data.ToArray().Where(val => val.Point > 0))
                Loot[returnTime].Add(new DetailedLoot(val));
        }

        public IEnumerable<DetailedLoot> LootForPoint(uint point)
        {
            return Loot.Values.SelectMany(val => val.Where(iVal => iVal.Point == point));
        }

        public IEnumerable<LootWithDate> LootForPointWithTime(uint point)
        {
            return Loot.SelectMany(kv => kv.Value.Where(iVal => iVal.Point == point).Select(loot => new LootWithDate(loot.Date, loot)));
        }
    }

    public record DetailedLoot(uint Point, uint Primary, ushort PrimaryCount, bool PrimaryHQ, uint Additional, ushort AdditionalCount, bool AdditionalHQ, DateTime Date)
    {
        [JsonConstructor]
        public DetailedLoot() : this(0, 0, 0, false, 0, 0, false, DateTime.MinValue) { }

        public DetailedLoot(DetailedLoot original, uint date) : this()
        {
            Point = original.Point;
            Primary = original.Primary;
            PrimaryCount = original.PrimaryCount;
            PrimaryHQ = original.PrimaryHQ;

            Additional = original.Additional;
            AdditionalCount = original.AdditionalCount;
            AdditionalHQ = original.AdditionalHQ;

            Date = original.Date == DateTime.MinValue
                       ? DateTime.UnixEpoch.AddSeconds(date).ToLocalTime()
                       : original.Date;
        }

        public DetailedLoot(HousingWorkshopSubmarineGathered data) : this()
        {
            Point = data.Point;
            Primary = data.ItemIdPrimary;
            PrimaryCount = data.ItemCountPrimary;
            PrimaryHQ = data.ItemHQPrimary;

            Additional = data.ItemIdAdditional;
            AdditionalCount = data.ItemCountAdditional;
            AdditionalHQ = data.ItemHQAdditional;

            Date = DateTime.Now;
        }

        [JsonIgnore] public Item PrimaryItem => ItemSheet.GetRow(Primary)!;
        [JsonIgnore] public Item AdditionalItem => ItemSheet.GetRow(Additional)!;
        [JsonIgnore] public bool ValidAdditional => Additional > 0;
    }

    public record Submarine(string Name, ushort Rank, ushort Hull, ushort Stern, ushort Bow, ushort Bridge, uint CExp, uint NExp)
    {
        public uint Register;
        public uint Return;
        public DateTime ReturnTime;
        public readonly List<uint> Points = new();

        public ushort HullDurability = 30000;
        public ushort SternDurability = 30000;
        public ushort BowDurability = 30000;
        public ushort BridgeDurability = 30000;

        [JsonConstructor]
        public Submarine() : this("", 0, 0, 0, 0, 0, 0, 0) { }

        public unsafe Submarine(HousingWorkshopSubmersibleSubData data, int idx) : this("", 0, 0, 0, 0, 0, 0, 0)
        {
            Name = MemoryHelper.ReadSeStringNullTerminated((nint)data.Name).ToString();
            Rank = data.RankId;
            Hull = data.HullId;
            Stern = data.SternId;
            Bow = data.BowId;
            Bridge = data.BridgeId;
            CExp = data.CurrentExp;
            NExp = data.NextLevelExp;

            Register = data.RegisterTime;
            Return = data.ReturnTime;
            ReturnTime = data.GetReturnTime();

            var managedArray = new byte[5];
            Marshal.Copy((nint)data.CurrentExplorationPoints, managedArray, 0, 5);

            foreach (var point in managedArray)
            {
                if (point > 0)
                    Points.Add(point);
            }

            try
            {
                var manager = InventoryManager.Instance();
                if (manager == null)
                    return;

                var offset = idx == 0 ? 0 : 5 * idx;

                HullDurability = manager->GetInventoryContainer(InventoryType.HousingInteriorPlacedItems2)->GetInventorySlot(0 + offset)->Condition;
                SternDurability = manager->GetInventoryContainer(InventoryType.HousingInteriorPlacedItems2)->GetInventorySlot(1 + offset)->Condition;
                BowDurability = manager->GetInventoryContainer(InventoryType.HousingInteriorPlacedItems2)->GetInventorySlot(2 + offset)->Condition;
                BridgeDurability = manager->GetInventoryContainer(InventoryType.HousingInteriorPlacedItems2)->GetInventorySlot(3 + offset)->Condition;
            }
            catch
            {
                PluginLog.Warning("Unable to read HousingInteriorPlacedItems2");
            }
        }

        private string GetPartName(ushort partId) => ItemSheet.GetRow(PartIdToItemId[partId])!.Name.ToString();
        private uint GetIconId(ushort partId) => ItemSheet.GetRow(PartIdToItemId[partId])!.Icon;

        #region parts
        [JsonIgnore] public string HullName => GetPartName(Hull);
        [JsonIgnore] public string SternName => GetPartName(Stern);
        [JsonIgnore] public string BowName => GetPartName(Bow);
        [JsonIgnore] public string BridgeName => GetPartName(Bridge);

        [JsonIgnore] public uint HullIconId => GetIconId(Hull);
        [JsonIgnore] public uint SternIconId => GetIconId(Stern);
        [JsonIgnore] public uint BowIconId => GetIconId(Bow);
        [JsonIgnore] public uint BridgeIconId => GetIconId(Bridge);

        [JsonIgnore] public double HullCondition => HullDurability / 300.0;
        [JsonIgnore] public double SternCondition => SternDurability / 300.0;
        [JsonIgnore] public double BowCondition => BowDurability / 300.0;
        [JsonIgnore] public double BridgeCondition => BridgeDurability / 300.0;

        [JsonIgnore] public bool NoRepairNeeded => HullDurability > 0 && SternDurability > 0 && BowDurability > 0 && BridgeDurability > 0;
        [JsonIgnore] public double LowestCondition => new[] { HullDurability, SternDurability, BowDurability, BridgeDurability }.Min() / 300.0;

        [JsonIgnore] public SubmarineBuild Build => new SubmarineBuild(this);

        public string BuildIdentifier()
        {
            var identifier = $"{ToIdentifier(Hull)}{ToIdentifier(Stern)}{ToIdentifier(Bow)}{ToIdentifier(Bridge)}";

            if (identifier.Count(l => l == '+') == 4)
                identifier = $"{identifier.Replace("+", "")}++";

            return identifier;
        }
        #endregion

        public bool IsValid() => Rank > 0;
        public bool ValidExpRange() => NExp > 0;
        public bool IsOnVoyage() => Points.Any();

        #region equals
        public bool VoyageEqual(List<uint> l, List<uint> r) => l.SequenceEqual(r);

        public virtual bool Equals(Submarine? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Name == other.Name && Rank == other.Rank && Hull == other.Hull &&
                   Stern == other.Stern && Bow == other.Bow && Bridge == other.Bridge &&
                   CExp == other.CExp && Return == other.Return && Register == other.Register &&
                   HullDurability == other.HullDurability && SternDurability == other.SternDurability  &&
                   BowDurability == other.BowDurability  && BridgeDurability == other.BridgeDurability &&
                   ReturnTime == other.ReturnTime && VoyageEqual(Points, other.Points);
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
                   x.Stern == y.Stern && x.Bow == y.Bow && x.Bridge == y.Bridge &&
                   x.CExp == y.CExp && x.Return == y.Return && x.Register == y.Register &&
                   x.HullDurability == y.HullDurability && x.SternDurability == y.SternDurability  &&
                   x.BowDurability == y.BowDurability  && x.BridgeDurability == y.BridgeDurability &&
                   x.ReturnTime == y.ReturnTime && VoyageEqual(x.Points, y.Points);
        }

        public override int GetHashCode() => HashCode.Combine(Name, Rank, Hull, Stern, Bow, Bridge, CExp, Points);
        #endregion
    }

    public readonly struct SubmarineBuild
    {
        private readonly SubmarineRank Bonus;
        private readonly SubmarinePart Hull;
        private readonly SubmarinePart Stern;
        private readonly SubmarinePart Bow;
        private readonly SubmarinePart Bridge;

        public SubmarineBuild(Submarine sub) : this(sub.Rank, sub.Hull, sub.Stern, sub.Bow, sub.Bridge) { }

        public SubmarineBuild(int rank, int hull, int stern, int bow, int bridge) : this()
        {
            Bonus = GetRank(rank);
            Hull = GetPart(hull);
            Stern = GetPart(stern);
            Bow = GetPart(bow);
            Bridge = GetPart(bridge);
        }

        public SubmarineBuild(RouteBuild build) : this()
        {
            Bonus = GetRank(build.Rank);
            Hull = GetPart(build.Hull);
            Stern = GetPart(build.Stern);
            Bow = GetPart(build.Bow);
            Bridge = GetPart(build.Bridge);
        }

        public SubmarineBuild(int rank, int rowCollection) : this()
        {
            Bonus = GetRank(rank);
            Hull = GetPart((rowCollection * 4) + 3);
            Stern = GetPart((rowCollection * 4) + 4);
            Bow = GetPart((rowCollection * 4) + 1);
            Bridge = GetPart((rowCollection * 4) + 2);
        }

        public int Surveillance => Bonus.SurveillanceBonus + Hull.Surveillance + Stern.Surveillance + Bow.Surveillance + Bridge.Surveillance;
        public int Retrieval => Bonus.RetrievalBonus + Hull.Retrieval + Stern.Retrieval + Bow.Retrieval + Bridge.Retrieval;
        public int Speed => Bonus.SpeedBonus + Hull.Speed + Stern.Speed + Bow.Speed + Bridge.Speed;
        public int Range => Bonus.RangeBonus + Hull.Range + Stern.Range + Bow.Range + Bridge.Range;
        public int Favor => Bonus.FavorBonus + Hull.Favor + Stern.Favor + Bow.Favor + Bridge.Favor;
        public int RepairCosts => Hull.RepairMaterials + Stern.RepairMaterials + Bow.RepairMaterials + Bridge.RepairMaterials;

        private SubmarineRank GetRank(int rank) => RankSheet.GetRow((uint)rank)!;
        private SubmarinePart GetPart(int partId) => PartSheet.GetRow((uint)partId)!;

        public string BuildIdentifier()
        {
            var identifier = $"{ToIdentifier((ushort) Hull.RowId)}{ToIdentifier((ushort) Stern.RowId)}{ToIdentifier((ushort) Bow.RowId)}{ToIdentifier((ushort) Bridge.RowId)}";

            if (identifier.Count(l => l == '+') == 4)
                identifier = $"{identifier.Replace("+", "")}++";

            return identifier;
        }

        public bool EqualsSubmarine(Submarine other)
        {
            return Bonus.RowId == other.Rank && Hull.RowId == other.Hull && Stern.RowId == other.Stern && Bow.RowId == other.Bow && Bridge.RowId == other.Bridge;
        }
    }

    public struct RouteBuild
    {
        public int OriginalSub = 0;

        public int Rank = 1;
        public int Hull = 3;
        public int Stern = 4;
        public int Bow = 1;
        public int Bridge = 2;

        public int Map = 0;
        public List<uint> Sectors = new();

        public RouteBuild() { }

        [JsonIgnore] public int OptimizedDistance = 0;
        [JsonIgnore] public List<SubmarineExplorationPretty> OptimizedRoute = new();

        [JsonIgnore] public SubmarineBuild GetSubmarineBuild => new(this);

        [JsonIgnore] public static RouteBuild Empty => new();

        public void UpdateBuild(Submarine sub)
        {
            Rank = sub.Rank;
            Hull = sub.Hull;
            Stern = sub.Stern;
            Bow = sub.Bow;
            Bridge = sub.Bridge;
        }

        public void ChangeMap(int newMap)
        {
            Map = newMap;

            Sectors.Clear();
            OptimizedDistance = 0;
            OptimizedRoute = new List<SubmarineExplorationPretty>();
        }

        public void UpdateOptimized((int Distance, List<SubmarineExplorationPretty> Points) optimized)
        {
            OptimizedDistance = optimized.Distance;
            OptimizedRoute = optimized.Points;
        }

        public void NoOptimized()
        {
            OptimizedDistance = 0;
            OptimizedRoute = new List<SubmarineExplorationPretty>();
        }
    }

    public static bool SubmarinesEqual(List<Submarine> l, List<Submarine> r)
    {
        if (!l.Any() || !r.Any())
            return false;

        if (l.Count != r.Count)
            return false;

        foreach (var (subL, subR) in l.Zip(r))
        {
            if (!subL.Equals(subR))
                return false;
        }

        return true;
    }

    public static uint FindVoyageStartPoint(uint point)
    {
        var startPoints = ExplorationSheet.Where(s => s.StartingPoint).Select(s => s.RowId).ToList();
        startPoints.Reverse();

        // This works because we reversed the list of start points
        foreach (var possibleStart in startPoints)
        {
            if (point > possibleStart)
                return possibleStart;
        }

        return 0;
    }

    public static readonly Dictionary<ulong, FcSubmarines> KnownSubmarines = new();

    public static readonly Dictionary<ushort, uint> PartIdToItemId = new Dictionary<ushort, uint>
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

    #region Character Handler
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

            // TODO Remove later
            // Migrate version 0 to version 1
            if (config.Version == 0)
            {
                foreach (var (key, subLoot) in config.Loot)
                {
                    foreach (var (keyLoot, valueLoot) in subLoot.Loot)
                    {
                        var newList = new List<DetailedLoot>();
                        foreach (var loot in valueLoot)
                        {
                            newList.Add(new DetailedLoot(loot, keyLoot));
                        }

                        subLoot.Loot[keyLoot] = newList;
                    }
                }

                config.Version = 1;
                config.Save();
            }

            KnownSubmarines.TryAdd(id, FcSubmarines.Empty);
            var playerFc = KnownSubmarines[id];

            if (SubmarinesEqual(playerFc.Submarines, config.Submarines))
                continue;

            KnownSubmarines[id] = new FcSubmarines(config.CharacterName, config.Tag, config.World, config.Submarines, config.Loot, config.ExplorationPoints);
        }
    }

    public static void SaveCharacter()
    {
        var id = Plugin.ClientState.LocalContentId;
        if (!KnownSubmarines.TryGetValue(id, out var playerFc))
            return;

        var points = playerFc.UnlockedSectors.Select(t => new Tuple<uint, bool, bool>(t.Key, t.Value, playerFc.ExploredSectors[t.Key])).ToList();

        var config = new CharacterConfiguration(id, playerFc.CharacterName, playerFc.Tag, playerFc.World, playerFc.Submarines, playerFc.SubLoot, points);
        config.Save();
    }

    public static void DeleteCharacter(ulong id)
    {
        if (!KnownSubmarines.ContainsKey(id))
            return;

        KnownSubmarines.Remove(id);
        var file = Plugin.PluginInterface.ConfigDirectory.EnumerateFiles().FirstOrDefault(f => f.Name == $"{id}.json");
        if (file == null)
            return;

        try
        {
            file.Delete();
        }
        catch (Exception e)
        {
            PluginLog.Error("Error while deleting character save file.");
            PluginLog.Error(e.Message);
        }
    }
    #endregion

    #region Optimizer
    public static uint CalculateDuration(IEnumerable<SubmarineExplorationPretty> walkingPoints, SubmarineBuild build)
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
            case 1: // 1 point makes no sense to optimize, so just return distance
                {
                    var onlyPoint = points[0];
                    return VoyageTime(start, onlyPoint, (short)build.Speed) + SurveyTime(onlyPoint, (short)build.Speed) + FixedVoyageTime;
                }
            case > 5: // More than 5 points isn't allowed ingame
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

        return (uint)allDurations.Sum() + FixedVoyageTime;
    }

    public static (int Distance, List<SubmarineExplorationPretty> Points) CalculateDistance(IEnumerable<SubmarineExplorationPretty> walkingPoints)
    {
        var walkWay = walkingPoints.ToArray();
        var start = walkWay.First();

        var points = new List<SubmarineExplorationPretty>();
        foreach (var p in walkWay.Skip(1))
            points.Add(p);


        // zero
        if (points.Count == 0)
            return (0, new List<SubmarineExplorationPretty>());

        // 1 point makes no sense to optimize, so just return distance
        if (points.Count == 1)
        {
            var onlyPoint = points[0];
            var distance = BestDistance(start, onlyPoint) + onlyPoint.SurveyDistance;
            return ((int)distance, new List<SubmarineExplorationPretty> { onlyPoint });
        }

        // More than 5 points isn't allowed ingame
        if (points.Count > 5)
            return (0, new List<SubmarineExplorationPretty>());

        List<(SubmarineExplorationPretty Key, uint Start, Dictionary<uint, uint> Distances)> AllDis = new();
        foreach (var (point, idx) in points.Select((val, i) => (val, i)))
        {
            AllDis.Add((point, BestDistance(start, point), new()));

            foreach (var iPoint in points)
            {
                if (point.RowId == iPoint.RowId)
                    continue;

                AllDis[idx].Distances.Add(iPoint.RowId, BestDistance(point, iPoint));
            }
        }

        List<(uint Way, List<SubmarineExplorationPretty> Points)> MinimalWays = new List<(uint Way, List<SubmarineExplorationPretty> Points)>();
        try
        {
            foreach (var (point, idx) in AllDis.Select((val, i) => (val, i)))
            {
                var otherPoints = AllDis.ToList();
                otherPoints.RemoveAt(idx);

                var others = new Dictionary<uint, Dictionary<uint, uint>>();
                foreach (var p in otherPoints)
                {
                    var listDis = new Dictionary<uint, uint>();
                    foreach (var dis in p.Distances)
                    {
                        listDis.Add(points.First(t => t.RowId == dis.Key).RowId, dis.Value);
                    }

                    others[p.Key.RowId] = listDis;
                }

                MinimalWays.Add(PathWalker(point, others, walkWay));
            }
        }
        catch (Exception e)
        {
            PluginLog.Error(e.Message);
            PluginLog.Error(e.StackTrace);
        }

        var min = MinimalWays.MinBy(m => m.Way);
        var surveyD = min.Points.Sum(d => d.SurveyDistance);
        return ((int)min.Way + surveyD, min.Points);
    }

    public static (uint Distance, List<SubmarineExplorationPretty> Points) PathWalker((SubmarineExplorationPretty Key, uint Start, Dictionary<uint, uint> Distances) point, Dictionary<uint, Dictionary<uint, uint>> otherPoints, SubmarineExplorationPretty[] allPoints)
    {
        List<(uint Distance, List<SubmarineExplorationPretty> Points)> possibleDistances = new();
        foreach (var pos1 in otherPoints)
        {
            if (point.Key.RowId == pos1.Key)
                continue;

            var startToFirst = point.Start + point.Distances[pos1.Key];

            if (otherPoints.Count == 1)
            {
                possibleDistances.Add((startToFirst, new List<SubmarineExplorationPretty> { point.Key, allPoints.First(t => t.RowId == pos1.Key), }));
                continue;
            }

            foreach (var pos2 in otherPoints)
            {
                if (pos1.Key == pos2.Key || point.Key.RowId == pos2.Key)
                    continue;

                var startToSecond = startToFirst + otherPoints[pos1.Key][pos2.Key];

                if (otherPoints.Count == 2)
                {
                    possibleDistances.Add((startToSecond, new List<SubmarineExplorationPretty> { point.Key, allPoints.First(t => t.RowId == pos1.Key), allPoints.First(t => t.RowId == pos2.Key), }));
                    continue;
                }

                foreach (var pos3 in otherPoints)
                {
                    if (pos1.Key == pos3.Key || pos2.Key == pos3.Key || point.Key.RowId == pos3.Key)
                        continue;

                    var startToThird = startToSecond + otherPoints[pos2.Key][pos3.Key];

                    if (otherPoints.Count == 3)
                    {
                        possibleDistances.Add((startToThird, new List<SubmarineExplorationPretty> { point.Key, allPoints.First(t => t.RowId == pos1.Key), allPoints.First(t => t.RowId == pos2.Key), allPoints.First(t => t.RowId == pos3.Key), }));
                        continue;
                    }

                    foreach (var pos4 in otherPoints)
                    {
                        if (pos1.Key == pos4.Key || pos2.Key == pos4.Key || pos3.Key == pos4.Key || point.Key.RowId == pos4.Key)
                            continue;

                        var startToLast = startToThird + otherPoints[pos3.Key][pos4.Key];

                        possibleDistances.Add((startToLast, new List<SubmarineExplorationPretty> { point.Key, allPoints.First(t => t.RowId == pos1.Key), allPoints.First(t => t.RowId == pos2.Key), allPoints.First(t => t.RowId == pos3.Key), allPoints.First(t => t.RowId == pos4.Key), }));
                    }
                }
            }
        }

        return possibleDistances.MinBy(a => a.Distance);
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
