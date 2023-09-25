using Dalamud.Interface.Windowing;
using Lumina.Excel;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Helpy;

public partial class HelpyWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    public static ExcelSheet<SubmarineExplorationPretty> ExplorationSheet = null!;

    public HelpyWindow(Plugin plugin, Configuration configuration) : base("Helpy")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(720, 520),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = configuration;

        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExplorationPretty>()!;

        InitProgression();
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (!Submarines.KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
        {
            Helper.NoData();
            return;
        }

        var buttonHeight = ImGui.CalcTextSize("RRRR").Y + (20.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginChild("HelpyContent", new Vector2(0, -buttonHeight)))
        {
            if (ImGui.BeginTabBar("##helperTabBar"))
            {
                ProgressionTab(fcSub);

                StorageTab();

                ImGui.EndTabBar();
            }
        }
        ImGui.EndChild();

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(1.0f);

        if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
            Helper.MainMenuIcon();
        ImGui.EndChild();
    }
}

