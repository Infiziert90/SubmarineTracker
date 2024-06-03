using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace SubmarineTracker.Data;

public static class Loot
{
    private static readonly ExcelSheet<Item> ItemSheet;

    static Loot()
    {
        ItemSheet = Plugin.Data.GetExcelSheet<Item>()!;
    }

    public record LootWithDate(DateTime Date, DetailedLoot Loot);
    public class SubmarineLoot
    {
        public Dictionary<uint, List<DetailedLoot>> Loot = new();

        [JsonConstructor]
        public SubmarineLoot() { }

        public void AddLootEntry(uint returnTime, Build.SubmarineBuild build, Span<HousingWorkshopSubmarineGathered> data)
        {
            if (data[0].ItemIdPrimary == 0)
                return;

            var list = new List<DetailedLoot>();
            foreach (var val in data.ToArray().Where(val => val.Point > 0))
                list.Add(new DetailedLoot(build, val));

            Loot[returnTime] = list;
        }

        public IEnumerable<DetailedLoot> LootForPoint(uint point, bool excludeLegacy)
        {
            return Loot.Values.SelectMany(list => list.Where(loot => loot.Sector == point && (!excludeLegacy || loot.Valid)));
        }

        public IEnumerable<LootWithDate> LootForPointWithTime(uint point, bool excludeLegacy)
        {
            return LootForPoint(point, excludeLegacy).Select(loot => new LootWithDate(loot.Date, loot));
        }
    }

    public class DetailedLoot : ICloneable
    {
        public ulong FreeCompanyId = 0;
        public uint Register = 0;
        public uint Return = 0;

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
        public uint Unlocked = 0;

        public uint Primary;
        public ushort PrimaryCount;
        public bool PrimaryHQ;

        public uint Additional;
        public ushort AdditionalCount;
        public bool AdditionalHQ;
        public DateTime Date = DateTime.MinValue;

        [JsonConstructor]
        public DetailedLoot() { }

        public DetailedLoot(Build.SubmarineBuild build, HousingWorkshopSubmarineGathered data)
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

        [JsonIgnore] public Item PrimaryItem => ItemSheet.GetRow(Primary)!;
        [JsonIgnore] public Item AdditionalItem => ItemSheet.GetRow(Additional)!;
        [JsonIgnore] public bool ValidAdditional => Additional > 0;


        public object Clone()
        {
            return this.MemberwiseClone();
        }
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
            20 => "Too Low",

            _ => "Unknown"
        };
    }
}
