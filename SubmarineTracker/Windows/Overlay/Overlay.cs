using Dalamud.Interface.Windowing;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Overlay;

// Inspired by Accountant Plugin from Ottermandias
public class OverlayWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    private readonly Vector4 TransparentBackground = new(0.0f, 0.0f, 0.0f, 0.8f);
    private readonly Vector4 TransparentCustomGreen = new(0.957f, 0.635f, 0.38f, 0.6f);
    private readonly Vector4 TransparentCustomCyan = new(0.165f, 0.616f, 0.561f, 0.6f);

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
        (int OnRoute, int Done, int Halt) voyageStats = (0, 0, 0);
        foreach (var sub in Submarines.KnownSubmarines.Values.SelectMany(fc => fc.Submarines))
        {
            if (sub.IsOnVoyage())
            {
                if (sub.IsDone())
                    voyageStats.Done += 1;
                else
                    voyageStats.OnRoute += 1;

                continue;
            }

            voyageStats.Halt += 1;
        }
        WindowName = $"Submarines: {voyageStats.Done} | {voyageStats.Halt} | {voyageStats.OnRoute}###submarineOverlay";

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

        var windowWidth = ImGui.GetWindowWidth() - (20.0f * ImGuiHelpers.GlobalScale);
        var y = ImGui.GetCursorPosY();
        ImGui.PushStyleColor(ImGuiCol.Header, timerSub.IsDone() ? TransparentCustomGreen : TransparentCustomCyan);
        var mainHeader = ImGui.CollapsingHeader("All");
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
            var longestSub = fc.GetLongestReturn();

            ImGui.PushStyleColor(ImGuiCol.Header, longestSub.IsDone() ? TransparentCustomGreen : TransparentCustomCyan);
            var header = ImGui.CollapsingHeader($"{Helper.BuildNameHeader(fc, Configuration.UseCharacterName)}##{id}");
            ImGui.PopStyleColor();

            SetHeaderText(longestSub, windowWidth, y);

            if (!header)
                continue;

            ImGui.Indent(10.0f);
            foreach (var sub in fc.Submarines)
            {
                ImGui.TextColored(sub.PredictDurability() > 0
                                      ? ImGuiColors.TankBlue
                                      : ImGuiColors.DalamudYellow, sub.Name);

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
