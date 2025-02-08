using Dalamud.Interface.Windowing;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    public ConfigWindow(Plugin plugin) : base("Configuration##SubmarineTracker")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(450, 650),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;

        InitializeLoot();
        InitializeNotify();
    }

    public void Dispose() { }

    public override void Draw()
    {
        var aboutOpen = false;
        var bottomContentHeight = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().WindowPadding.Y + Helper.GetSeparatorPaddingHeight;
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
        ImGuiHelpers.ScaledDummy(Helper.SeparatorPadding);

        using var bottomChild = ImRaii.Child("ConfigBottomBar", Vector2.Zero, false, 0);
        if (!bottomChild.Success)
            return;

        if (aboutOpen)
        {
            using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.ParsedBlue))
                if (ImGui.Button(Language.MenuDiscordThread))
                    Plugin.DiscordSupport();

            ImGui.SameLine();

            using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.ParsedPurple))
                if (ImGui.Button(Language.MenuLocalization))
                    Plugin.LocHelp();

            ImGui.SameLine();

            using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.DPSRed))
                if (ImGui.Button(Language.MenuIssues))
                    Plugin.IssuePage();

            ImGui.SameLine();

            using (ImRaii.PushColor(ImGuiCol.Button, Helper.CustomFullyDone))
                if (ImGui.Button(Language.MenuKoFi))
                    Plugin.Kofi();
        }

        Helper.MainMenuIcon();
    }
}
