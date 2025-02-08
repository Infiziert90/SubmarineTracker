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
        var bottomContentHeight = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().WindowPadding.Y + Helper.GetSeparatorPaddingHeight;
        using (var contentChild = ImRaii.Child("HelpyContent", new Vector2(0, -bottomContentHeight)))
        {
            if (contentChild.Success)
            {
                using var tabBar = ImRaii.TabBar("##HelperTabBar");
                if (tabBar.Success)
                {
                    ProgressionTab();

                    StorageTab();
                }
            }
        }

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(Helper.SeparatorPadding);

        using var bottomChild = ImRaii.Child("BottomBar", Vector2.Zero, false, 0);
        if (!bottomChild.Success)
            return;

        Helper.MainMenuIcon();
    }
}

