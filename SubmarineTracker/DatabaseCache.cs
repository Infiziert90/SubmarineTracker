using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;

namespace SubmarineTracker;

public class DatabaseCache : IDisposable
{
    private const long ShortDelay = 5_000; // 5s;
    private const long LongDelay = 30_000; // 30s;

    public readonly Database Database = new();

    private Loot[] Loot = [];
    private Submarine[] Submarines = [];
    private Dictionary<ulong, FreeCompany> FreeCompanies = [];

    // Build from data
    private Dictionary<ulong, Dictionary<uint, Dictionary<Item, int>>> AllLoot = new();
    private Dictionary<ulong, Dictionary<DateTime, Dictionary<Item, int>>> TimeLoot = new();

    private long FCRefresh;
    private long SubRefresh;
    private long LootRefresh;

    public DatabaseCache()
    {
        RefreshLoot();
        RefreshSubmarines();
        RefreshFreeCompanies();

        FCRefresh = Environment.TickCount64;
        SubRefresh = Environment.TickCount64;
        LootRefresh = Environment.TickCount64;
    }

    public void Dispose()
    {
        Database.Dispose();
        GC.SuppressFinalize(this);
    }

    public Loot[] GetLoot()
    {
        CheckLoot();

        return Loot;
    }

    public Dictionary<uint, Dictionary<Item, int>> GetFCAllLoot(ulong id)
    {
        CheckLoot();

        AllLoot.TryGetValue(id, out var dict);
        return dict ?? new();
    }

    public Dictionary<DateTime, Dictionary<Item, int>> GetFCTimeLoot(ulong id)
    {
        CheckLoot();

        TimeLoot.TryGetValue(id, out var dict);
        return dict ?? new();
    }

    public Submarine[] GetSubmarines()
    {
        CheckSubmarines();

        return Submarines;
    }

    public Submarine[] GetSubmarines(ulong id)
    {
        return GetSubmarines().Where(s => s.FreeCompanyId == id).ToArray();
    }

    public Dictionary<ulong, FreeCompany> GetFreeCompanies()
    {
        CheckFreeCompany();

        return FreeCompanies;
    }

    private void CheckLoot()
    {
        if (LootRefresh < Environment.TickCount64)
        {
            LootRefresh = Environment.TickCount64 + LongDelay;
            Task.Run(RefreshLoot);
        }
    }

    private void RefreshLoot()
    {
        try
        {
            var result = Database.GetLoot().ToArray();

            var allDict = new Dictionary<ulong, Dictionary<uint, Dictionary<Item, int>>>();
            foreach (var point in Sheets.PossiblePoints)
            {
                foreach (var loot in result.Where(loot => loot.Sector == point.RowId && (!Plugin.Configuration.ExcludeLegacy || loot.Valid)))
                {
                    var fc = allDict.GetOrCreate(loot.FreeCompanyId);

                    var lootList = fc.GetOrCreate(point.RowId);
                    if (!lootList.TryAdd(loot.PrimaryItem, loot.PrimaryCount))
                        lootList[loot.PrimaryItem] += loot.PrimaryCount;

                    if (!loot.ValidAdditional)
                        continue;

                    if (!lootList.TryAdd(loot.AdditionalItem, loot.AdditionalCount))
                        lootList[loot.AdditionalItem] += loot.AdditionalCount;
                }
            }

            var timeDict = new Dictionary<ulong, Dictionary<DateTime, Dictionary<Item, int>>>();
            foreach (var point in Sheets.PossiblePoints)
            {
                foreach (var (date, loot) in result.Where(loot => loot.Sector == point.RowId && (!Plugin.Configuration.ExcludeLegacy || loot.Valid)).Select(loot => new LootWithDate(loot.Date, loot)))
                {
                    var fc = timeDict.GetOrCreate(loot.FreeCompanyId);

                    var lootList = fc.GetOrCreate(date);
                    if (!lootList.TryAdd(loot.PrimaryItem, loot.PrimaryCount))
                        lootList[loot.PrimaryItem] += loot.PrimaryCount;

                    if (!loot.ValidAdditional)
                        continue;

                    if (!lootList.TryAdd(loot.AdditionalItem, loot.AdditionalCount))
                        lootList[loot.AdditionalItem] += loot.AdditionalCount;
                }
            }

            Thread.MemoryBarrier();
            Loot = result;
            AllLoot = allDict;
            TimeLoot = timeDict;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Unable to refresh loot data");
        }
    }

