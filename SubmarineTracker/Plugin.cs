using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Interface.Windowing;
using EurekaTrackerAutoPopper.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using SubmarineTracker.Data;
using SubmarineTracker.Windows;

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

        public string Name => "Submarine Tracker";

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("Submarine Tracker");

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }
        private BuilderWindow BuilderWindow { get; init; }
        private LootWindow LootWindow { get; init; }
        private HelpyWindow HelpyWindow { get; init; }
        private NotifyOverlay NotifyOverlay { get; init; }

        public static readonly string Authors = "Infi";
        public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

        private readonly PluginCommandManager<Plugin> CommandManager;
        private Notify Notify;

        public Plugin()
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            Notify = new Notify(this);

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this, Configuration);
            BuilderWindow = new BuilderWindow(this, Configuration);
            LootWindow = new LootWindow(this, Configuration);
            HelpyWindow = new HelpyWindow(this, Configuration);
            NotifyOverlay = new NotifyOverlay(this, Configuration, Notify);

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(BuilderWindow);
            WindowSystem.AddWindow(LootWindow);
            WindowSystem.AddWindow(HelpyWindow);
            WindowSystem.AddWindow(NotifyOverlay);

            CommandManager = new PluginCommandManager<Plugin>(this, Commands);

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            Submarines.Initialize();
            TexturesCache.Initialize();

            Submarines.LoadCharacters();
            LoadFCOrder();

            Framework.Update += FrameworkUpdate;
            Framework.Update += Notify.NotifyLoop;

            if (Configuration.NotifyOverlayAlways || Configuration.NotifyOverlayOnStartup)
                NotifyOverlay.IsOpen = true;
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();

            ConfigWindow.Dispose();
            MainWindow.Dispose();
            BuilderWindow.Dispose();
            LootWindow.Dispose();

            CommandManager.Dispose();

            TexturesCache.Instance?.Dispose();

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

        public unsafe void FrameworkUpdate(Framework _)
        {
            var instance = HousingManager.Instance();
            if (instance == null || instance->WorkshopTerritory == null)
                return;

            var local = ClientState.LocalPlayer;
            if (local == null)
                return;

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
                if (sub.ReturnTime != 0)
                    fc.AddSubLoot(sub.RegisterTime, sub.ReturnTime, sub.GatheredDataSpan);

            fc.Refresh = true;
            LoadFCOrder();
            Submarines.SaveCharacter();
        }

        #region Draws
        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }

        public void OpenNotify()
        {
            NotifyOverlay.IsOpen = true;
        }
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
