using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using Lumina.Excel.GeneratedSheets;

namespace SubmarineTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public bool ShowExtendedPartsList = false;
        public bool ShowTimeInOverview = false;
        public bool UseDateTimeInstead = false;
        public bool ShowRouteInOverview = false;
        public bool UseCharacterName = false;


        public List<uint> CustomLoot = new();
        public Dictionary<uint, int> CustomLootWithValue = new();

        [NonSerialized]
        private DalamudPluginInterface? PluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;

            if (Version == 0)
            {
                var itemSheet = Plugin.Data.GetExcelSheet<Item>()!;
                foreach (var key in CustomLoot)
                {
                    var value = 0;
                    var item = itemSheet.GetRow(key)!;
                    if (item.PriceLow > 1000)
                        value = (int) item.PriceLow;

                    CustomLootWithValue.Add(key, value);
                }

                CustomLoot.Clear();
                Version = 1;
                Save();
            }
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }
    }
}