    private void CheckSubmarines()
    {
        if (SubRefresh < Environment.TickCount64)
        {
            SubRefresh = Environment.TickCount64 + ShortDelay;
            Task.Run(RefreshSubmarines);
        }
    }

    private void RefreshSubmarines()
    {
        try
        {
            var result = Database.GetSubmarines().ToArray();

            Thread.MemoryBarrier();
            Submarines = result;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Unable to refresh submarine data");
        }
    }

    private void CheckFreeCompany()
    {
        if (FCRefresh < Environment.TickCount64)
        {
            FCRefresh = Environment.TickCount64 + ShortDelay;
            Task.Run(RefreshFreeCompanies);
        }
    }

    private void RefreshFreeCompanies()
    {
        try
        {
            var result = Database.GetFreeCompanies().ToDictionary(f => f.FreeCompanyId, f => f);

            Thread.MemoryBarrier();
            FreeCompanies = result;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Unable to refresh freecompany data");
        }
    }
}

public record LootWithDate(DateTime Date, Loot Loot);

public record FreeCompany
{
    public ulong FreeCompanyId;
    public string Tag = "";
    public string World = "";
    public string CharacterName = "";

    public Dictionary<uint, bool> UnlockedSectors = new();
    public Dictionary<uint, bool> ExploredSectors = new();

    public FreeCompany() {}

    // TODO Remove after migration time
    public FreeCompany(ulong id, Submarines.FcSubmarines fc)
    {
        FreeCompanyId = id;

        Tag = fc.Tag;
        World = fc.World;
        CharacterName = fc.CharacterName;

        UnlockedSectors = fc.UnlockedSectors;
        ExploredSectors = fc.ExploredSectors;
    }
};

public record Submarine
{
    public ulong FreeCompanyId;

    public string Name = "";
    public ushort Rank;

    public ushort Hull;
    public ushort Stern;
    public ushort Bow;
    public ushort Bridge;

    public uint CExp;
    public uint NExp;

    public uint Register;
    public uint Return;
    public DateTime ReturnTime = DateTime.UnixEpoch;
    public List<uint> Points = [];

    public ushort HullDurability = 30000;
    public ushort SternDurability = 30000;
    public ushort BowDurability = 30000;
    public ushort BridgeDurability = 30000;

    public Submarine() {}

    // TODO Remove after migration time
    public Submarine(ulong fcId, Submarines.Submarine sub)
    {
        FreeCompanyId = fcId;

        Name = sub.Name;
        Rank = sub.Rank;

        Hull = sub.Hull;
        Stern = sub.Stern;
        Bow = sub.Bow;
        Bridge = sub.Bridge;

        CExp = sub.CExp;
        NExp = sub.NExp;

        Register = sub.Register;
        Return = sub.Return;

        Points = sub.Points;

        HullDurability = sub.HullDurability;
        SternDurability = sub.SternDurability;
        BowDurability = sub.BowDurability;
        BridgeDurability = sub.BridgeDurability;
    }

    public Submarine(uint returnTime)
    {
        Return = returnTime;
    }

    public Submarine(HousingWorkshopSubmersibleSubData data)
    {
        Rank = data.RankId;
        Hull = data.HullId;
        Stern = data.SternId;
        Bow = data.BowId;
        Bridge = data.BridgeId;

        Register = data.RegisterTime;
        Return = data.ReturnTime;
    }

