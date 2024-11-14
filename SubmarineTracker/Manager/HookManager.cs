using System.Threading.Tasks;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using SubmarineTracker.Data;

namespace SubmarineTracker.Manager;

public class HookManager
{
    private readonly Plugin Plugin;

    private const string PacketReceiverSig = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 44 0F B6 43 ?? 4C 8D 4B 17";
    private const string PacketReceiverSigCN = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 44 0F B6 46 ?? 4C 8D 4E 17";
    private delegate void PacketDelegate(uint param1, ushort param2, sbyte param3, Int64 param4, char param5);
    private readonly Hook<PacketDelegate> PacketHandlerHook;

    public HookManager(Plugin plugin)
    {
        Plugin = plugin;

        // Try to resolve the CN sig if normal one fails ...
        // Doing this because CN people use an outdated version that still uploads data
        // so trying to get them at least somewhat up to date
        nint packetReceiverPtr;
        try
        {
            packetReceiverPtr = Plugin.SigScanner.ScanText(PacketReceiverSig);
        }
        catch (Exception)
        {
            Plugin.Log.Error("Exception in sig scan, maybe CN client?");
            packetReceiverPtr = Plugin.SigScanner.ScanText(PacketReceiverSigCN);
        }

        PacketHandlerHook = Plugin.Hook.HookFromAddress<PacketDelegate>(packetReceiverPtr, PacketReceiver);
        PacketHandlerHook.Enable();
    }

    public void Dispose()
    {
        PacketHandlerHook.Dispose();
    }

    private unsafe void PacketReceiver(uint param1, ushort param2, sbyte param3, Int64 param4, char param5)
    {
        PacketHandlerHook.Original(param1, param2, param3, param4, param5);

        // We only care about voyage Result
        if (param1 != 721343)
            return;

        try
        {
            var instance = HousingManager.Instance();
            if (instance == null || instance->WorkshopTerritory == null)
                return;

            var current = instance->WorkshopTerritory->Submersible.DataPointers[4];
            if (current.Value == null)
                return;

            var sub = current.Value;

            var fcId = Plugin.GetFCId;
            var register = sub->RegisterTime;
            var returnTime = sub->RegisterTime;

            var data = sub->GatheredData;
            if (data[0].ItemIdPrimary == 0)
                return;

            var validSectors = data.Filter(val => val.Point > 0);
            var expGathered = (uint) validSectors.Sum(val => val.ExpGained);
            var buildRank = Sectors.CalculateOriginalRank(sub->RankId, sub->CurrentExp, expGathered);
            var build = new Build.SubmarineBuild(buildRank, sub->HullId, sub->SternId, sub->BowId, sub->BridgeId);

            var lootList = new List<Loot>();
            foreach (var val in validSectors)
                lootList.Add(new Loot(build, val) {FreeCompanyId = fcId, Register = register, Return = returnTime});

            Task.Run(() =>
            {
                try
                {
                    foreach (var loot in lootList)
                        Plugin.DatabaseCache.Database.InsertLootEntry(loot);
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error(ex, "Error while upsert of loot entry");
                }
            });
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Error in packet receiver");
        }
    }
}
