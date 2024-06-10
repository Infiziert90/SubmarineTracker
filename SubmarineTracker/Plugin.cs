using System.IO;
using System.Threading.Tasks;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using SubmarineTracker.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Newtonsoft.Json;
using SubmarineTracker.Data;
using SubmarineTracker.IPC;
using SubmarineTracker.Manager;
using SubmarineTracker.Windows;
using SubmarineTracker.Windows.Main;
using SubmarineTracker.Windows.Loot;
using SubmarineTracker.Windows.Helpy;
using SubmarineTracker.Windows.Config;
using SubmarineTracker.Windows.Builder;
using SubmarineTracker.Windows.Migration;
using SubmarineTracker.Windows.Overlays;

namespace SubmarineTracker
{
    public class Plugin : IDalamudPlugin
    {
        [PluginService] public static IDataManager Data { get; private set; } = null!;
        [PluginService] public static IFramework Framework { get; private set; } = null!;
        [PluginService] public static ICommandManager Commands { get; private set; } = null!;
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static IClientState ClientState { get; private set; } = null!;
        [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
        [PluginService] public static IToastGui ToastGui { get; private set; } = null!;
        [PluginService] public static IGameGui GameGui { get; private set; } = null!;
        [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
        [PluginService] public static IGameInteropProvider Hook { get; private set; } = null!;
        [PluginService] public static ITextureProvider Texture { get; private set; } = null!;
        [PluginService] public static IPluginLog Log { get; private set; } = null!;
        [PluginService] public static INotificationManager Notification { get; private set; } = null!;
        [PluginService] public static IDtrBar DtrBar { get; private set; } = null!;

        public static Configuration Configuration { get; private set; } = null!;
        public static FileDialogManager FileDialogManager { get; private set; } = null!;

        public readonly WindowSystem WindowSystem = new("Submarine Tracker");
        public ConfigWindow ConfigWindow { get; init; }
        public MainWindow MainWindow { get; init; }
        public BuilderWindow BuilderWindow { get; init; }
        public LootWindow LootWindow { get; init; }
        public HelpyWindow HelpyWindow { get; init; }
        public ReturnOverlay ReturnOverlay { get; init; }
        public RouteOverlay RouteOverlay { get; init; }
        public NextOverlay NextOverlay { get; init; }
        public UnlockOverlay UnlockOverlay { get; init; }
        public MigrationWindow MigrationWindow { get; init; }

        public static string PluginDir => PluginInterface.AssemblyLocation.DirectoryName!;

        private const string GithubIssue = "https://github.com/Infiziert90/SubmarineTracker/issues";
        private const string DiscordThread = "https://canary.discord.com/channels/581875019861328007/1094255662860599428";
        private const string Crowdin = "https://crowdin.com/project/submarine-tracker";
        private const string KoFiLink = "https://ko-fi.com/infiii";

        private readonly PluginCommandManager<Plugin> CommandManager;
        private readonly ServerBar ServerBar;

        public static DatabaseCache DatabaseCache = null!;
        public readonly Notify Notify;
        public readonly NameConverter NameConverter;
        public static HookManager HookManager = null!;
        public static AllaganToolsConsumer AllaganToolsConsumer = null!;

        public readonly Localization Localization = new();

        public readonly Dictionary<uint, Submarine> SubmarinePreVoyage = new();
        private bool ShowIgnoredWarning = true;
        private bool ShowStorageMessage = true;

        public readonly bool FirstTimeMigration;

        public Plugin()
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Localization.SetupWithLangCode(PluginInterface.UiLanguage);

            FileDialogManager = new FileDialogManager();

            // Is required by everything, so init it here
            FirstTimeMigration = !File.Exists(Database.DatabasePath());
            DatabaseCache = new DatabaseCache();

            NameConverter = new NameConverter();
            Notify = new Notify(this);

            HookManager = new HookManager(this);
            AllaganToolsConsumer = new AllaganToolsConsumer();

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this);
            BuilderWindow = new BuilderWindow(this);
            LootWindow = new LootWindow(this);
            HelpyWindow = new HelpyWindow(this);

            ReturnOverlay = new ReturnOverlay(this);
            RouteOverlay = new RouteOverlay(this);
            NextOverlay = new NextOverlay(this);
            UnlockOverlay = new UnlockOverlay(this);
            MigrationWindow = new MigrationWindow(this);

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(BuilderWindow);
            WindowSystem.AddWindow(LootWindow);
            WindowSystem.AddWindow(HelpyWindow);

            WindowSystem.AddWindow(ReturnOverlay);
            WindowSystem.AddWindow(RouteOverlay);
            WindowSystem.AddWindow(NextOverlay);
            WindowSystem.AddWindow(UnlockOverlay);
            WindowSystem.AddWindow(MigrationWindow);

            CommandManager = new PluginCommandManager<Plugin>(this, Commands);
            ServerBar = new ServerBar(this);

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
            PluginInterface.LanguageChanged += Localization.SetupWithLangCode;

            LoadFCOrder();

            Framework.Update += FrameworkUpdate;
            Framework.Update += Notify.NotifyLoop;

            ClientState.Login += Login;

            // Try to init it last, just to make sure that loc actually loaded fine
            Helper.Initialize(this);

            var subDone = DatabaseCache.GetSubmarines().Any(s => s.IsDone());
            if (Configuration.OverlayOpen || (Configuration.OverlayStartUp && subDone))
                ReturnOverlay.IsOpen = true;

            // Trigger Importer to precalculate hashes
            Log.Debug($"Loading: {Importer.Filename}");

            if (ClientState.IsLoggedIn)
                Login();
        }

