using Dalamud.Interface.Windowing;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows;

public partial class BuilderWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    public static ExcelSheet<SubmarineRank> RankSheet = null!;
    public static ExcelSheet<SubmarineMap> MapSheet = null!;
    public static ExcelSheet<SubmarineExplorationPretty> ExplorationSheet = null!;

    public Submarines.RouteBuild CurrentBuild = new();

    private string CurrentInput = "";

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

                ImGui.SameLine();

                ImGui.Button("Save");
                if (SaveBuild());

                ImGui.SameLine();

                ImGui.Button("Load");
                if (LoadBuild());
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

    private bool SaveBuild()
    {
        ImGui.SetNextWindowSize(new Vector2(200 * ImGuiHelpers.GlobalScale, 90 * ImGuiHelpers.GlobalScale));
        if (!ImGui.BeginPopupContextItem("##savePopup", ImGuiPopupFlags.None))
            return false;

        ImGui.BeginChild("SavePopupChild", Vector2.Zero, false);

        var ret = false;

        ImGuiHelpers.ScaledDummy(3.0f);
        ImGui.SetNextItemWidth(180 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##SavePopupName", "Name", ref CurrentInput, 128, ImGuiInputTextFlags.AutoSelectAll);
        ImGuiHelpers.ScaledDummy(3.0f);

        if (ImGui.Button("Save Build"))
        {
            // make sure that original sub hasn't changed in the future
            CurrentBuild.OriginalSub = 0;
            if (Configuration.SavedBuilds.TryAdd(CurrentInput, CurrentBuild))
            {
                Configuration.Save();
                ret = true;
            }
            else
            {
                if (ImGui.GetIO().KeyCtrl)
                {
                    Configuration.SavedBuilds[CurrentInput] = CurrentBuild;
                    Configuration.Save();
                    ret = true;
                }
            }

            if (!ret)
                Plugin.ChatGui.PrintError(Utils.ErrorMessage("Build with same name exists already."));
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Hold Control to overwrite");


        // ImGui issue #273849, children keep popups from closing automatically
        if (ret)
            ImGui.CloseCurrentPopup();

        ImGui.EndChild();
        ImGui.EndPopup();

        return ret;
    }

    private bool LoadBuild()
    {
        ImGui.SetNextWindowSize(new Vector2(0, 250 * ImGuiHelpers.GlobalScale));
        if (!ImGui.BeginPopupContextItem("##LoadPopup", ImGuiPopupFlags.None))
            return false;

        var longest = 0.0f;
        foreach (var (key, value) in Configuration.SavedBuilds)
        {
            var width = ImGui.CalcTextSize(Utils.FormattedRouteBuild(key, value)).X;
            if (width > longest)
                longest = width;
        }

        // set width + padding
        ImGui.Dummy(new Vector2(longest + (30.0f * ImGuiHelpers.GlobalScale), 0));

        ImGuiHelpers.ScaledDummy(3.0f);
        ImGui.TextColored(ImGuiColors.ParsedOrange, "Load by clicking");
        ImGui.Indent(5.0f);
        ImGui.BeginChild("LoadPopupChild", Vector2.Zero, false);

        var ret = false;

        foreach (var (key, value) in Configuration.SavedBuilds)
        {
            if (ImGui.Selectable(Utils.FormattedRouteBuild(key, value)))
            {
                CurrentBuild = value;
                if (CurrentBuild.Sectors.Any())
                {
                    var startPoint = Submarines.FindVoyageStartPoint(CurrentBuild.Sectors.First());
                    var points = CurrentBuild.Sectors.Prepend(startPoint).Select(ExplorationSheet.GetRow).ToList();
                    CurrentBuild.UpdateOptimized(Submarines.CalculateDistance(points!));
                }
                else
                {
                    CurrentBuild.NoOptimized();
                }
                ret = true;
            }
        }

        ImGui.Unindent(5.0f);

        // ImGui issue #273849, children keep popups from closing automatically
        if (ret)
            ImGui.CloseCurrentPopup();

        ImGui.EndChild();
        ImGui.EndPopup();

        return ret;
    }

    public void Reset()
    {
        CurrentBuild = Submarines.RouteBuild.Empty;
    }
}
