using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using SubmarineTracker.Data;

namespace SubmarineTracker.Manager;

public class HookManager
{
    private Plugin Plugin;

    private const string PacketReceiverSig = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 44 0F B6 43 ?? 4C 8D 4B 17";
    private delegate void PacketDelegate(uint param1, ushort param2, sbyte param3, Int64 param4, char param5);
    private Hook<PacketDelegate> PacketHandlerHook;

    public HookManager(Plugin plugin)
    {
        Plugin = plugin;

        var packetReceiverPtr = Plugin.SigScanner.ScanText(PacketReceiverSig);
        PacketHandlerHook = Hook<PacketDelegate>.FromAddress(packetReceiverPtr, PacketReceiver);
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
            PluginLog.Warning("No cached submarine found");
            return;
        }

        fc.AddSubLoot(sub->RegisterTime, cachedStats.Return, cachedStats.Build, sub->GatheredDataSpan);
    }
}
