using Dalamud.Interface.Windowing;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    public Build.RouteBuild CurrentBuild = new();

    private string CurrentInput = "";

    public BuilderWindow(Plugin plugin) : base("Builder##SubmarineTracker")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(470, 750),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;

        InitializeShip();
        InitializeLeveling();
    }

    public void Dispose() { }

    public override void Draw()
    {
        var infoTabOpen = false;
        var shipTabOpen = false;

        var buttonHeight = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().WindowPadding.Y + Helper.GetSeparatorPaddingHeight;
        using (var child = ImRaii.Child("SubContent", new Vector2(0, -buttonHeight)))
        {
            if (child.Success)
            {
                var sub = new Submarine();

                using var tabBar = ImRaii.TabBar("SubBuilderTab");
                if (tabBar.Success)
                {
                    BuildTab(ref sub);

                    RouteTab();

                    ExpTab();

                    shipTabOpen |= ShipTab();

                    shipTabOpen |= LevelingTab();

                    shipTabOpen |= LootTab();

                    infoTabOpen |= InfoTab();
                }

                if (!infoTabOpen && !shipTabOpen)
                    BuildStats(ref sub);
            }
        }

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(Helper.SeparatorPadding);

        using (var child = ImRaii.Child("BottomBar", Vector2.Zero, false))
        {
            if (child.Success)
            {
                if (!infoTabOpen)
                {
                    using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.ParsedBlue))
                        if (ImGui.Button(Language.BuilderWindowButtonReset))
                            Reset();

                    ImGui.SameLine();

                    ImGui.Button(Language.BuilderWindowButtonSave);
                    SaveBuild();

                    ImGui.SameLine();

                    ImGui.Button(Language.BuilderWindowButtonLoad);
                    LoadBuild();
                }
                else
                {
                    using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.ParsedBlue))
                        if (ImGui.Button(Language.TermJoinDiscord))
                            Dalamud.Utility.Util.OpenLink("https://discord.gg/overseascasuals");

                    if (ImGui.IsItemHovered())
                        Helper.Tooltip(Language.OverseasDiscordTooltip);

                    ImGui.SameLine();

                    using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.ParsedGrey))
                        if (ImGui.Button(Language.TermSpreadsheet))
                            Dalamud.Utility.Util.OpenLink("https://gacha.infi.ovh/submarine");
                }

                Helper.MainMenuIcon();
            }
        }
    }

    private bool SaveBuild()
    {
        ImGui.SetNextWindowSize(new Vector2(200 * ImGuiHelpers.GlobalScale, 90 * ImGuiHelpers.GlobalScale));
        using var context = ImRaii.ContextPopupItem("##savePopup", ImGuiPopupFlags.None);
        if (!context.Success)
            return false;

        using var child = ImRaii.Child("SavePopupChild", Vector2.Zero, false);
        if (!child.Success)
            return false;

        var ret = false;
        ImGuiHelpers.ScaledDummy(3.0f);
        ImGui.SetNextItemWidth(180 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##SavePopupName", Language.TermsName, ref CurrentInput, 128, ImGuiInputTextFlags.AutoSelectAll);
        ImGuiHelpers.ScaledDummy(3.0f);

        if (ImGui.Button(Language.BuilderWindowButtonSaveBuild))
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
                Plugin.ChatGui.PrintError(Utils.ErrorMessage(Language.BuilderWindowErrorSameName));
        }

        if (ImGui.IsItemHovered())
            Helper.Tooltip(Language.BuilderWindowTooltipOverwrite);


        // ImGui issue #273849, children keep popups from closing automatically
        if (ret)
            ImGui.CloseCurrentPopup();

        return ret;
    }

    private bool LoadBuild()
    {
        ImGui.SetNextWindowSize(new Vector2(0, 250 * ImGuiHelpers.GlobalScale));
        using var context = ImRaii.ContextPopupItem("##LoadPopup", ImGuiPopupFlags.None);
        if (!context.Success)
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
        Helper.TextColored(ImGuiColors.ParsedOrange, Language.BuilderWindowTipLoading);

        using var indent = ImRaii.PushIndent(5.0f);
        using var child = ImRaii.Child("LoadPopupChild", Vector2.Zero, false);
        if (!child.Success)
            return false;

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

        // ImGui issue #273849, children keep popups from closing automatically
        if (ret)
            ImGui.CloseCurrentPopup();

        return ret;
    }

    public void Reset()
    {
        CurrentBuild = Build.RouteBuild.Empty;

        VoyageInterfaceSelection = 0;
        CacheValid = false;
    }
}
