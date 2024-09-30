using System.IO;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace SubmarineTracker.Windows.Overlays;

// Inspired by Accountant from Ottermandias
public class ReturnOverlay : Window, IDisposable
{
    private readonly Plugin Plugin;

    private (int OnRoute, int Done, int Halt) VoyageStats = (0, 0, 0);

    public ReturnOverlay(Plugin plugin) : base("Submarines: 0|0|0###submarineOverlay")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 140),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        RespectCloseHotkey = false;
        DisableWindowSounds = true;

        Plugin = plugin;
    }

    public void Dispose() { }

    public override void Update()
    {
        Flags = (Plugin.Configuration.OverlayLockLocation ? ImGuiWindowFlags.NoMove : 0) | (Plugin.Configuration.OverlayLockSize ? ImGuiWindowFlags.NoResize : 0);
    }

    public override void PreOpenCheck()
    {
        if (Plugin.Configuration.OverlayHoldClosed && !Plugin.DatabaseCache.GetSubmarines().Any(sub => sub.IsDone()))
            IsOpen = false;
    }

    public override void PreDraw()
    {
        VoyageStats = (0, 0, 0);
        var nextSub = new Submarine(Plugin.Configuration.OverlayFirstReturn ? uint.MaxValue : 0);
        foreach (var sub in Plugin.DatabaseCache.GetSubmarines())
        {
            if (sub.IsOnVoyage())
            {
                if (sub.IsDone())
                {
                    VoyageStats.Done += 1;
                }
                else
                {
                    VoyageStats.OnRoute += 1;
                    if (!Plugin.Configuration.OverlayTitleTime)
                        continue;

                    if (Plugin.Configuration.OverlayFirstReturn)
                    {
                        if (nextSub.Return > sub.Return)
                            nextSub = sub;
                    }
                    else
                    {
                        if (nextSub.Return < sub.Return)
                            nextSub = sub;
                    }
                }

                continue;
            }

            VoyageStats.Halt += 1;
        }

        var returnText = "";
        if (Plugin.Configuration.OverlayTitleTime)
        {
            if (Plugin.Configuration.OverlayFirstReturn)
            {
                if (nextSub.Return < uint.MaxValue)
                    returnText = $"   -   {(nextSub.IsDone() ? "Done" : Utils.ToTime(nextSub.LeftoverTime()))}";
            }
            else
            {
                if (nextSub.Return > 0)
                    returnText = $"   -   {Utils.ToTime(nextSub.LeftoverTime())}";
            }
        }

        WindowName = $"{Loc.Localize("Terms - Submarines", "Submarines")}: {VoyageStats.Done} | {VoyageStats.Halt} | {VoyageStats.OnRoute}{returnText}###submarineOverlay";

        ImGui.PushStyleColor(ImGuiCol.WindowBg, Helper.TransparentBackground);
    }

    public override void Draw()
    {
        var showLast = !Plugin.Configuration.OverlayFirstReturn;

        Submarine? timerSub = null;
        foreach (var fc in Plugin.DatabaseCache.GetFreeCompanies().Keys)
        {
            var subs = Plugin.DatabaseCache.GetSubmarines(fc);
            var timer = showLast ? subs.MaxBy(s => s.Return) : subs.MinBy(s => s.Return);
            if (timer == null)
                continue;

            if (timerSub == null || (showLast ? timer.ReturnTime > timerSub.ReturnTime : timer.ReturnTime < timerSub.ReturnTime))
                timerSub = timer;
        }

        if (timerSub == null)
            return;

        var scrollbarSpacing = ImGui.GetScrollMaxY() > 0.0f ? ImGui.GetStyle().ScrollbarSize : 0;
        var windowWidth = ImGui.GetWindowWidth() - (20.0f * ImGuiHelpers.GlobalScale) - scrollbarSpacing;
        var y = ImGui.GetCursorPosY();

        var color = VoyageStats is { Done: > 0, OnRoute: > 0 }
                        ? Plugin.Configuration.OverlayPartlyDone : VoyageStats.OnRoute == 0
                            ? Plugin.Configuration.OverlayAllDone : Plugin.Configuration.OverlayNoneDone;
        bool mainHeader;
        using (ImRaii.PushColor(ImGuiCol.Header, color))
        {
            mainHeader = ImGui.CollapsingHeader("All###overlayAll", ImGuiTreeNodeFlags.DefaultOpen);
        }

        SetHeaderText(timerSub, windowWidth, y);

        if (!mainHeader)
            return;

        Plugin.EnsureFCOrderSafety();
        var fcList = Plugin.GetFCOrderWithoutHidden().Select(id => (Plugin.DatabaseCache.GetFreeCompanies()[id], Plugin.DatabaseCache.GetSubmarines(id))).Where(tuple => tuple.Item2.Length != 0);
        if (Plugin.Configuration.OverlaySortReverse)
            fcList = fcList.OrderByDescending(tuple => tuple.Item2.Min(s => s.Return));
        else if (Plugin.Configuration.OverlaySort)
            fcList = fcList.OrderBy(tuple => tuple.Item2.Min(s => s.Return));

        if (Plugin.Configuration.OverlayOnlyReturned)
            fcList = fcList.Where(tuple => tuple.Item2.Any(s => s.IsDone()));


        var sortedFcList = fcList.ToArray();
        if (sortedFcList.Length == 0)
        {
            using var indent = ImRaii.PushIndent(10.0f);
            ImGui.TextColored(ImGuiColors.DalamudOrange,Loc.Localize("Return Overlay Info - No Return", "No sub has returned."));
            return;
        }

        using var outerIndent = ImRaii.PushIndent(10.0f);
        foreach (var (fc, subs) in sortedFcList)
        {
            y = ImGui.GetCursorPosY();
            var anySubDone = subs.Any(s => s.IsDone());
            var longestSub = showLast ? subs.MaxBy(s => s.Return) : subs.MinBy(s => s.Return);

            if (longestSub == null)
                continue;

            bool header;
            using (ImRaii.PushColor(ImGuiCol.Header, longestSub.IsDone() ? Plugin.Configuration.OverlayAllDone : anySubDone ? Plugin.Configuration.OverlayPartlyDone : Plugin.Configuration.OverlayNoneDone))
            {
                header = ImGui.CollapsingHeader($"{Plugin.NameConverter.GetName(fc)}###overlayFC{fc.FreeCompanyId}");
            }

            SetHeaderText(longestSub, windowWidth, y);

            if (!header)
                continue;

            using var innerIndent = ImRaii.PushIndent(10.0f);
            foreach (var sub in subs)
            {
                var needsRepair = sub.PredictDurability() <= 0;
                var subText = $"{(Plugin.Configuration.OverlayShowRank ? $"{Loc.Localize("Terms - Rank", "Rank")} {sub.Rank}. " : "")}{Plugin.NameConverter.GetJustSub(sub)}{(Plugin.Configuration.OverlayShowBuild ? $" ({sub.Build.FullIdentifier()})" : "")}";
                ImGui.TextColored(!needsRepair ? ImGuiColors.TankBlue : ImGuiColors.DalamudYellow, subText);

                if (needsRepair && ImGui.IsItemHovered())
                    ImGui.SetTooltip(Loc.Localize("Return Overlay Tooltip - Repair Needed", "This submarine needs repair on return."));

                var timeText = Helper.GenerateVoyageText(sub, !Plugin.Configuration.OverlayShowDate);
                var timeWidth = ImGui.CalcTextSize(timeText).X;
                ImGui.SameLine(windowWidth - timeWidth);
                ImGui.TextUnformatted(timeText);

            }
            ImGui.Columns(1);
        }
    }

    public override void OnOpen()
    {
        try
        {
            Plugin.Configuration.OverlayOpen = true;
            Plugin.Configuration.Save();
        }
        catch (IOException)
        {
            // We just ignore
        }
    }

    public override void OnClose()
    {
        try
        {
            Plugin.Configuration.OverlayOpen = false;
            Plugin.Configuration.Save();
        }
        catch (IOException)
        {
            // We just ignore
        }
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor();
    }

    public static void SetHeaderText(Submarine sub, float windowWidth, float lastY)
    {
        var cursorPos = ImGui.GetCursorPos();
        var longestText = Helper.GenerateVoyageText(sub, !Plugin.Configuration.OverlayShowDate);
        var longestWidth = ImGui.CalcTextSize(longestText).X;
        ImGui.SetCursorPos(new Vector2(windowWidth - longestWidth, lastY));
        ImGui.AlignTextToFramePadding();
        ImGui.Text(longestText);
        ImGui.SetCursorPos(cursorPos);
    }
}
