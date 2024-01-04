using Dalamud.Interface.Windowing;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;
    private static ExcelSheet<Item> ItemSheet = null!;

    public ConfigWindow(Plugin plugin) : base("Configuration##SubmarineTracker")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(450, 650),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = plugin.Configuration;
        ItemSheet = Plugin.Data.GetExcelSheet<Item>()!;

        InitializeLoot();
    }

    public void Dispose() { }

    public override void Draw()
    {
        var aboutOpen = false;
        var buttonHeight = ImGui.CalcTextSize("RRRR").Y + (20.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginChild("ConfigContent", new Vector2(0, -buttonHeight)))
        {
            if (ImGui.BeginTabBar("##ConfigTabBar"))
            {
                Tracker();

                Builder();

                Loot();

                Overlay();

                Notify();

                Manage();

                Upload();

                aboutOpen = About();

                ImGui.EndTabBar();
            }
        }
        ImGui.EndChild();

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(1.0f);

        if (ImGui.BeginChild("ConfigBottomBar", new Vector2(0, 0), false, 0))
        {
            if (aboutOpen)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedBlue);
                if (ImGui.Button(Loc.Localize("Menu - Discord Thread", "Discord Thread")))
                    Plugin.DiscordSupport();
                ImGui.PopStyleColor();

                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedBlue);
                if (ImGui.Button(Loc.Localize("Menu - Localization", "Localization")))
                    Plugin.LocHelp();
                ImGui.PopStyleColor();

                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.DPSRed);
                if (ImGui.Button(Loc.Localize("Menu - Issues", "Issues")))
                    Plugin.IssuePage();
                ImGui.PopStyleColor();

                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Button, Helper.CustomFullyDone);
                if (ImGui.Button(Loc.Localize("Menu - KoFi", "Ko-Fi Tip")))
                    Plugin.Kofi();
                ImGui.PopStyleColor();
            }

            Helper.MainMenuIcon();
        }
        ImGui.EndChild();
    }
}
