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
    private ICallGateSubscriber<uint, ulong, uint, uint> ItemCount = null!;

    private void Subscribe()
    {
        try
        {
            IsInitialized = Plugin.PluginInterface.GetIpcSubscriber<bool>("AllaganTools.IsInitialized");
            ItemCount = Plugin.PluginInterface.GetIpcSubscriber<uint, ulong, uint, uint>("AllaganTools.ItemCount");
        }
        catch (Exception e)
        {
            PluginLog.LogDebug($"Failed to subscribe to AllaganTools\nReason: {e}");
        }
    }

    public uint GetCount(uint itemId, ulong characterId, uint inventoryType)
    {
        try
        {
            return ItemCount.InvokeFunc(itemId, characterId, inventoryType);
        }
        catch
        {
            Plugin.ChatGui.PrintError("AllaganTools plugin is not responding");
            return uint.MaxValue;
        }
    }
}
