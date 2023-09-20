using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace SubmarineTracker.Data;

public static class Loot
{
    private static ExcelSheet<Item> ItemSheet = null!;

    public static void Initialize()
    {
        ItemSheet = Plugin.Data.GetExcelSheet<Item>()!;
    }

    public record LootWithDate(DateTime Date, DetailedLoot Loot);

    public class SubmarineLoot
    {
        public Dictionary<uint, List<DetailedLoot>> Loot = new();

        [JsonConstructor]
        public SubmarineLoot() { }

        public void Snapshot(uint returnTime, Submarines.Submarine sub)
        {
            if (sub.Points.Count == 0)
                return;

            if (!Loot.TryAdd(returnTime, new List<DetailedLoot>()))
                return;

            foreach (var _ in sub.Points)
                Loot[returnTime].Add(new DetailedLoot(new Build.SubmarineBuild(sub)));
        }

        public void LootAdd(uint returnTime, Span<HousingWorkshopSubmarineGathered> data)
        {
            if (data[0].ItemIdPrimary == 0)
                return;

            if (!Loot.ContainsKey(returnTime))
                return;

            foreach (var (val, i) in data.ToArray().Where(val => val.Point > 0).Select((val, i) => (val, i)))
                Loot[returnTime][i].AddLoot(val);
        }

        public IEnumerable<DetailedLoot> LootForPoint(uint point, bool excludeLegacy)
        {
            return Loot.Values.SelectMany(val => val
                                                 .Where(iVal => iVal.Sector == point)
                                                 .Where(iVal => !excludeLegacy || iVal.Valid));
        }

        public IEnumerable<LootWithDate> LootForPointWithTime(uint point, bool excludeLegacy)
        {
            return Loot.SelectMany(kv => kv.Value
                                           .Where(iVal => iVal.Sector == point)
                                           .Where(iVal => !excludeLegacy || iVal.Valid)
                                           .Select(loot => new LootWithDate(loot.Date, loot)));
        }
    }

    public class DetailedLoot
    {
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

        public uint Primary;
        public ushort PrimaryCount;
        public bool PrimaryHQ;

        public uint Additional;
        public ushort AdditionalCount;
        public bool AdditionalHQ;
        public DateTime Date = DateTime.MinValue;

        [JsonConstructor]
        public DetailedLoot() { }

        public DetailedLoot(Build.SubmarineBuild build)
        {
            Rank = (int) build.Bonus.RowId;
            Surv = build.Surveillance;
            Ret = build.Retrieval;
            Fav = build.Favor;
        }

        public void AddLoot(HousingWorkshopSubmarineGathered data)
        {
            Valid = true;
            Date = DateTime.Now;

            Sector = data.Point;

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
        }

        [JsonIgnore] public Item PrimaryItem => ItemSheet.GetRow(Primary)!;
        [JsonIgnore] public Item AdditionalItem => ItemSheet.GetRow(Additional)!;
        [JsonIgnore] public bool ValidAdditional => Additional > 0;
    }

    public static string ProcToText(uint proc)
    {
        return proc switch
        {
            // Surveillance Procs
            4 => "T3 High",
            5 => "T2 High",
            6 => "T1 High",
            7 => "T2 Mid",
            8 => "T1 Mid",
            9 => "T1 Low",

            // Retrieval Procs
            14 => "Optimal",
            15 => "Normal",
            16 => "Poor",

            // Favor Procs
            18 => "Yes",
            19 => "CSCFIFFE",
            20 => "No",

            _ => "Unknown"
        };
    }
}
