// ReSharper disable FieldCanBeMadeReadOnly.Global
// MessagePack can't deserialize into readonly

using System.Collections.Frozen;
using System.IO;
using MessagePack;
using Newtonsoft.Json;

namespace SubmarineTracker.Data;

[MessagePackObject]
public class CalculatedData
{
    [Key(0)] public uint MaxSector;
    [Key(1)] public Dictionary<int, Route[]> Maps = [];
}

[MessagePackObject]
public struct Route
{
    [Key(0)] public uint Distance;
    [Key(1)] public uint[] Sectors;
}

[MessagePackObject]
public class ItemDetailed
{
    [Key(0)] public Dictionary<uint, List<ItemDetail>> Items = new();
}

[MessagePackObject]
public struct ItemDetail
{
    [Key(0)] public uint Sector;
    [Key(1)] public string Tier;
    [Key(2)] public string Poor;
    [Key(3)] public string Normal;
    [Key(4)] public string Optimal;
}

public static class Importer
{
    public const string Filename = "CalculatedData.msgpack";
    public const string FilenameItem = "ItemDetailed.msgpack";

    public static ItemDetailed ItemDetailed = new();
    public static CalculatedData CalculatedData = new();
    public static Dictionary<int, FrozenDictionary<int, Route>> HashedRoutes = new();

    static Importer()
    {
        Load();

        HashedRoutes.Clear();
        foreach (var (map, routes) in CalculatedData.Maps)
        {
            var dict = new Dictionary<int, Route>();
            foreach (var route in routes)
                dict.Add(Utils.GetUniqueHash(route.Sectors), route);

            HashedRoutes.Add(map, dict.ToFrozenDictionary());
        }
    }

    private static void Load()
    {
        try
        {
            using var calculatedStream = File.OpenRead(Path.Combine(Plugin.PluginDir, Filename));
            CalculatedData = MessagePackSerializer.Deserialize<CalculatedData>(calculatedStream);

            using var itemStream = File.OpenRead(Path.Combine(Plugin.PluginDir, FilenameItem));
            ItemDetailed = MessagePackSerializer.Deserialize<ItemDetailed>(itemStream);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed loading message pack data.");

            ItemDetailed = new ItemDetailed();
            CalculatedData = new CalculatedData();
        }
    }

    #region MessagePackCreation
    #if DEBUG
    public static void Export()
    {
        try
        {
            Plugin.Log.Information("Start route build");
            var dict = new Dictionary<int, Route[]>();
            foreach (var mapId in Sheets.MapSheet.Where(m => m.RowId != 0).Select(m => m.RowId))
                dict.Add((int) mapId, Voyage.FindAllRoutes(mapId));

            CalculatedData = new CalculatedData {Maps = dict};

            var path = Path.Combine(Plugin.PluginDir, Filename);
            if (File.Exists(path))
                File.Delete(path);

            File.WriteAllBytes(path, MessagePackSerializer.Serialize(CalculatedData));
            Plugin.Log.Information("Finished route build");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Can't build routes.");
        }
    }

    [Serializable]
    // From: https://github.com/Infiziert90/FFXIVGachaSpreadsheet/blob/master/Export/SupabaseExporter/SupabaseExporter/Structures/Exports/SubLoot.cs
    public class SubLoot
    {
        // Used for internal cache keeping
        public uint ProcessedId;

        public int Total;
        public Dictionary<uint, Sector> Sectors = [];

        public SubLoot() { }

        public class Sector
        {
            public int Records;

            public uint Id;
            public string Name;
            public string Letter;
            public uint Rank;
            public uint Stars;
            public uint UnlockedFrom;

            public Dictionary<SurvTier, LootPool> Pools = [];

            public Sector() { }
        }

        public class LootPool
        {
            public int Records;

            public Dictionary<uint, PoolReward> Rewards = [];

            public LootPool() { }
        }

        public record PoolReward
        {
            public uint Id;
            public long Amount;
            public long Total;

            public Dictionary<RetTier, int[]> MinMax = [];

            public PoolReward() { }
        }
    }

    public enum SurvTier : uint
    {
        Invalid = 0,

        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3,
    }

    public static string ToTier(this SurvTier tier) => (tier) switch
        {
            SurvTier.Tier1 => "T1",
            SurvTier.Tier2 => "T2",
            SurvTier.Tier3 => "T3",
            _ => "Invalid"
        };

    public enum RetTier : uint
    {
        Invalid = 0,

        Poor = 1,
        Normal = 2,
        Optimal = 3,
    }

    public static void ExportDetailed(string itemPath)
    {
        Plugin.Log.Information("Start item build");
        ItemDetailed.Items.Clear();

        using var reader = new FileInfo(itemPath).OpenText();
        var data = JsonConvert.DeserializeObject<SubLoot>(reader.ReadToEnd());
        if (data == null)
        {
            Plugin.Log.Error("Item detailed import failed. Invalid json.");
            return;
        }

        foreach (var sectorData in data.Sectors.Select(pair => pair.Value))
        {
            if (!Sheets.ExplorationSheet.HasRow(sectorData.Id))
            {
                Plugin.Log.Warning($"Invalid sector id found in data: {sectorData.Id}");
                continue;
            }

            foreach (var (tier, pool) in sectorData.Pools)
            {
                foreach (var (itemId, reward) in pool.Rewards)
                {
                    if (!Sheets.ItemSheet.HasRow(itemId))
                    {
                        Plugin.Log.Warning($"Invalid item id found in data: {itemId}");
                        continue;
                    }

                    var detail = new ItemDetail
                    {
                        Sector = sectorData.Id,
                        Tier = tier.ToTier(),
                        Poor = $"{reward.MinMax[RetTier.Poor][0]} - {reward.MinMax[RetTier.Poor][1]}",
                        Normal = $"{reward.MinMax[RetTier.Normal][0]} - {reward.MinMax[RetTier.Normal][1]}",
                        Optimal = $"{reward.MinMax[RetTier.Optimal][0]} - {reward.MinMax[RetTier.Optimal][1]}"
                    };

                    if (!ItemDetailed.Items.TryAdd(itemId, [detail]))
                        ItemDetailed.Items[itemId].Add(detail);
                }
            }
        }

        var path = Path.Combine(Plugin.PluginDir, FilenameItem);
        if (File.Exists(path))
            File.Delete(path);

        File.WriteAllBytes(path, MessagePackSerializer.Serialize(ItemDetailed));
        Plugin.Log.Information("Finished item build");
    }
    #endif
    #endregion
}
