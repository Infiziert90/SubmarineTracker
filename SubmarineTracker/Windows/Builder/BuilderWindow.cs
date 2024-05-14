using Dalamud.Interface.Windowing;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    public readonly List<SubmarineRank> RankSheet;
    public readonly ExcelSheet<Item> ItemSheet;
    public readonly ExcelSheet<SubmarineMap> MapSheet;
    public readonly ExcelSheet<SubExplPretty> ExplorationSheet;

    public Build.RouteBuild CurrentBuild = new();

    private string CurrentInput = "";

    public BuilderWindow(Plugin plugin) : base("Builder##SubmarineTracker")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(470, 750),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;

        ItemSheet = Plugin.Data.GetExcelSheet<Item>()!;
        MapSheet = Plugin.Data.GetExcelSheet<SubmarineMap>()!;
        RankSheet = Plugin.Data.GetExcelSheet<SubmarineRank>()!.Where(t => t.Capacity != 0).ToList();
        ExplorationSheet = Plugin.Data.GetExcelSheet<SubExplPretty>()!;

        InitializeShip();
        InitializeLeveling();
    }

    public void Dispose() { }

    public override void Draw()
    {
        var infoTabOpen = false;
        var shipTabOpen = false;

        var buttonHeight = ImGui.CalcTextSize("RRRR").Y + (20.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginChild("SubContent", new Vector2(0, -buttonHeight)))
        {
            var sub = new Submarines.Submarine();

            if (ImGui.BeginTabBar("SubBuilderTab"))
            {
                BuildTab(ref sub);

                RouteTab();

                ExpTab();

                shipTabOpen |= ShipTab();

                shipTabOpen |= LevelingTab();

                shipTabOpen |= SearchTab();

                infoTabOpen |= InfoTab();

                ImGui.EndTabBar();
            }

            if (!infoTabOpen && !shipTabOpen)
            {
                BuildStats(ref sub);
            }
        }
        ImGui.EndChild();

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(1.0f);

        if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
        {
            if (!infoTabOpen)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedBlue);
                if (ImGui.Button(Loc.Localize("Builder Window Button - Reset","Reset")))
                    Reset();
                ImGui.PopStyleColor();

                ImGui.SameLine();

                ImGui.Button(Loc.Localize("Builder Window Button - Save","Save"));
                SaveBuild();

                ImGui.SameLine();

                ImGui.Button(Loc.Localize("Builder Window Button - Load","Load"));
                LoadBuild();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedBlue);
                if (ImGui.Button("Join Discord"))
                    Dalamud.Utility.Util.OpenLink("https://discord.gg/overseascasuals");
                ImGui.PopStyleColor();

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Discord: Overseas Casuals\nRecommended discord for submarines\nJust select the 'Subs' channel and\njoin us in '#you-dont-pay-my-sub'");

                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedGrey);
                if (ImGui.Button("Spreadsheet"))
                    Dalamud.Utility.Util.OpenLink("https://docs.google.com/spreadsheets/d/1eiuGRrQTjJ5n4ogybKOBxJRT7EiJLlX93fge-Z8MmD4");
                ImGui.PopStyleColor();
            }

            Helper.MainMenuIcon();
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
        ImGui.InputTextWithHint("##SavePopupName", Loc.Localize("Terms - Name", "Name"), ref CurrentInput, 128, ImGuiInputTextFlags.AutoSelectAll);
        ImGuiHelpers.ScaledDummy(3.0f);

        if (ImGui.Button(Loc.Localize("Builder Window Button - Save Build", "Save Build")))
        {
            // make sure that original sub hasn't changed in the future
            CurrentBuild.OriginalSub = 0;
            if (Plugin.Configuration.SavedBuilds.TryAdd(CurrentInput, CurrentBuild))
            {
                Plugin.Configuration.Save();
                ret = true;
            }
            else
            {
                if (ImGui.GetIO().KeyCtrl)
                {
                    Plugin.Configuration.SavedBuilds[CurrentInput] = CurrentBuild;
                    Plugin.Configuration.Save();
                    ret = true;
                }
            }

            if (!ret)
                Plugin.ChatGui.PrintError(Utils.ErrorMessage(Loc.Localize("Builder Window Error - Same Name", "Build with same name exists already.")));
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Loc.Localize("Builder Window Tooltip - Overwrite", "Hold Control to overwrite"));


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
        foreach (var (key, value) in Plugin.Configuration.SavedBuilds)
        {
            var width = ImGui.CalcTextSize(Utils.FormattedRouteBuild(key, value)).X;
            if (width > longest)
                longest = width;
        }

        // set width + padding
        ImGui.Dummy(new Vector2(longest + (30.0f * ImGuiHelpers.GlobalScale), 0));

        ImGuiHelpers.ScaledDummy(3.0f);
        ImGui.TextColored(ImGuiColors.ParsedOrange, Loc.Localize("Builder Window Tip - Loading", "Load by clicking"));
        ImGuiHelpers.ScaledIndent(5.0f);
        ImGui.BeginChild("LoadPopupChild", Vector2.Zero, false);

        var ret = false;

        foreach (var (key, value) in Plugin.Configuration.SavedBuilds)
        {
            if (ImGui.Selectable(Utils.FormattedRouteBuild(key, value)))
            {
                CurrentBuild = value;
                if (CurrentBuild.Sectors.Count != 0)
                    CurrentBuild.UpdateOptimized(Voyage.FindCalculatedRoute(CurrentBuild.Sectors.ToArray()));
                else
                    CurrentBuild.NotOptimized();
                ret = true;
            }
        }

        ImGuiHelpers.ScaledIndent(-5.0f);

        // ImGui issue #273849, children keep popups from closing automatically
        if (ret)
            ImGui.CloseCurrentPopup();

        ImGui.EndChild();
        ImGui.EndPopup();

        return ret;
    }

    public void Reset()
    {
        CurrentBuild = Build.RouteBuild.Empty;

        VoyageInterfaceSelection = 0;
        CacheValid = false;
    }
}
