using Dalamud.Interface.Windowing;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;
    private static ExcelSheet<Item> ItemSheet = null!;

    private static readonly ExcelSheetSelector.ExcelSheetPopupOptions<Item> ItemPopupOptions = new()
    {
        FormatRow = a => a.RowId switch { _ => $"[#{a.RowId}] {ToStr(a.Name)}" },
        FilteredSheet = Plugin.Data.GetExcelSheet<Item>()!.Skip(1).Where(i => ToStr(i.Name) != "")
    };

    public ConfigWindow(Plugin plugin) : base("Configuration")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(320, 460),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.Plugin = plugin;
        this.Configuration = plugin.Configuration;
        ItemSheet = Plugin.Data.GetExcelSheet<Item>()!;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("##ConfigTabBar"))
        {
            Tracker();

            Builder();

            Loot();

            Notify();

            Order();

            About();
        }
        ImGui.EndTabBar();
    }
}
