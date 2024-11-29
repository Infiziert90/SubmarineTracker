using System.Globalization;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace SubmarineTracker.Windows.Loot;

public partial class LootWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    private string Format = string.Empty;
    private static readonly Vector2 IconSize = new(28, 28);

    public LootWindow(Plugin plugin) : base("Custom Loot Overview##SubmarineTracker")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(370, 600),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        InitializeAnalyse();
    }

    public void Dispose() { }

    public override void Draw()
    {
        Format = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;

        var buttonHeight = ImGui.CalcTextSize("RRRR").Y + (20.0f * ImGuiHelpers.GlobalScale);
        using (var contentChild = ImRaii.Child("SubContent", new Vector2(0, -buttonHeight)))
        {
            if (contentChild.Success)
            {
                using var tabBar = ImRaii.TabBar("##LootTabBar");
                if (tabBar.Success)
                {
                    CustomLootTab();

                    VoyageTab();

                    RouteTab();

                    AnalyseTab();

                    ExportTab();
                }
            }
        }

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(1.0f);

        using var bottomChild = ImRaii.Child("BottomBar", new Vector2(0, 0), false, 0);
        if (!bottomChild.Success)
            return;

        Helper.MainMenuIcon();
    }
}
