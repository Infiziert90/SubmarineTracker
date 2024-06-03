using Newtonsoft.Json;

namespace SubmarineTracker.Data;

public static class Submarines
{
    // TODO Remove after migration time
    public class FcSubmarines
    {
        public ulong FreeCompanyId = 0;
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
    }

    // TODO Remove after migration time
    public record Submarine(string Name, ushort Rank, ushort Hull, ushort Stern, ushort Bow, ushort Bridge, uint CExp, uint NExp)
    {
        public ulong FreeCompanyId = 0;

        public uint Register;
        public uint Return;
        public List<uint> Points = new();

        public ushort HullDurability = 30000;
        public ushort SternDurability = 30000;
        public ushort BowDurability = 30000;
        public ushort BridgeDurability = 30000;

        [JsonConstructor]
        public Submarine() : this("", 0, 0, 0, 0, 0, 0, 0) { }
    }

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
}