        public void Dispose()
        {
            DatabaseCache.Dispose();

            WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();
            MainWindow.Dispose();
            BuilderWindow.Dispose();
            LootWindow.Dispose();
            HelpyWindow.Dispose();

            ReturnOverlay.Dispose();
            RouteOverlay.Dispose();
            NextOverlay.Dispose();
            UnlockOverlay.Dispose();
            MigrationWindow.Dispose();

            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;
            PluginInterface.LanguageChanged -= Localization.SetupWithLangCode;

            CommandManager.Dispose();
            HookManager.Dispose();
            ServerBar.Dispose();

            ClientState.Login -= Login;

            Framework.Update -= FrameworkUpdate;
            Framework.Update -= Notify.NotifyLoop;
        }

        [Command("/stracker")]
        [HelpMessage("Opens the tracker")]
        private void OnCommand(string command, string args)
        {
            MainWindow.IsOpen ^= true;
        }

        [Command("/sbuilder")]
        [HelpMessage("Opens the builder")]
        private void OnBuilderCommand(string command, string args)
        {
            BuilderWindow.IsOpen ^= true;
        }

        [Command("/sloot")]
        [HelpMessage("Opens the custom loot overview\n/sloot copy - Export loot history to clipboard in CSV format using current settings")]
        private void OnLootCommand(string command, string args)
        {
            switch (args.Trim())
            {
                case "copy":
                    LootWindow.ExportToClipboard();
                    break;
                default:
                    LootWindow.IsOpen ^= true;
                    break;
            }
        }

        [Command("/shelpy")]
        [HelpMessage("Opens the helper window with lots of helpful information")]
        private void OnUnlockedCommand(string command, string args)
        {
            HelpyWindow.IsOpen ^= true;
        }

        [Command("/sconf")]
        [HelpMessage("Opens the config")]
        private void OnConfigCommand(string command, string args)
        {
            ConfigWindow.IsOpen ^= true;
        }

        [Command("/soverlay")]
        [HelpMessage("Opens the overlay")]
        private void OnOverlayCommand(string command, string args)
        {
            ReturnOverlay.IsOpen ^= true;

            Configuration.OverlayOpen ^= true;
            Configuration.Save();
        }

        // TODO Remove after migration time
        private void Login()
        {
            string LoadFile(FileSystemInfo fileInfo)
            {
                for (var i = 0; i < 5; i++)
                {
                    try
                    {
                        using var reader = new StreamReader(fileInfo.FullName);
                        return reader.ReadToEnd();
                    }
                    catch
                    {
                        if (i == 4)
                            Notification.AddNotification(new Notification
                            {
                                Content = Loc.Localize("Warnings - Config Fail", "Failed to read config"),
                                Type = NotificationType.Warning,
                                Minimized = false,
                            });

                        Log.Warning($"Config file read failed {i + 1}/5");
                    }
                }

                return string.Empty;
            }

            var local = ClientState.LocalPlayer;
            if (local == null)
                return;

            var file = new FileInfo(Path.Combine(PluginInterface.ConfigDirectory.FullName, $"{ClientState.LocalContentId}.json"));
            if (!file.Exists)
                return;

            try
            {
                var config = JsonConvert.DeserializeObject<CharacterConfiguration>(LoadFile(file));
                var fc = new Submarines.FcSubmarines(config!);

                if (fc.Tag != Utils.ToStr(local.CompanyTag))
                {
                    Log.Warning("Stored data for this character is outdated, FC tags don't match");
                    Log.Warning("Import cancelled");
                    return;
                }

                Framework.RunOnTick(() =>
                {
                    var fcId = GetFCId;
                    Log.Information($"Working with id {fcId}");
                    if (fcId == 0)
                        return;

                    Task.Run(() =>
                    {
                        try
                        {
                            DatabaseCache.Database.UpsertFreeCompany(new FreeCompany(fcId, fc));

                            foreach (var sub in fc.Submarines)
                                DatabaseCache.Database.UpsertSubmarine(new Submarine(fcId, sub));

                            foreach (var (registerTime, lootEntry) in fc.SubLoot)
                                foreach (var (returnTime, sectors) in lootEntry.Loot)
                                    foreach (var sectorLoot in sectors)
                                        DatabaseCache.Database.InsertLootEntry(new Loot(fcId, registerTime, returnTime, sectorLoot));

                            file.Delete();
                            Log.Information($"Migrating id {fcId} done");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Unable to upsert entry");
                        }
                    });
                }, TimeSpan.FromSeconds(10));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to migrate character data");
            }
        }