    public unsafe Submarine(HousingWorkshopSubmersibleSubData data, int idx)
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
            Plugin.Log.Warning("Unable to read submarine conditions");
        }
    }

    public string Identifier() => Build.FullIdentifier();
    private string GetPartName(ushort partId) => Sheets.ItemSheet.GetRow(Submarines.PartIdToItemId[partId])!.Name.ToString();
    private uint GetIconId(ushort partId) => Sheets.ItemSheet.GetRow(Submarines.PartIdToItemId[partId])!.Icon;

    #region parts
    public string HullName => GetPartName(Hull);
    public string SternName => GetPartName(Stern);
    public string BowName => GetPartName(Bow);
    public string BridgeName => GetPartName(Bridge);

    public uint HullIconId => GetIconId(Hull);
    public uint SternIconId => GetIconId(Stern);
    public uint BowIconId => GetIconId(Bow);
    public uint BridgeIconId => GetIconId(Bridge);

    public double HullCondition => HullDurability / 300.0;
    public double SternCondition => SternDurability / 300.0;
    public double BowCondition => BowDurability / 300.0;
    public double BridgeCondition => BridgeDurability / 300.0;

    public bool NoRepairNeeded => HullDurability > 0 && SternDurability > 0 && BowDurability > 0 && BridgeDurability > 0;
    public IEnumerable<(ushort Part, ushort Condition)> PartConditions => new[] { (Hull, HullDurability), (Stern, SternDurability), (Bow, BowDurability), (Bridge, BridgeDurability) };

    public double LowestCondition()
    {
        var lowest = PredictDurability();
        return lowest > 0 ? lowest / 300.0 : 0;
    }

    public Build.SubmarineBuild Build => new(this);

    // Credits: https://docs.google.com/spreadsheets/d/e/2PACX-1vTy99IDOlZ48efiFunLGMtZ-_fcfy4Z0Y_GqnL_1dvL7PmH0u7N_op5dysh0U4bVhKaLMHGuGlBf8zq/pubhtml#
    public int PredictDurability()
    {
        var lowest = 30000;
        foreach (var (part, durability) in PartConditions)
        {
            int damaged = durability;
            foreach (var sector in Points)
                damaged -= (335 + Sheets.ExplorationSheet.GetRow(sector)!.RankReq - Sheets.PartSheet.GetRow(part)!.Rank) * 7;

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
                damaged += (335 + Sheets.ExplorationSheet.GetRow(sector)!.RankReq - Sheets.PartSheet.GetRow(part)!.Rank) * 7;

            if (highestDamage < damaged)
                highestDamage = damaged;
        }

        return highestDamage;
    }

    public (uint Rank, double Exp) PredictExpGrowth()
    {
        var currentRank = Sheets.RankSheet.GetRow(Rank)!;
        var leftover = CExp + Sectors.CalculateExpForSectors(Sheets.ToExplorationArray(Points), Build);

        // This happens whenever the user has a new sub with no voyage
        if (leftover == 0)
            return (Rank, 0.0);

        while (leftover > 0)
        {
            if (currentRank.RowId == Sheets.LastRank)
                break;

            if (leftover > currentRank.ExpToNext)
            {
                leftover -= currentRank.ExpToNext;
                currentRank = Sheets.RankSheet.GetRow(currentRank.RowId + 1)!;
            }
            else
            {
                break;
            }
        }

        return currentRank.RowId < Sheets.LastRank
                   ? (currentRank.RowId, (double) leftover / currentRank.ExpToNext * 100.0)
                   : (Sheets.LastRank, 100.0);
    }
    #endregion

    public bool IsValid() => Rank > 0;
    public bool ValidExpRange() => NExp > 0;
    public bool IsOnVoyage() => Points.Count != 0;
    public bool IsDone() => LeftoverTime().TotalSeconds < 0;
    public TimeSpan LeftoverTime() => ReturnTime - DateTime.Now.ToUniversalTime();

    #region equals
    public bool VoyageEqual(List<uint> l, List<uint> r) => l.SequenceEqual(r);

    #pragma warning disable CS8851 // Doesn't need GetHashCode
    public virtual bool Equals(Submarine? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Return == other.Return && Register == other.Register && Name == other.Name &&
               Rank == other.Rank && Hull == other.Hull && Stern == other.Stern && Bow == other.Bow &&
               Bridge == other.Bridge && CExp == other.CExp && HullDurability == other.HullDurability &&
               SternDurability == other.SternDurability && BowDurability == other.BowDurability &&
               BridgeDurability == other.BridgeDurability && VoyageEqual(Points, other.Points);
    }
    #pragma warning restore CS8851
    #endregion
}

