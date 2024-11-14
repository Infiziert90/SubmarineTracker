// ReSharper disable FieldCanBeMadeReadOnly.Global
// MessagePack can't deserialize into readonly

using System.Collections.Frozen;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Lumina.Excel.Sheets;
using MessagePack;

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
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Failed loading message pack data.");

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
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Can't build routes.");
        }
    }

    // ReSharper disable once UnusedType.Global
    public class SectorCSV
    {
        [Name("Sector name")] public uint Sector { get; set; }
        [Name("T1 high surv proc")] public uint T1HighSurv { get; set; }
        [Name("T2 high surv proc")] public string T2HighSurv { get; set; }
        [Name("T3 high surv proc")] public string T3HighSurv { get; set; }
        [Name("Favor high surv proc")] public string HighFavor { get; set; }
        [Name("Favor proc chance")] public string MidFavor { get; set; }
        [Name("T1 mid surv proc")] public string T1MidSurv { get; set; }
        [Name("T2 mid surv proc")] public string T2MidSurv { get; set; }
    }

    // ReSharper disable once UnusedType.Global
    public class ItemCSV
    {
        [Name("Sector name")] public string Sector { get; set; }
        [Name("Item name")] public string Item { get; set; }
        [Name("Loot tier")] public string Tier { get; set; }
        [Name("Poor min")] public double PoorMin { get; set; }
        [Name("Poor max")] public double PoorMax { get; set; }
        [Name("Normal min")] public double NormalMin { get; set; }
        [Name("Normal max")] public double NormalMax { get; set; }
        [Name("Optimal min")] public double OptimalMin { get; set; }
        [Name("Optimal max")] public double OptimalMax { get; set; }
    }

    private const string ItemPath = "Items (detailed).csv";
    private const string SectorPath = "Sectors (detailed).csv";

    public static void ExportDetailed()
    {
        ItemDetailed.Items.Clear();

        using var reader = new FileInfo(Path.Combine(Plugin.PluginDir, "Resources", ItemPath)).OpenText();
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });
        foreach (var itemDetailed in csv.GetRecords<ItemCSV>())
        {
            var itemRow = Sheets.ItemSheet.First(i => i.Name == itemDetailed.Item).RowId;
            var subRow = Sheets.ExplorationSheet.First(s => string.Equals(Utils.UpperCaseStr(s.Destination), itemDetailed.Sector, StringComparison.InvariantCultureIgnoreCase)).RowId;

            var detail = new ItemDetail
            {
                Sector = subRow,
                Tier = itemDetailed.Tier,
                Poor = $"{itemDetailed.PoorMin:N0} - {itemDetailed.PoorMax:N0}",
                Normal = $"{itemDetailed.NormalMin:N0} - {itemDetailed.NormalMax:N0}",
                Optimal = $"{itemDetailed.OptimalMin:N0} - {itemDetailed.OptimalMax:N0}"

            };

            if (!ItemDetailed.Items.TryAdd(itemRow, [detail]))
                ItemDetailed.Items[itemRow].Add(detail);
        }

        var path = Path.Combine(Plugin.PluginDir, FilenameItem);
        if (File.Exists(path))
            File.Delete(path);

        File.WriteAllBytes(path, MessagePackSerializer.Serialize(ItemDetailed));
    }
    #endif
    #endregion
}