        private bool IsUpserting;
        public bool NeedsRefresh => IsUpserting || DatabaseCache.FCNeedsRefresh || DatabaseCache.SubsNeedRefresh;

        public unsafe void FrameworkUpdate(IFramework _)
        {
            // Reload stale data
            if (DatabaseCache.NewData)
            {
                DatabaseCache.NewData = false;
                LoadFCOrder();
            }

            var fcId = GetFCId;
            if (fcId == 0)
                return;

            var instance = HousingManager.Instance();
            if (instance == null || instance->WorkshopTerritory == null)
            {
                // Clear the cache after we left workshop
                SubmarinePreVoyage.Clear();
                ShowIgnoredWarning = true;
                ShowStorageMessage = true;
                return;
            }

            var local = ClientState.LocalPlayer;
            if (local == null)
                return;

            // 6.4 triggers HousingManager + WorkshopTerritory in Island Sanctuary
            if (Sheets.TerritorySheet.GetRow(ClientState.TerritoryType)!.TerritoryIntendedUse == 49)
                return;

            // Notify the user once about upload opt out
            if (Configuration.UploadNotification)
            {
                // User received the notice, so we schedule the first upload 1h after
                Configuration.UploadNotification = false;
                Configuration.UploadNotificationReceived = DateTime.Now.AddHours(1);
                Configuration.Save();

                ChatGui.Print(Utils.SuccessMessage(Loc.Localize("Terms - Important","Important")));
                ChatGui.Print(Utils.SuccessMessage(Loc.Localize("Notifications - Upload Opt Out","This plugin will collect anonymized, submarine specific data. " +
                                                       "For more information on the exact data collected please see the upload tab in the plugin configuration menu.  " +
                                                       "You can opt out of any and all forms of data collection.")));
            }

            if (Configuration.IgnoredCharacters.ContainsKey(ClientState.LocalContentId))
            {
                if (ShowIgnoredWarning)
                {
                    Notification.AddNotification(new Notification
                    {
                        Content = Loc.Localize("Warnings - Ignored Character", "Ignored Character"),
                        Type = NotificationType.Warning,
                        Minimized = false,
                    });
                    ShowIgnoredWarning = false;
                }

                return;
            }

            if (Configuration.ShowStorageMessage && ShowStorageMessage)
            {

                if (DatabaseCache.GetFreeCompanies().ContainsKey(fcId))
                {
                    var subs = DatabaseCache.GetSubmarines().Where(s => s.FreeCompanyId == fcId);
                    var status = Storage.CheckLeftovers(subs);
                    if (status is { Voyages: > -1, Repairs: > -1 })
                    {
                        if (status is {Voyages: 0, Repairs: 0})
                            ChatGui.Print(Utils.ErrorMessage(Loc.Localize("Storage - Both","Not enough Tanks and Repair Kits!")));
                        else if (status.Voyages == 0)
                            ChatGui.Print(Utils.ErrorMessage(Loc.Localize("Storage - No Tanks","Not enough Tanks!")));
                        else if (status.Repairs == 0)
                            ChatGui.Print(Utils.ErrorMessage(Loc.Localize("Storage - No Kits","Not enough Repair Kits!")));
                        else
                            ChatGui.Print(Utils.SuccessMessage(Loc.Localize("Storage - All Okay","Your fleet has enough for {0} voyages and {1} combined repairs.").Format(status.Voyages, status.Repairs)));
                    }
                }

                ShowStorageMessage = false;
            }

            var workshopData = instance->WorkshopTerritory->Submersible;
            var submarineData = workshopData.DataListSpan.ToArray();

            BuilderWindow.VoyageInterfaceSelection = 0;
            if (Configuration.AutoSelectCurrent)
            {
                var current = workshopData.DataPointerListSpan[4];
                if (current.Value != null)
                {
                    BuilderWindow.VoyageInterfaceSelection = current.Value->RegisterTime;
                    if (BuilderWindow.CurrentBuild.Rank != current.Value->RankId)
                        BuilderWindow.CacheValid = false;
                }
            }

            var possibleNewSubs = new List<Submarine>();
            foreach (var (sub, idx) in submarineData.Where(data => data.RankId != 0).WithIndex())
            {
                possibleNewSubs.Add(new Submarine(sub, idx) {FreeCompanyId = fcId});

                // We prefill the current submarines once to have the original stats
                if (!SubmarinePreVoyage.ContainsKey(sub.RegisterTime))
                    SubmarinePreVoyage[sub.RegisterTime] = new Submarine(sub) {FreeCompanyId = fcId};
            }

            if (NeedsRefresh || possibleNewSubs.Count == 0)
                return;

            var orgSubs = DatabaseCache.GetSubmarines(fcId).ToList();
            if (Utils.SubmarinesEqual(orgSubs, possibleNewSubs))
                return;

            var fc = new FreeCompany
            {
                FreeCompanyId = fcId,
                Tag = Utils.ToStr(local.CompanyTag),
                World = Utils.ToStr(local.HomeWorld.GameData!.Name),
                CharacterName = Utils.ToStr(local.Name),
            };

            foreach (var submarineExploration in Sheets.ExplorationSheet)
            {
                fc.UnlockedSectors[submarineExploration.RowId] = HousingManager.IsSubmarineExplorationUnlocked((byte)submarineExploration.RowId);
                fc.ExploredSectors[submarineExploration.RowId] = HousingManager.IsSubmarineExplorationExplored((byte)submarineExploration.RowId);
            }

            foreach (var sub in submarineData.Where(data => data.RankId != 0 && data.ReturnTime != 0))
                Notify.TriggerDispatch(sub.RegisterTime, sub.ReturnTime);

            LoadFCOrder();
            IsUpserting = true;
            Task.Run(() =>
            {
                try
                {
                    DatabaseCache.Database.UpsertFreeCompany(fc);
                    foreach (var sub in possibleNewSubs)
                        DatabaseCache.Database.UpsertSubmarine(sub);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error while upsert of fc and submarines");
                }
                finally
                {
                    IsUpserting = false;
                    DatabaseCache.FCNeedsRefresh = true;
                    DatabaseCache.SubsNeedRefresh = true;
                }
            });
        }

