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
        public bool AutoSelectCurrent = true;
        public bool ShowRouteOverlay = true;
        public bool MaximizeDuration = true;
        public int HighestLevel = 90;
        public bool ShowNextOverlay = true;
        public bool ShowUnlockOverlay = true;

        public bool CalculateOnInteraction = false;
        public DurationLimit DurationLimit = DurationLimit.None;

        public bool NotifyForAll = true;
        public Dictionary<string, bool> NotifySpecific = new ();
        public bool NotifyForReturns = true;
        public bool NotifyForRepairs = true;
        public bool ShowRepairToast = true;
        public bool WebhookDispatch = true;
        public bool WebhookReturn = true;
        public string WebhookUrl = string.Empty;


        public bool OverlayOpen = false;
        public bool OverlayStartUp = false;
        public bool OverlayAlwaysOpen = false;
        public bool OverlayUnminimized = false;
        public bool OverlayCharacterName = false;
        public bool OverlayFirstReturn = false;
        public bool OverlayShowDate = false;
        public bool OverlayOnlyReturned = false;
        public bool OverlaySort = false;
        public bool OverlaySortReverse = false;
        public bool OverlayLockSize = false;
        public bool OverlayLockLocation = false;
        public bool OverlayShowRank = false;
        public bool OverlayShowBuild = false;

        public bool ExcludeLegacy = false;
        public Dictionary<uint, int> CustomLootWithValue = new();
        public DateLimit DateLimit = DateLimit.None;

        public bool ExportExcludeDate = true;
        public bool ExportExcludeHash;
        public string ExportOutputPath = string.Empty;

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
