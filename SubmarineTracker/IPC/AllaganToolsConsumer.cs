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
                return Available;

            try
            {
                TimeSinceLastCheck = Environment.TickCount64;

                IsInitialized.InvokeFunc();
                Available = true;
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
        catch (Exception ex)
        {
            Plugin.Log.Debug(ex, "Failed to subscribe to AllaganTools");
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
            Plugin.ChatGui.PrintError(Utils.ErrorMessage("AllaganTools plugin is not responding"));
            return uint.MaxValue;
        }
    }
}
