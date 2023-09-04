using System.Reflection;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using SubmarineTracker.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;
using SubmarineTracker.IPC;
using SubmarineTracker.Windows;
using SubmarineTracker.Windows.Loot;
using SubmarineTracker.Windows.Helpy;
using SubmarineTracker.Windows.Config;
using SubmarineTracker.Windows.Builder;
using SubmarineTracker.Windows.Overlays;

namespace SubmarineTracker
{
    public class Plugin : IDalamudPlugin
    {
        [PluginService] public static DataManager Data { get; private set; } = null!;
        [PluginService] public static Framework Framework { get; private set; } = null!;
        [PluginService] public static CommandManager Commands { get; private set; } = null!;
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static ClientState ClientState { get; private set; } = null!;
        [PluginService] public static ChatGui ChatGui { get; private set; } = null!;
        [PluginService] public static ToastGui ToastGui { get; private set; } = null!;
        [PluginService] public static GameGui GameGui { get; private set; } = null!;

        public static FileDialogManager FileDialogManager { get; private set; } = null!;

        public string Name => "Submarine Tracker";

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("Submarine Tracker");

        public ConfigWindow ConfigWindow { get; init; }
        public MainWindow MainWindow { get; init; }
        public BuilderWindow BuilderWindow { get; init; }
        public LootWindow LootWindow { get; init; }
        public HelpyWindow HelpyWindow { get; init; }
        public ReturnOverlay ReturnOverlay { get; init; }
        public RouteOverlay RouteOverlay { get; init; }
        public NextOverlay NextOverlay { get; init; }
        public UnlockOverlay UnlockOverlay { get; init; }

        public ConfigurationBase ConfigurationBase;

        public const string Authors = "Infi";
        public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

        private readonly PluginCommandManager<Plugin> CommandManager;
        public Notify Notify;

        private static ExcelSheet<TerritoryType> TerritoryTypes = null!;

        public static AllaganToolsConsumer AllaganToolsConsumer = null!;

        public Plugin()
        {
            ConfigurationBase = new ConfigurationBase(this);

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            FileDialogManager = new FileDialogManager();

            Notify = new Notify(this);

            Loot.Initialize();
            Build.Initialize();
            Voyage.Initialize();
            Submarines.Initialize();
            TexturesCache.Initialize();
            ImportantItemsMethods.Initialize();

            Webhook.Init(Configuration);
            Helper.Initialize(this);

            AllaganToolsConsumer = new AllaganToolsConsumer();

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this, Configuration);
            BuilderWindow = new BuilderWindow(this, Configuration);
            LootWindow = new LootWindow(this, Configuration);
            HelpyWindow = new HelpyWindow(this, Configuration);

            ReturnOverlay = new ReturnOverlay(this, Configuration);
            RouteOverlay = new RouteOverlay(this, Configuration);
            NextOverlay = new NextOverlay(this, Configuration);
            UnlockOverlay = new UnlockOverlay(this, Configuration);

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(BuilderWindow);
            WindowSystem.AddWindow(LootWindow);
            WindowSystem.AddWindow(HelpyWindow);

            WindowSystem.AddWindow(ReturnOverlay);
            WindowSystem.AddWindow(RouteOverlay);
            WindowSystem.AddWindow(NextOverlay);
            WindowSystem.AddWindow(UnlockOverlay);

            CommandManager = new PluginCommandManager<Plugin>(this, Commands);

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;

            TerritoryTypes = Data.GetExcelSheet<TerritoryType>()!;

            ConfigurationBase.Load();
            LoadFCOrder();

            Framework.Update += FrameworkUpdate;
            Framework.Update += Notify.NotifyLoop;

            var subDone = Submarines.KnownSubmarines.Values.Any(fc => fc.AnySubDone());
            if (Configuration.OverlayOpen || (Configuration.OverlayStartUp && subDone))
            {
                ReturnOverlay.IsOpen = true;
                // TODO Check for a valid way to uncollapse something once
                // if (Configuration is { OverlayStartUp: true, OverlayUnminimized: true } && subDone)
                // {
                //     OverlayWindow.CollapsedCondition = ImGuiCond.Appearing;
                //     OverlayWindow.Collapsed = false;
                // }
            }
        }

        public void Dispose() => Dispose(true);

