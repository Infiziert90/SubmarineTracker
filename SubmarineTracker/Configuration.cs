using Dalamud.Configuration;
using Dalamud.Plugin;
using SubmarineTracker.Data;

namespace SubmarineTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public bool ShowExtendedPartsList = false;
        public bool ShowTimeInOverview = true;
        public bool UseDateTimeInstead = false;
        public bool ShowBothOptions = false;
        public bool ShowRouteInOverview = true;
        public bool ShowOnlyLowest = true;
        public bool ShowPrediction = true;
        public bool UseCharacterName = false;
        public bool UserResize = false;
        public bool ShowAll = true;
        public bool ShowRouteInAll = false;
        public bool ShowDateInAll = false;

        public bool ShowOnlyCurrentFC = false;

        public bool CalculateOnInteraction = false;
        public DurationLimit DurationLimit = DurationLimit.None;

        public bool NotifyOverlayAlways = false;
        public bool NotifyOverlayOnStartup = false;
        public bool NotifyForAll = true;
        public Dictionary<string, bool> NotifySpecific = new ();
        public bool NotifyForRepairs = true;
        public bool ShowRepairToast = true;

        public bool ExcludeLegacy = false;
        public Dictionary<uint, int> CustomLootWithValue = new();
        public DateLimit DateLimit = DateLimit.None;

        public Dictionary<string, Build.RouteBuild> SavedBuilds = new();

        public List<ulong> FCOrder = new();

        [NonSerialized]
        private DalamudPluginInterface? PluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }
    }
}