public record Loot
{
    public ulong FreeCompanyId;
    public uint Register;
    public uint Return;

    public bool Valid;
    public int Rank;
    public int Surv;
    public int Ret;
    public int Fav;

    public uint PrimarySurvProc;
    public uint AdditionalSurvProc;
    public uint PrimaryRetProc;
    public uint AdditionalRetProc;
    public uint FavProc;

    public uint Sector;
    public uint Unlocked;

    public uint Primary;
    public ushort PrimaryCount;
    public bool PrimaryHQ;

    public uint Additional;
    public ushort AdditionalCount;
    public bool AdditionalHQ;
    public DateTime Date = DateTime.MinValue;

    public Loot() {}

    // TODO Remove after migration time
    public Loot(ulong fcId, uint register, uint returnTime, Data.Loot.DetailedLoot loot)
    {
        FreeCompanyId = fcId;
        Register = register;
        Return = returnTime;

        Valid = loot.Valid;
        Rank = loot.Rank;
        Surv = loot.Surv;
        Ret = loot.Ret;
        Fav = loot.Fav;

        PrimarySurvProc = loot.PrimarySurvProc;
        AdditionalSurvProc = loot.AdditionalSurvProc;
        PrimaryRetProc = loot.PrimaryRetProc;
        AdditionalRetProc = loot.AdditionalRetProc;
        FavProc = loot.FavProc;

        Sector = loot.Sector;
        Unlocked = loot.Unlocked;

        Primary = loot.Primary;
        PrimaryCount = loot.PrimaryCount;
        PrimaryHQ = loot.PrimaryHQ;

        Additional = loot.Additional;
        AdditionalCount = loot.AdditionalCount;
        AdditionalHQ = loot.AdditionalHQ;

        Date = loot.Date;
    }

    public Loot(Build.SubmarineBuild build, HousingWorkshopSubmarineGathered data)
    {
        Valid = true;

        Rank = (int) build.Bonus.RowId;
        Surv = build.Surveillance;
        Ret = build.Retrieval;
        Fav = build.Favor;

        Sector = data.Point;
        Unlocked = data.UnlockedPoint;

        Primary = data.ItemIdPrimary;
        PrimaryCount = data.ItemCountPrimary;
        PrimaryHQ = data.ItemHQPrimary;
        PrimarySurvProc = data.SurveyLinePrimary;
        PrimaryRetProc = data.YieldLinePrimary;

        Additional = data.ItemIdAdditional;
        AdditionalCount = data.ItemCountAdditional;
        AdditionalHQ = data.ItemHQAdditional;
        AdditionalSurvProc = data.SurveyLineAdditional;
        AdditionalRetProc = data.YieldLineAdditional;
        FavProc = data.FavorLine;

        Date = DateTime.Now;

        Plugin.EntryUpload(this);
    }

    public Item PrimaryItem => Sheets.ItemSheet.GetRow(Primary)!;
    public Item AdditionalItem => Sheets.ItemSheet.GetRow(Additional)!;
    public bool ValidAdditional => Additional > 0;
}
