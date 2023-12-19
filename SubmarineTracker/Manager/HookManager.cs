using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using SubmarineTracker.Data;

namespace SubmarineTracker.Manager;

public class HookManager
{
    private readonly Plugin Plugin;

    private const string PacketReceiverSig = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 44 0F B6 43 ?? 4C 8D 4B 17";
    private const string PacketReceiverSigCN = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 44 0F B6 46 ??";
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

            var current = instance->WorkshopTerritory->Submersible.DataPointerListSpan[4];
            if (current.Value == null)
                return;

            var sub = current.Value;
            var fc = Submarines.KnownSubmarines[Plugin.ClientState.LocalContentId];
            if (!Plugin.SubmarinePreVoyage.TryGetValue(sub->RegisterTime, out var cachedStats))
            {
                Plugin.Log.Warning("No cached submarine found");
                return;
            }

            fc.AddSubLoot(sub->RegisterTime, cachedStats.Return, cachedStats.Build, sub->GatheredDataSpan);
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e.Message);
            Plugin.Log.Error(e.StackTrace ?? "Unknown");
        }
    }
}
