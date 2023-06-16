using Lumina.Excel.GeneratedSheets;

namespace SubmarineTracker.Data;

public static class Storage
{
    public static bool Refresh = true;
    public static Dictionary<ulong, Dictionary<uint, CachedItem>> StorageCache = new();

    public record CachedItem(Item Item, uint Count);

    public static void BuildStorageCache()
    {
        if (Refresh)
            Refresh = false;
        else
            return;

        StorageCache.Clear();

        var possibleItems = (ImportantItems[]) Enum.GetValues(typeof(ImportantItems));
        foreach (var key in Submarines.KnownSubmarines.Keys)
        {
            StorageCache.Add(key, new Dictionary<uint, CachedItem>());

            foreach (var item in possibleItems.Select(e => e.GetItem()))
            {
                var count = Plugin.AllaganToolsConsumer.GetCount(item.RowId, key);

                if (count != 0 && count != uint.MaxValue)
                    StorageCache[key].Add(item.RowId, new CachedItem(item, count));
            }
        }
    }
}
