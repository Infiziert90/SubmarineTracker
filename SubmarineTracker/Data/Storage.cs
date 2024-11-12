using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

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

        var possibleItems = (Items[]) Enum.GetValues(typeof(Items));
        foreach (var key in  Plugin.DatabaseCache.GetFreeCompanies().Keys)
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

    public static unsafe int InventoryCount(Items item)
    {
        var manager = InventoryManager.Instance();
        return manager == null ? -1 : manager->GetInventoryItemCount((uint) item, false, false);
    }

    public static (int Voyages, int Repairs) CheckLeftovers(IEnumerable<Submarine> subs)
    {
        var tanks = InventoryCount(Items.Tanks);
        var kits = InventoryCount(Items.Kits);

        if (tanks == -1 || kits == -1)
        {
            Plugin.Log.Warning("InventoryManager was null");
            return (-1, -1);
        }

        var requiredTanks = 0;
        var requiredKits = 0;
        foreach (var sub in subs)
        {
            requiredTanks += sub.Points.Sum(p => Sheets.ExplorationSheet.GetRow(p)!.CeruleumTankReq);
            requiredKits += sub.Build.RepairCosts;
        }

        if (requiredTanks == 0 || requiredKits == 0)
            return (-1, -1);

        return (tanks / requiredTanks, kits / requiredKits);
    }
}
