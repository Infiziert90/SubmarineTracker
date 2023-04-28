using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using SubmarineTracker.Data;

namespace SubmarineTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public bool ShowExtendedPartsList = false;
        public bool ShowTimeInOverview = false;
        public bool UseDateTimeInstead = false;
        public bool ShowBothOptions = false;
        public bool ShowRouteInOverview = false;
        public bool UseCharacterName = false;

        public bool NotifyOverlayAlways = false;
        public bool NotifyOverlayOnStartup = false;
        public bool NotifyForAll = false;
        public Dictionary<string, bool> NotifySpecific = new ();

        public Dictionary<uint, int> CustomLootWithValue = new();
        public DateLimit DateLimit = DateLimit.None;

        public Dictionary<string, Submarines.RouteBuild> SavedBuilds = new();

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