        public void Dispose(bool full)
        {
            ConfigurationBase.Dispose();
            WindowSystem.RemoveAllWindows();

            ConfigWindow.Dispose();
            MainWindow.Dispose();
            BuilderWindow.Dispose();
            LootWindow.Dispose();

            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;

            CommandManager.Dispose();

            TexturesCache.Instance?.Dispose();

            if (full)
            {
                Framework.Update -= FrameworkUpdate;
                Framework.Update -= Notify.NotifyLoop;
            }
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
        [HelpMessage("Opens the custom loot overview")]
        private void OnLootCommand(string command, string args)
        {
            LootWindow.IsOpen ^= true;
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

        public unsafe void FrameworkUpdate(Framework _)
        {
            var instance = HousingManager.Instance();
            if (instance == null || instance->WorkshopTerritory == null)
                return;

            var local = ClientState.LocalPlayer;
            if (local == null)
                return;

            // 6.4 triggers HousingManager + WorkshopTerritory in Island Sanctuary
            if (TerritoryTypes.GetRow(ClientState.TerritoryType)!.TerritoryIntendedUse == 49)
                return;

            if (Configuration.AutoSelectCurrent)
            {
                var current = instance->WorkshopTerritory->Submersible.DataPointerListSpan[4];

                if (current.Value != null)
                {
                    BuilderWindow.VoyageInterfaceSelection = current.Value->RegisterTime;
                    if (BuilderWindow.CurrentBuild.Rank != current.Value->RankId)
                        BuilderWindow.CacheValid = false;
                }
                else
                {
                    BuilderWindow.VoyageInterfaceSelection = 0;
                }
            }
            else
            {
                BuilderWindow.VoyageInterfaceSelection = 0;
            }

            var workshopData = instance->WorkshopTerritory->Submersible.DataListSpan.ToArray();

            var possibleNewSubs = new List<Submarines.Submarine>();
            foreach (var (sub, idx) in workshopData.Where(data => data.RankId != 0).Select((val, i) => (val, i)))
                possibleNewSubs.Add(new Submarines.Submarine(sub, idx));

            if (!possibleNewSubs.Any())
                return;

            Submarines.KnownSubmarines.TryAdd(ClientState.LocalContentId, Submarines.FcSubmarines.Empty);

            var fc = Submarines.KnownSubmarines[ClientState.LocalContentId];
            if (Submarines.SubmarinesEqual(fc.Submarines, possibleNewSubs) && fc.CharacterName != "")
                return;

            fc.CharacterName = Utils.ToStr(local.Name);
            fc.Tag = Utils.ToStr(local.CompanyTag);
            fc.World = Utils.ToStr(local.HomeWorld.GameData!.Name);
            fc.Submarines = possibleNewSubs;
            fc.GetUnlockedAndExploredSectors();

            foreach (var sub in workshopData.Where(data => data.RankId != 0))
            {
                if (sub.ReturnTime == 0)
                    continue;

                Notify.TriggerDispatch(sub.RegisterTime, sub.ReturnTime);
                fc.AddSubLoot(sub.RegisterTime, sub.ReturnTime, sub.GatheredDataSpan);
            }

            fc.Refresh = true;
            LoadFCOrder();
            ConfigurationBase.SaveCharacterConfig();
        }

        public void Sync()
        {
            foreach (var fc in Submarines.KnownSubmarines.Values)
                fc.Refresh = true;

            Storage.Refresh = true;
            ConfigurationBase.Load();
            LoadFCOrder();
        }

        #region Draws
        private void DrawUI() => WindowSystem.Draw();

        public void OpenTracker() => MainWindow.IsOpen = true;
        public void OpenBuilder() => BuilderWindow.IsOpen = true;
        public void OpenLoot() => LootWindow.IsOpen = true;
        public void OpenHelpy() => HelpyWindow.IsOpen = true;
        public void OpenConfig() => ConfigWindow.IsOpen = true;
        #endregion

        public void LoadFCOrder()
        {
            foreach (var id in Submarines.KnownSubmarines.Keys)
                if (!Configuration.FCOrder.Contains(id))
                    Configuration.FCOrder.Add(id);

            Configuration.Save();
        }

        public void EnsureFCOrderSafety()
        {
            var notSafe = false;
            foreach (var id in Configuration.FCOrder.ToArray())
            {
                if (!Submarines.KnownSubmarines.ContainsKey(id))
                {
                    notSafe = true;
                    Configuration.FCOrder.Remove(id);
                }
            }

            if (notSafe)
                Configuration.Save();
        }
    }
}
