using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows;

public partial class BuilderWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    public static ExcelSheet<SubmarineRank> RankSheet = null!;
    public static ExcelSheet<SubmarinePart> PartSheet = null!;
    public static ExcelSheet<SubmarineMap> MapSheet = null!;
    public static ExcelSheet<SubmarineExploration> ExplorationSheet = null!;

    public int SelectSub;
    public int SelectedRank = 1;
    public int SelectedHull = 3;
    public int SelectedStern = 4;
    public int SelectedBow = 1;
    public int SelectedBridge = 2;

    public int SelectedMap;
    public List<uint> SelectedLocations = new();
    public (int Distance, List<uint> Points) OptimizedRoute = (0, new List<uint>());

    public int OrgSpeed;

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
        PartSheet = Plugin.Data.GetExcelSheet<SubmarinePart>()!;
        MapSheet = Plugin.Data.GetExcelSheet<SubmarineMap>()!;
        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExploration>()!;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var infoTabOpen = false;

        if (ImGui.BeginChild("SubContent", new Vector2(0, -50)))
        {
            var sub = new Submarines.Submarine();

            if (ImGui.BeginTabBar("SubBuilderTab"))
            {
                BuildTab(ref sub);

                RouteTab();

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

    public SubmarinePart GetPart(int partId) => PartSheet.GetRow((uint) partId)!;

    public void Reset()
    {
        SelectedRank = 1;
        SelectedHull = 3;
        SelectedStern = 4;
        SelectedBow = 1;
        SelectedBridge = 2;

        SelectedMap = 0;
        SelectedLocations.Clear();

        OrgSpeed = 0;
    }
}
