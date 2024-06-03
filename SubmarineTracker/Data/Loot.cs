using Newtonsoft.Json;

namespace SubmarineTracker.Data;

public static class Loot
{
    public class SubmarineLoot
    {
        public Dictionary<uint, List<DetailedLoot>> Loot = new();

        [JsonConstructor]
        public SubmarineLoot() { }
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
