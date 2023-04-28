using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;
using System;
using System.Numerics;

namespace SubmarineTracker.Windows;

public partial class BuilderWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    public static ExcelSheet<SubmarineRank> RankSheet = null!;
    public static ExcelSheet<SubmarineMap> MapSheet = null!;
    public static ExcelSheet<SubmarineExplorationPretty> ExplorationSheet = null!;

    public Submarines.RouteBuild CurrentBuild = new();

    public BuilderWindow(Plugin plugin, Configuration configuration) : base("Builder")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 650),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = configuration;

        RankSheet = Plugin.Data.GetExcelSheet<SubmarineRank>()!;
        MapSheet = Plugin.Data.GetExcelSheet<SubmarineMap>()!;
        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExplorationPretty>()!;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var infoTabOpen = false;

        var buttonHeight = ImGui.CalcTextSize("XXX").Y + 10.0f;
        if (ImGui.BeginChild("SubContent", new Vector2(0, -(buttonHeight + (30.0f * ImGuiHelpers.GlobalScale)))))
        {
            var sub = new Submarines.Submarine();

            if (ImGui.BeginTabBar("SubBuilderTab"))
            {
                BuildTab(ref sub);

                RouteTab();

                ExpTab();

                infoTabOpen |= InfoTab();
            }
            ImGui.EndTabBar();

            if (!infoTabOpen)
            {
                BuildStats(ref sub);
            }
        }
        ImGui.EndChild();

        ImGuiHelpers.ScaledDummy(5);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);

        if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
        {
            if (!infoTabOpen)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedBlue);
                if (ImGui.Button("Reset"))
                    Reset();
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedBlue);
                if (ImGui.Button("Submarine Discord"))
                    Dalamud.Utility.Util.OpenLink("https://discord.gg/GAVegXNtwK");
                ImGui.PopStyleColor();

                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedGrey);
                if (ImGui.Button("Spreadsheet"))
                    Dalamud.Utility.Util.OpenLink("https://docs.google.com/spreadsheets/d/1-j0a-I7bQdjnXkplP9T4lOLPH2h3U_-gObxAiI4JtpA/edit#gid=1894926908");
                ImGui.PopStyleColor();
            }
        }
        ImGui.EndChild();
    }

    public void Reset()
    {
        CurrentBuild = Submarines.RouteBuild.Empty;
    }
}
