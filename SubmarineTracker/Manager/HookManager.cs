using System.Threading.Tasks;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using SubmarineTracker.Data;

namespace SubmarineTracker.Manager;

public unsafe class HookManager
{
    // Dalamud CustomTalkEventResponsePacketHandler <https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Game/Network/Internal/NetworkHandlersAddressResolver.cs#L17>
    private const string PacketReceiverSig = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 49 8B D9 41 0F B6 F8 0F B7 F2 8B E9 E8 ?? ?? ?? ?? 44 0F B6 54 24 ?? 44 0F B6 CF 44 88 54 24 ?? 44 0F B7 C6 8B D5";
    private const string PacketReceiverSigCN = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 44 0F B6 46 ?? 4C 8D 4E 17";
    private delegate void PacketDelegate(nuint a1, ushort eventId, byte responseId, uint* args, byte argCount);
    private readonly Hook<PacketDelegate> PacketHandlerHook;

    public HookManager()
    {
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

    private void PacketReceiver(nuint a1, ushort eventId, byte responseId, uint* args, byte argCount)
    {
        PacketHandlerHook.Original(a1, eventId, responseId, args, argCount);

        // We only care about voyage results
        if (a1 != 721343)
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
            var returnTime = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // sub->ReturnTime is 0 at this point

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
