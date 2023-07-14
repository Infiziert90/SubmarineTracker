using Dalamud.Interface.Windowing;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Overlay;

// Inspired by Accountant Plugin from Ottermandias
public class OverlayWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    private (int OnRoute, int Done, int Halt) VoyageStats = (0, 0, 0);

    public OverlayWindow(Plugin plugin, Configuration configuration) : base("Submarines: 0|0|0###submarineOverlay")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 140),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        RespectCloseHotkey = false;

        Plugin = plugin;
        Configuration = configuration;
    }

    public void Dispose() { }

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
        WindowName = $"Submarines: {VoyageStats.Done} | {VoyageStats.Halt} | {VoyageStats.OnRoute}###submarineOverlay";

        ImGui.PushStyleColor(ImGuiCol.WindowBg, Helper.TransparentBackground);
    }

    public override void Draw()
    {
        Plugin.EnsureFCOrderSafety();

        var showLast = !Configuration.OverlayFirstReturn;
        Submarines.Submarine? timerSub = null;
        foreach (var fc in Submarines.KnownSubmarines.Values)
        {
            var timer = showLast ? fc.GetLastReturn() : fc.GetFirstReturn();
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

        ImGui.Indent(10.0f);
        foreach (var id in Configuration.FCOrder)
        {
            var fc = Submarines.KnownSubmarines[id];
            if (!fc.Submarines.Any())
                continue;

            y = ImGui.GetCursorPosY();
            var anySubDone = fc.Submarines.Any(s => s.IsDone());
            var longestSub = showLast ? fc.GetLastReturn() : fc.GetFirstReturn();

            ImGui.PushStyleColor(ImGuiCol.Header, longestSub.IsDone() ? Helper.CustomFullyDone : anySubDone ? Helper.CustomPartlyDone : Helper.CustomOnRoute);
            var header = ImGui.CollapsingHeader($"{Helper.BuildNameHeader(fc, Configuration.UseCharacterName)}###overlayFC{id}");
            ImGui.PopStyleColor();

            SetHeaderText(longestSub, windowWidth, y);

            if (!header)
                continue;

            ImGui.Indent(10.0f);
            foreach (var sub in fc.Submarines)
            {
                var notNeedsRepair = sub.PredictDurability() > 0;
                ImGui.TextColored(notNeedsRepair ? ImGuiColors.TankBlue : ImGuiColors.DalamudYellow, sub.Name);

                if (!notNeedsRepair && ImGui.IsItemHovered())
                    ImGui.SetTooltip("This submarine will need repairs");

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
        Configuration.OverlayOpen = true;
        Configuration.Save();
    }

    public override void OnClose()
    {
        Configuration.OverlayOpen = false;
        Configuration.Save();
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
