// ReSharper disable FieldCanBeMadeReadOnly.Global
// MessagePack can't deserialize into readonly

using System.Collections.Frozen;
using System.IO;
using Lumina.Excel.GeneratedSheets;
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

public static class Importer
{
    public const string Filename = "CalculatedData.msgpack";

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
            using var fileStream = File.OpenRead(Path.Combine(Plugin.PluginDir, Filename));
            CalculatedData = MessagePackSerializer.Deserialize<CalculatedData>(fileStream);
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Failed loading calculated data.");
            CalculatedData = new CalculatedData();
        }
    }

    #region MessagePackCreation
    #if DEBUG
    public static void Export()
    {
        try
        {
            var dict = new Dictionary<int, Route[]>();
            foreach (var mapId in Plugin.Data.GetExcelSheet<SubmarineMap>()!.Where(m => m.RowId != 0).Select(m => m.RowId))
                dict.Add((int) mapId, Voyage.FindAllRoutes(mapId));

            CalculatedData = new CalculatedData {Maps = dict};

            var path = Path.Combine(Plugin.PluginDir, Filename);
            if (File.Exists(path))
                File.Delete(path);

            File.WriteAllBytes(path, MessagePackSerializer.Serialize(CalculatedData));
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Can't build routes.");
        }
    }
    #endif
    #endregion
}
