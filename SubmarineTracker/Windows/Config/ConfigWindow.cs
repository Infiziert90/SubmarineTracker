using Dalamud.Interface.Windowing;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    private const float SeparatorPadding = 1.0f;
    private static float GetSeparatorPaddingHeight => SeparatorPadding * ImGuiHelpers.GlobalScale;

    public ConfigWindow(Plugin plugin) : base("Configuration##SubmarineTracker")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(450, 650),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;

        InitializeLoot();
    }

    public void Dispose() { }

    public override void Draw()
    {
        var aboutOpen = false;
        var bottomContentHeight = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().WindowPadding.Y + GetSeparatorPaddingHeight;
        using (var contentChild = ImRaii.Child("ConfigContent", new Vector2(0, -bottomContentHeight)))
        {
            if (contentChild.Success)
            {
                using var tabBar = ImRaii.TabBar("##ConfigTabBar");
                if (tabBar.Success)
                {
                    General();

                    Tracker();

                    Builder();

                    Loot();

                    Overlay();

                    Notify();

                    Manage();

                    aboutOpen = About();
                }
            }
        }

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(SeparatorPadding);

        using var bottomChild = ImRaii.Child("ConfigBottomBar", new Vector2(0, 0), false, 0);
        if (!bottomChild.Success)
            return;

        if (aboutOpen)
        {
            using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.ParsedBlue))
            {
                if (ImGui.Button(Loc.Localize("Menu - Discord Thread", "Discord Thread")))
                    Plugin.DiscordSupport();
            }

            ImGui.SameLine();

            using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.ParsedPurple))
            {
                if (ImGui.Button(Loc.Localize("Menu - Localization", "Localization")))
                    Plugin.LocHelp();
            }

            ImGui.SameLine();

            using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.DPSRed))
            {
                if (ImGui.Button(Loc.Localize("Menu - Issues", "Issues")))
                    Plugin.IssuePage();
            }

            ImGui.SameLine();

            using (ImRaii.PushColor(ImGuiCol.Button, Helper.CustomFullyDone))
            {
                if (ImGui.Button(Loc.Localize("Menu - KoFi", "Ko-Fi Tip")))
                    Plugin.Kofi();
            }
        }

        Helper.MainMenuIcon();
    }
}
