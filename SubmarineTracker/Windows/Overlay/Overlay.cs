using Dalamud.Interface.Windowing;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Overlay;

// Inspired by Accountant Plugin from Ottermandias
public class OverlayWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    private readonly Vector4 TransparentBackground = new(0.0f, 0.0f, 0.0f, 0.8f);
    private readonly Vector4 CustomFullyDone = new(0.12549f, 0.74902f, 0.33333f, 0.6f);
    private readonly Vector4 CustomPartlyDone = new(1.0f, 0.81569f, 0.27451f, 0.6f);
    private readonly Vector4 CustomOnRoute = new(0.85882f, 0.22745f, 0.20392f, 0.6f);

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

        ImGui.PushStyleColor(ImGuiCol.WindowBg, TransparentBackground);
    }

    public override void Draw()
    {
        Plugin.EnsureFCOrderSafety();

        Submarines.Submarine? timerSub = null;
        foreach (var fc in Submarines.KnownSubmarines.Values)
        {
            var timer = fc.GetLongestReturn();
            if (timerSub == null || timer.ReturnTime > timerSub.ReturnTime)
                timerSub = timer;
        }

        if (timerSub == null)
            return;

        var scrollbarSpacing = ImGui.GetScrollMaxY() > 0.0f ? ImGui.GetStyle().ScrollbarSize : 0;
        var windowWidth = ImGui.GetWindowWidth() - (20.0f * ImGuiHelpers.GlobalScale) - scrollbarSpacing;
        var y = ImGui.GetCursorPosY();
        ImGui.PushStyleColor(ImGuiCol.Header, VoyageStats is { Done: > 0, OnRoute: > 0 }
                                                  ? CustomPartlyDone : VoyageStats.OnRoute == 0
                                                      ? CustomFullyDone : CustomOnRoute);
        var mainHeader = ImGui.CollapsingHeader("All###overlayAll");
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
            var longestSub = fc.GetLongestReturn();

            ImGui.PushStyleColor(ImGuiCol.Header, longestSub.IsDone() ? CustomFullyDone : anySubDone ? CustomPartlyDone : CustomOnRoute);
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

                var timeText = Helper.GenerateVoyageText(sub);
                var timeWidth = ImGui.CalcTextSize(timeText).X;
                ImGui.SameLine(windowWidth - timeWidth);
                ImGui.TextUnformatted(timeText);

            }
            ImGui.Columns(1);
            ImGui.Unindent(10.0f);
        }
        ImGui.Unindent(10.0f);
    }

    public override void OnClose()
    {
        Configuration.OverlayOpen = false;
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor();
    }

    public void SetHeaderText(Submarines.Submarine sub, float windowWidth, float lastY)
    {
        var cursorPos = ImGui.GetCursorPos();
        var longestText = Helper.GenerateVoyageText(sub);
        var longestWidth = ImGui.CalcTextSize(longestText).X;
        ImGui.SetCursorPos(new Vector2(windowWidth - longestWidth, lastY));
        ImGui.AlignTextToFramePadding();
        ImGui.Text(longestText);
        ImGui.SetCursorPos(cursorPos);
    }
}
