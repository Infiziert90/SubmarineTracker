using System.Globalization;
using Dalamud.Interface.Windowing;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace SubmarineTracker.Windows.Loot;

public partial class LootWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    private static ExcelSheet<Item> ItemSheet = null!;
    private static ExcelSheet<SubmarineExplorationPretty> ExplorationSheet = null!;

    private int SelectedSubmarine;
    private int SelectedVoyage;

    private static Vector2 IconSize = new(28, 28);

    private string Format = string.Empty;

    public LootWindow(Plugin plugin, Configuration configuration) : base("Custom Loot Overview")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(370, 570),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = configuration;

        ItemSheet = Plugin.Data.GetExcelSheet<Item>()!;
        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExplorationPretty>()!;

        InitializeAnalyse();
    }

    public void Dispose() { }

    public override void Draw()
    {
        Plugin.FileDialogManager.Draw();
        Format = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;

        var buttonHeight = ImGui.CalcTextSize("RRRR").Y + (20.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginChild("SubContent", new Vector2(0, -buttonHeight)))
        {
            if (ImGui.BeginTabBar("##LootTabBar"))
            {
                CustomLootTab();

                VoyageTab();

                AnalyseTab();

                ExportTab();
            }
            ImGui.EndTabBar();
        }
        ImGui.EndChild();

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(1.0f);

        if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
        {
            Helper.MainMenuIcon(Plugin);
        }
        ImGui.EndChild();
    }
}
