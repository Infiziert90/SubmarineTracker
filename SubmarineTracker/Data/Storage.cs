using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace SubmarineTracker.Data;

public static class Storage
{
    public static bool Refresh = true;
    public static readonly Dictionary<ulong, Dictionary<uint, CachedItem>> StorageCache = new();

    public record CachedItem(Item Item, uint Count);

    public static int InventorySlotsFree = -1;

    public static void BuildStorageCache()
    {
        if (!Refresh)
            return;

        Refresh = false;
        StorageCache.Clear();

        var possibleItems = Enum.GetValues<Items>();
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

    public static unsafe void GetFreeSlotCount()
    {
        var manager = InventoryManager.Instance();
        InventorySlotsFree = manager == null ? -1 : (int)manager->GetEmptySlotsInBag();
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

        var requiredKits = 0;
        var requiredTanks = 0;
        foreach (var sub in subs)
        {
            requiredKits += sub.Build.RepairCosts;
            requiredTanks += Voyage.ToExplorationArray(sub.Points).Sum(p => p.CeruleumTankReq);
        }

        if (requiredTanks == 0 || requiredKits == 0)
            return (-1, -1);

        return (tanks / requiredTanks, kits / requiredKits);
    }
}
