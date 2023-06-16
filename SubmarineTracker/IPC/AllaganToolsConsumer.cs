using Dalamud.Logging;
using Dalamud.Plugin.Ipc;

namespace SubmarineTracker.IPC;

public class AllaganToolsConsumer
{
    private bool Available;
    private long TimeSinceLastCheck;

    public AllaganToolsConsumer() => Subscribe();

    public bool IsAvailable
    {
        get
        {
            if (TimeSinceLastCheck + 5000 > Environment.TickCount64)
            {
                return Available;
            }

            try
            {
                IsInitialized.InvokeFunc();
                Available = true;
                TimeSinceLastCheck = Environment.TickCount64;
            }
            catch
            {
                Available = false;
            }

            return Available;
        }
    }

    private ICallGateSubscriber<bool> IsInitialized = null!;
    private ICallGateSubscriber<uint, ulong, int, uint> ItemCount = null!;

    private void Subscribe()
    {
        try
        {
            IsInitialized = Plugin.PluginInterface.GetIpcSubscriber<bool>("AllaganTools.IsInitialized");
            ItemCount = Plugin.PluginInterface.GetIpcSubscriber<uint, ulong, int, uint>("AllaganTools.ItemCount");
        }
        catch (Exception e)
        {
            PluginLog.LogDebug($"Failed to subscribe to AllaganTools\nReason: {e}");
        }
    }

    public uint GetCount(uint itemId, ulong characterId)
    {
        try
        {
            // -1 checks all inventories
            return ItemCount.InvokeFunc(itemId, characterId, -1);
        }
        catch
        {
            Plugin.ChatGui.PrintError("AllaganTools plugin is not responding");
            return uint.MaxValue;
        }
    }
}
