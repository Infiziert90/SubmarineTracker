using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using EurekaTrackerAutoPopper.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using SubmarineTracker.Data;
using SubmarineTracker.Windows;
using static SubmarineTracker.Utils;

namespace SubmarineTracker
{
    public class Plugin : IDalamudPlugin
    {
        [PluginService] public static DataManager Data { get; private set; } = null!;
        [PluginService] public static Framework Framework { get; private set; } = null!;
        [PluginService] public static GameGui GameGui { get; private set; } = null!;
        [PluginService] public static CommandManager Commands { get; private set; } = null!;
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static ClientState ClientState { get; private set; } = null!;

        public string Name => "Submarine Tracker";

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("Submarine Tracker");

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }
        private BuilderWindow BuilderWindow { get; init; }

        private readonly PluginCommandManager<Plugin> CommandManager;

        public Plugin()
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this, Configuration);
            BuilderWindow = new BuilderWindow(this, Configuration);

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(BuilderWindow);

            CommandManager = new PluginCommandManager<Plugin>(this, Commands);

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            Submarines.Initialize();
            TexturesCache.Initialize();

            Framework.Update += FrameworkUpdate;
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();

            ConfigWindow.Dispose();
            MainWindow.Dispose();

            CommandManager.Dispose();

            TexturesCache.Instance?.Dispose();

            Framework.Update -= FrameworkUpdate;
        }

        [Command("/stracker")]
        [HelpMessage("Toggles UI")]
        private void OnCommand(string command, string args)
        {
            Submarines.LoadCharacters();
            MainWindow.IsOpen = true;
        }

        [Command("/sbuilder")]
        [HelpMessage("Toggles UI")]
        private void OnBuilderCommand(string command, string args)
        {
            Submarines.LoadCharacters();
            BuilderWindow.IsOpen = true;
        }

        public unsafe void FrameworkUpdate(Framework _)
        {
            var instance = HousingManager.Instance();
            if (instance == null || instance->WorkshopTerritory == null)
                return;

            var local = ClientState.LocalPlayer;
            if (local == null)
                return;

            Submarines.KnownSubmarines.TryAdd(ClientState.LocalContentId, Submarines.FcSubmarines.Empty);

            var equal = false;
            var possibleNewSubs = new List<Submarines.Submarine>();
            foreach (var sub in instance->WorkshopTerritory->Submersible.DataListSpan.ToArray().Where(data => data.RankId != 0))
            {
                var playerSub = new Submarines.Submarine(sub);
                possibleNewSubs.Add(playerSub);

                equal |= Submarines.KnownSubmarines[ClientState.LocalContentId].Submarines.Any(s => s.Equals(playerSub));
            }

            if (equal)
                return;

            var newEntry = new Submarines.FcSubmarines(ToStr(local.CompanyTag),ToStr(local.HomeWorld.GameData!.Name), possibleNewSubs);
            Submarines.KnownSubmarines[ClientState.LocalContentId] = newEntry;

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
        #endregion
    }
}
