using System.IO;
using Dalamud.Interface.Windowing;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Overlays;

// Inspired by Accountant from Ottermandias
public class ReturnOverlay : Window, IDisposable
{
    private readonly Plugin Plugin;
    private readonly Configuration Configuration;

    private (int OnRoute, int Done, int Halt) VoyageStats = (0, 0, 0);

    public ReturnOverlay(Plugin plugin, Configuration configuration) : base("Submarines: 0|0|0###submarineOverlay")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 140),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        RespectCloseHotkey = false;
        DisableWindowSounds = true;

        Plugin = plugin;
        Configuration = configuration;
    }

    public void Dispose() { }

    public override void Update()
    {
        Flags = (Configuration.OverlayLockLocation ? ImGuiWindowFlags.NoMove : 0) | (Configuration.OverlayLockSize ? ImGuiWindowFlags.NoResize : 0);


    }

    public override void PreOpenCheck()
    {
        if (Configuration.OverlayHoldClosed && !Submarines.KnownSubmarines.Values.Any(fc => fc.AnySubDone()))
            IsOpen = false;
    }

    public override void PreDraw()
    {
        VoyageStats = (0, 0, 0);
        foreach (var sub in Submarines.KnownSubmarines.Values.SelectMany(fc => fc.Submarines))
        {
            if (sub.IsOnVoyage())
            {
                if (sub.IsDone())
                    VoyageStats.Done += 1;
                else
                    VoyageStats.OnRoute += 1;

                continue;
            }

            VoyageStats.Halt += 1;
        }
        WindowName = $"{Loc.Localize("Terms - Submarines", "Submarines")}: {VoyageStats.Done} | {VoyageStats.Halt} | {VoyageStats.OnRoute}###submarineOverlay";

        ImGui.PushStyleColor(ImGuiCol.WindowBg, Helper.TransparentBackground);
    }

    public override void Draw()
    {
        var showLast = !Configuration.OverlayFirstReturn;
        Submarines.Submarine? timerSub = null;
        foreach (var fc in Submarines.KnownSubmarines.Values)
        {
            var timer = showLast ? fc.GetLastReturn() : fc.GetFirstReturn();
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
        ImGui.PushStyleColor(ImGuiCol.Header, VoyageStats is { Done: > 0, OnRoute: > 0 }
                                                  ? Helper.CustomPartlyDone : VoyageStats.OnRoute == 0
                                                      ? Helper.CustomFullyDone : Helper.CustomOnRoute);
        var mainHeader = ImGui.CollapsingHeader("All###overlayAll", ImGuiTreeNodeFlags.DefaultOpen);
        ImGui.PopStyleColor();

        SetHeaderText(timerSub, windowWidth, y);

        if (!mainHeader)
            return;


        Plugin.EnsureFCOrderSafety();
        var fcList = Configuration.FCOrder.Select(id => Submarines.KnownSubmarines[id]).Where(fc => fc.Submarines.Any());
        if (Configuration.OverlaySortReverse)
            fcList = fcList.OrderByDescending(fc => fc.ReturnTimes().Min());
        else if (Configuration.OverlaySort)
            fcList = fcList.OrderBy(fc => fc.ReturnTimes().Min());

        if (Configuration.OverlayOnlyReturned)
            fcList = fcList.Where(fc => fc.AnySubDone());


        var sortedFcList = fcList.ToArray();
        if (!sortedFcList.Any())
        {
            ImGui.Indent(10.0f);
            ImGui.TextColored(ImGuiColors.DalamudOrange,Loc.Localize("Return Overlay Info - No Return", "No sub has returned."));
            ImGui.Unindent(10.0f);
            return;
        }

        ImGui.Indent(10.0f);
        foreach (var fc in sortedFcList)
        {
            y = ImGui.GetCursorPosY();
            var anySubDone = fc.Submarines.Any(s => s.IsDone());
            var longestSub = showLast ? fc.GetLastReturn() : fc.GetFirstReturn();

            if (longestSub == null)
                continue;

            ImGui.PushStyleColor(ImGuiCol.Header, longestSub.IsDone() ? Helper.CustomFullyDone : anySubDone ? Helper.CustomPartlyDone : Helper.CustomOnRoute);
            var header = ImGui.CollapsingHeader($"{Helper.GetOverlayName(fc)}###overlayFC{fc.Submarines.First().Register}");
            ImGui.PopStyleColor();

            SetHeaderText(longestSub, windowWidth, y);

            if (!header)
                continue;

            ImGui.Indent(10.0f);
            foreach (var sub in fc.Submarines)
            {
                var needsRepair = sub.PredictDurability() <= 0;
                var subText = $"{(Configuration.OverlayShowRank ? $"{Loc.Localize("Terms - Rank", "Rank")} {sub.Rank}. " : "")}{sub.Name}{(Configuration.OverlayShowBuild ? $" ({sub.Build.FullIdentifier()})" : "")}";
                ImGui.TextColored(!needsRepair ? ImGuiColors.TankBlue : ImGuiColors.DalamudYellow, subText);

                if (needsRepair && ImGui.IsItemHovered())
                    ImGui.SetTooltip(Loc.Localize("Return Overlay Tooltip - Repair Needed", "This submarine needs repair on return."));

                var timeText = Helper.GenerateVoyageText(sub, !Configuration.OverlayShowDate);
                var timeWidth = ImGui.CalcTextSize(timeText).X;
                ImGui.SameLine(windowWidth - timeWidth);
                ImGui.TextUnformatted(timeText);

            }
            ImGui.Columns(1);
            ImGui.Unindent(10.0f);
        }
        ImGui.Unindent(10.0f);
    }

    public override void OnOpen()
    {
        try
        {
            Configuration.OverlayOpen = true;
            Configuration.Save();
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
            Configuration.OverlayOpen = false;
            Configuration.Save();
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

    public void SetHeaderText(Submarines.Submarine sub, float windowWidth, float lastY)
    {
        var cursorPos = ImGui.GetCursorPos();
        var longestText = Helper.GenerateVoyageText(sub, !Configuration.OverlayShowDate);
        var longestWidth = ImGui.CalcTextSize(longestText).X;
        ImGui.SetCursorPos(new Vector2(windowWidth - longestWidth, lastY));
        ImGui.AlignTextToFramePadding();
        ImGui.Text(longestText);
        ImGui.SetCursorPos(cursorPos);
    }
}
