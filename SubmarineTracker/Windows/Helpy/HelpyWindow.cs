using Dalamud.Interface.Windowing;
using Lumina.Excel;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Helpy;

public partial class HelpyWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    private static ExcelSheet<SubExplPretty> ExplorationSheet = null!;

    public HelpyWindow(Plugin plugin) : base("Helpy##SubmarineTracker")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(720, 520),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;

        ExplorationSheet = Plugin.Data.GetExcelSheet<SubExplPretty>()!;

        InitProgression();
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (!Plugin.DatabaseCache.GetFreeCompanies().TryGetValue(Plugin.GetFCId, out var fcSub))
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

