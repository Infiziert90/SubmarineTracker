using System.IO;
using Dalamud.Configuration;
using Newtonsoft.Json;
using SubmarineTracker.Data;
using SubmarineTracker.Windows;

namespace SubmarineTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public NameOptions NameOption = NameOptions.Default;

        public bool ShowExtendedPartsList = false;
        public bool ShowTimeInOverview = true;
        public bool UseDateTimeInstead = false;
        public bool ShowBothOptions = false;
        public bool ShowRouteInOverview = true;
        public bool ShowOnlyLowest = true;
        public bool ShowPrediction = true;
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
        public bool MainRouteAutoInclude = true;

        public bool CalculateOnInteraction = false;
        public DurationLimit DurationLimit = DurationLimit.None;
        public int CustomHour = 42;
        public int CustomMinute = 30;

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
        public bool OverlayFirstReturn = false;
        public bool OverlayShowDate = false;
        public bool OverlayOnlyReturned = false;
        public bool OverlaySort = false;
        public bool OverlaySortReverse = false;
        public bool OverlayLockSize = false;
        public bool OverlayLockLocation = false;
        public bool OverlayShowRank = false;
        public bool OverlayShowBuild = false;
        public bool OverlayHoldClosed = false;
        public bool OverlayTitleTime = false;
        public Vector4 OverlayAllDone = Helper.CustomFullyDone;
        public Vector4 OverlayPartlyDone = Helper.CustomPartlyDone;
        public Vector4 OverlayNoneDone = Helper.CustomOnRoute;

        public bool ExcludeLegacy = false;
        public Dictionary<string, Dictionary<uint, int>> CustomLootProfiles = new() {{ "Default", new Dictionary<uint, int>() }};
        public DateLimit DateLimit = DateLimit.None;

        public bool ExportExcludeDate = true;
        public bool ExportExcludeHash;
        public string ExportOutputPath = string.Empty;

        public bool UploadNotification = true;
        public DateTime UploadNotificationReceived = DateTime.MaxValue;
        public bool UploadPermission = true;

        public bool ShowStorageMessage = true;
        public Dictionary<ulong, string> IgnoredCharacters = new();

        public Dictionary<string, Build.RouteBuild> SavedBuilds = new();

        public List<ulong> FCOrder = new();

        public void Save()
        {
            WriteAllTextSafe(Plugin.PluginInterface.ConfigFile.FullName, JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects
            }));
        }

        internal static void WriteAllTextSafe(string path, string text)
        {
            try
            {
                var str = path + ".tmp";
                if (File.Exists(str))
                    File.Delete(str);
                File.WriteAllText(str, text);
                File.Move(str, path, true);
            }
            catch (Exception e)
            {
                Plugin.Log.Error(e.Message);
                Plugin.Log.Error(e.StackTrace ?? "Unknown");
            }
        }
    }
}
