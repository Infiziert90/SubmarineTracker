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
    private static ExcelSheet<SubmarineExplorationPretty> ExplorationSheet = null!;

    private static uint LastRank = 0;
    private static List<SubmarineExplorationPretty> PossiblePoints = new();

    public static void Initialize()
    {
        ItemSheet = Plugin.Data.GetExcelSheet<Item>()!;
        RankSheet = Plugin.Data.GetExcelSheet<SubmarineRank>()!;
        PartSheet = Plugin.Data.GetExcelSheet<SubmarinePart>()!;
        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExplorationPretty>()!;

        PossiblePoints = ExplorationSheet.Where(r => r.ExpReward > 0).ToList();
        LastRank = RankSheet.Last(t => t.Capacity != 0).RowId;
    }

    public class FcSubmarines
    {
        public string CharacterName = "";
        public string Tag = "";
        public string World = "";
        public List<Submarine> Submarines = new();

        public Dictionary<uint, Loot.SubmarineLoot> SubLoot = new();
        public Dictionary<uint, bool> UnlockedSectors = new();
        public Dictionary<uint, bool> ExploredSectors = new();

        [JsonConstructor]
        public FcSubmarines() { }

        public FcSubmarines(CharacterConfiguration config)
        {
            CharacterName = config.CharacterName;
            Tag = config.Tag;
            World = config.World;
            Submarines = config.Submarines;
            SubLoot = config.Loot;
            foreach (var (point, unlocked, explored) in config.ExplorationPoints)
            {
                UnlockedSectors[point] = unlocked;
                ExploredSectors[point] = explored;
            }
        }


        public FcSubmarines(string characterName, string tag, string world, List<Submarine> submarines, Dictionary<uint, Loot.SubmarineLoot> loot, List<Tuple<uint, bool, bool>> points)
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

        public static FcSubmarines Empty => new("", "", "Unknown", new List<Submarine>(), new Dictionary<uint, Loot.SubmarineLoot>(), new List<Tuple<uint, bool, bool>>());

        public void AddSubLoot(uint key, uint returnTime, Span<HousingWorkshopSubmarineGathered> data)
        {
            SubLoot.TryAdd(key, new Loot.SubmarineLoot());

            var subLoot = SubLoot[key];
            var sub = Submarines.Find(s => s.Register == key)!;

            // add last voyage loot and procs
            if (subLoot.Loot.Any())
            {
                var lastVoyage = subLoot.Loot.Last();

                // prevent the current snapshot from being overwritten
                if (lastVoyage.Key == sub.Return)
                    return;

                if (lastVoyage.Value.First().Sector == 0)
                    subLoot.LootAdd(lastVoyage.Key, data);
            }

            // add snapshot of current submarine stats
            subLoot.Snapshot(returnTime, sub);
        }

        public void GetUnlockedAndExploredSectors()
        {
            foreach (var submarineExploration in ExplorationSheet)
            {
                UnlockedSectors[submarineExploration.RowId] = HousingManager.IsSubmarineExplorationUnlocked((byte)submarineExploration.RowId);
                ExploredSectors[submarineExploration.RowId] = HousingManager.IsSubmarineExplorationExplored((byte)submarineExploration.RowId);
            }
        }

        public uint[] ReturnTimes() => Submarines.Select(sub => sub.Return).ToArray();
        public Submarine? GetLastReturn() => Submarines.MaxBy(sub => sub.Return);
        public Submarine? GetFirstReturn() => Submarines.MinBy(sub => sub.Return);
        public bool AnySubDone() => Submarines.Any(sub => sub.IsDone());

        #region Loot
        [JsonIgnore] public bool Refresh = true;
        [JsonIgnore] public Dictionary<uint, Dictionary<Item, int>> AllLoot = new();
        [JsonIgnore] public Dictionary<DateTime, Dictionary<Item, int>> TimeLoot = new();

        public void RebuildStats(bool excludeLegacy = false)
        {
            if (Refresh)
                Refresh = false;
            else
                return;

            AllLoot.Clear();
            foreach (var point in PossiblePoints)
            {
                var possibleLoot = SubLoot.Values.SelectMany(val => val.LootForPoint(point.RowId, excludeLegacy)).ToList();
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
                var possibleLoot = SubLoot.Values.SelectMany(val => val.LootForPointWithTime(point.RowId, excludeLegacy)).ToList();
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

    public record Submarine(string Name, ushort Rank, ushort Hull, ushort Stern, ushort Bow, ushort Bridge, uint CExp, uint NExp)
    {
        public uint Register;
        public uint Return;
        public DateTime ReturnTime = DateTime.UnixEpoch;
        public readonly List<uint> Points = new();

        public ushort HullDurability = 30000;
        public ushort SternDurability = 30000;
        public ushort BowDurability = 30000;
        public ushort BridgeDurability = 30000;

        [JsonConstructor]
        public Submarine() : this("", 0, 0, 0, 0, 0, 0, 0) { }

        public unsafe Submarine(HousingWorkshopSubmersibleSubData data, int idx) : this("", 0, 0, 0, 0, 0, 0, 0)
        {
            Name = MemoryHelper.ReadSeStringNullTerminated((nint) data.Name).ToString();
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
        [JsonIgnore] public IEnumerable<(ushort Part, ushort Condition)> PartConditions => new[] { (Hull, HullDurability), (Stern, SternDurability), (Bow, BowDurability), (Bridge, BridgeDurability) };

        public double LowestCondition()
        {
            var lowest = PredictDurability();
            return lowest > 0 ? lowest / 300.0 : 0;
        }

        [JsonIgnore] public Build.SubmarineBuild Build => new(this);

        // Credits for the formula
        // https://docs.google.com/spreadsheets/d/e/2PACX-1vTy99IDOlZ48efiFunLGMtZ-_fcfy4Z0Y_GqnL_1dvL7PmH0u7N_op5dysh0U4bVhKaLMHGuGlBf8zq/pubhtml#
        public int PredictDurability()
        {
            var lowest = 30000;
            foreach (var (part, durability) in PartConditions)
            {
                int damaged = durability;
                foreach (var sector in Points)
                    damaged -= (335 + ExplorationSheet.GetRow(sector)!.RankReq - PartSheet.GetRow(part)!.Rank) * 7;

                if (lowest > damaged)
                    lowest = damaged;
            }

            return lowest;
        }

        public int CalculateUntilRepair()
        {
            var dmg = VoyageDamage();
            if (dmg == 1)
                return -1;

            var voyages = 0;
            var health = 30000;
            while (health > 0)
            {
                voyages += 1;
                health -= dmg;
            }

            return voyages;
        }

        public int VoyageDamage()
        {
            var highestDamage = 1;
            foreach (var (part, _) in PartConditions)
            {
                var damaged = 0;
                foreach (var sector in Points)
                    damaged += (335 + ExplorationSheet.GetRow(sector)!.RankReq - PartSheet.GetRow(part)!.Rank) * 7;

                if (highestDamage < damaged)
                    highestDamage = damaged;
            }

            return highestDamage;
        }

        public (uint Rank, double Exp) PredictExpGrowth()
        {
            var currentRank = RankSheet.GetRow(Rank)!;
            var leftover = CExp + Sectors.CalculateExpForSectors(ToSheetArray(Points), Build);

            // This happens whenever the user has a new sub with no voyage
            if (leftover == 0)
                return (Rank, 0.0);

            while (leftover > 0)
            {
                if (currentRank.RowId == LastRank)
                    break;

                if (leftover > currentRank.ExpToNext)
                {
                    leftover -= currentRank.ExpToNext;
                    currentRank = RankSheet.GetRow(currentRank.RowId + 1)!;
                }
                else
                {
                    break;
                }
            }

            return currentRank.RowId < LastRank
                       ? (currentRank.RowId, (double) leftover / currentRank.ExpToNext * 100.0)
                       : (LastRank, 100.0);
        }
        #endregion

        public bool IsValid() => Rank > 0;
        public bool ValidExpRange() => NExp > 0;
        public bool IsOnVoyage() => Points.Any();
        public bool IsDone() => LeftoverTime().TotalSeconds < 0;
        public TimeSpan LeftoverTime() => ReturnTime - DateTime.Now.ToUniversalTime();

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
                   HullDurability == other.HullDurability && SternDurability == other.SternDurability &&
                   BowDurability == other.BowDurability && BridgeDurability == other.BridgeDurability &&
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
                   x.HullDurability == y.HullDurability && x.SternDurability == y.SternDurability &&
                   x.BowDurability == y.BowDurability && x.BridgeDurability == y.BridgeDurability &&
                   x.ReturnTime == y.ReturnTime && VoyageEqual(x.Points, y.Points);
        }

        public override int GetHashCode() => HashCode.Combine(Name, Rank, Hull, Stern, Bow, Bridge, CExp, Points);
        #endregion
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

    public static readonly Dictionary<ulong, FcSubmarines> KnownSubmarines = new();

    public static readonly Dictionary<ushort, uint> PartIdToItemId = new()
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

    public static SubmarineExplorationPretty[] ToSheetArray(IEnumerable<uint> sectors) => sectors.Select(s => ExplorationSheet.GetRow(s)!).ToArray();
}
