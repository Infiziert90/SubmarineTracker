using Dalamud.Interface.Windowing;

namespace SubmarineTracker.Windows.Helpy;

public partial class HelpyWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    public HelpyWindow(Plugin plugin) : base("Helpy##SubmarineTracker")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(720, 520),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;

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
        using (var contentChild = ImRaii.Child("HelpyContent", new Vector2(0, -buttonHeight)))
        {
            if (contentChild.Success)
            {
                using var tabBar = ImRaii.TabBar("##HelperTabBar");
                if (tabBar.Success)
                {
                    ProgressionTab(fcSub);

                    StorageTab();
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