        public void Sync()
        {
            Storage.Refresh = true;
            LoadFCOrder();
        }

        public static void IssuePage() => Util.OpenLink(GithubIssue);
        public static void DiscordSupport() => Util.OpenLink(DiscordThread);
        public static void Kofi() => Util.OpenLink(KoFiLink);
        public static void LocHelp() => Util.OpenLink(Crowdin);

        #region Draws

        private void DrawUI()
        {
            WindowSystem.Draw();
            FileDialogManager.Draw();
        }

        public void OpenTracker() => MainWindow.IsOpen = true;
        public void OpenBuilder() => BuilderWindow.IsOpen = true;
        public void OpenLoot() => LootWindow.IsOpen = true;
        public void OpenHelpy() => HelpyWindow.IsOpen = true;
        public void OpenOverlay() => ReturnOverlay.IsOpen = true;
        public void OpenConfig() => ConfigWindow.IsOpen = true;
        #endregion

        public static unsafe ulong GetFCId => InfoProxyFreeCompany.Instance()->ID;

        public static void LoadFCOrder()
        {
            var changed = false;
            foreach (var id in DatabaseCache.GetFreeCompanies().Keys)
            {
                if (!Configuration.FCIdOrder.Contains(id))
                {
                    changed = true;
                    Configuration.FCIdOrder.Add(id);
                }
            }

            if (changed)
                Configuration.Save();
        }

        public static void EnsureFCOrderSafety()
        {
            var notSafe = false;
            foreach (var id in Configuration.FCIdOrder.ToArray())
            {
                if (!DatabaseCache.GetFreeCompanies().ContainsKey(id))
                {
                    notSafe = true;
                    Configuration.FCIdOrder.Remove(id);
                }
            }

            if (notSafe)
                Configuration.Save();
        }

        public static void EntryUpload(Loot loot)
        {
            if (Configuration.UploadPermission)
            {
                // Check that the user had enough time to opt out after notification
                if (Configuration.UploadNotificationReceived > DateTime.Now)
                    return;

                Task.Run(() => Export.UploadEntry(loot));
            }
        }
    }
}
