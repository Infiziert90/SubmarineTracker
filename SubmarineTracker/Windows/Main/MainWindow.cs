using Dalamud.Interface.Windowing;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Main;

public partial class MainWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    private ulong CurrentSelection = 1;
    private static readonly Vector2 IconSize = new(28, 28);
    private static readonly int MaxLength = "Heavens' Eye Materia III".Length;

    public MainWindow(Plugin plugin) : base("Tracker##SubmarineTracker")
    {
        Plugin = plugin;

        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(800, 550),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGuiHelpers.ScaledDummy(5.0f);

        if (Plugin.DatabaseCache.GetSubmarines().Length == 0)
        {
            Helper.NoData();
            return;
        }

        var bottomContentHeight = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().WindowPadding.Y + Helper.GetSeparatorPaddingHeight;
        using (var subContent = ImRaii.Child("##subContent", new Vector2(0, -bottomContentHeight)))
        {
            if (subContent.Success)
            {
                Plugin.EnsureFCOrderSafety();
                var style = ImGui.GetStyle();

                var width = 0.0f;
                var lastCheckedLength = 0;
                foreach (var fc in Plugin.DatabaseCache.GetFreeCompanies().Values)
                {
                    var text = Plugin.NameConverter.GetName(fc);
                    if (text.Length <= lastCheckedLength)
                        continue;

                    lastCheckedLength = text.Length;
                    width = ImGui.CalcTextSize(text).X + (style.ItemSpacing.X * 2);
                }

                ImGui.Columns(2, "columns", true);
                if (!Plugin.Configuration.UserResize)
                    ImGui.SetColumnWidth(0, width + (20 * ImGuiHelpers.GlobalScale));
                else
                    width = ImGui.GetContentRegionAvail().X;

                using (var fcList = ImRaii.Child("##fcList"))
                {
                    if (fcList.Success)
                    {
                        if (!(Plugin.Configuration.ShowAll && CurrentSelection == 1))
                            if (!Plugin.DatabaseCache.GetFreeCompanies().ContainsKey(CurrentSelection))
                                CurrentSelection = Plugin.GetFCOrderWithoutHidden().First();

                        var current = CurrentSelection;
                        if (Plugin.Configuration.ShowAll)
                        {
                            using var buttonColor = ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.ParsedPink, current == 1);
                            if (ImGui.Button(Language.TermsAll, new Vector2(width, 0)))
                                CurrentSelection = 1;
                        }

                        var fcs = Plugin.DatabaseCache.GetFreeCompanies();
                        foreach (var key in Plugin.GetFCOrderWithoutHidden())
                        {
                            var fcSubs = Plugin.DatabaseCache.GetSubmarines(fcs[key].FreeCompanyId);
                            if (fcSubs.Length == 0)
                                continue;

                            using var buttonColor = ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.ParsedPink, current == key);
                            if (ImGui.Button($"{Plugin.NameConverter.GetName(fcs[key])}##{key}", new Vector2(width, 0)))
                                CurrentSelection = key;
                        }
                    }
                }

                ImGui.NextColumn();
                if (CurrentSelection != 1)
                    Overview();
                else
                    All();
            }
        }

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(Helper.SeparatorPadding);

        using var bottomBar = ImRaii.Child("BottomBar", Vector2.Zero, false, 0);
        if (bottomBar.Success)
            Helper.MainMenuIcon();
    }
}
