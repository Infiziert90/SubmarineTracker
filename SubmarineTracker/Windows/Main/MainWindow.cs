using Dalamud.Interface.Windowing;
using SubmarineTracker.Data;

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

        var buttonHeight = ImGui.CalcTextSize("RRRR").Y + (20.0f * ImGuiHelpers.GlobalScale);
        using (var subContent = ImRaii.Child("##subContent", new Vector2(0, -buttonHeight)))
        {
            if (subContent)
            {
                var buttonWidth = Plugin.Configuration.NameOption != NameOptions.FullName
                                      ? ImGui.CalcTextSize("XXXXX@Halicarnassus").X + (10 * ImGuiHelpers.GlobalScale)
                                      : ImGui.CalcTextSize("Character Name@Halicarnassus").X + (10 * ImGuiHelpers.GlobalScale);

                ImGui.Columns(2, "columns", true);
                if (!Plugin.Configuration.UserResize)
                    ImGui.SetColumnWidth(0, buttonWidth + (20 * ImGuiHelpers.GlobalScale));
                else
                    buttonWidth = ImGui.GetContentRegionAvail().X;

                using (var fcList = ImRaii.Child("##fcList"))
                {
                    if (fcList)
                    {
                        Plugin.EnsureFCOrderSafety();
                        if (!(Plugin.Configuration.ShowAll && CurrentSelection == 1))
                            if (!Plugin.DatabaseCache.GetFreeCompanies().ContainsKey(CurrentSelection))
                                CurrentSelection = Plugin.GetFCOrderWithoutHidden().First();

                        var current = CurrentSelection;
                        if (Plugin.Configuration.ShowAll)
                        {
                            using var buttonColor = ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.ParsedPink, current == 1);
                            if (ImGui.Button(Loc.Localize("Terms - All", "All"), new Vector2(buttonWidth, 0)))
                                CurrentSelection = 1;
                        }

                        foreach (var key in Plugin.GetFCOrderWithoutHidden())
                        {
                            var fc = Plugin.DatabaseCache.GetFreeCompanies()[key];
                            var fcSubs = Plugin.DatabaseCache.GetSubmarines(fc.FreeCompanyId);
                            if (fcSubs.Length == 0)
                                continue;

                            var text = Plugin.NameConverter.GetName(fc);
                            using var buttonColor = ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.ParsedPink, current == key);
                            if (ImGui.Button($"{text}##{key}", new Vector2(buttonWidth, 0)))
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
        ImGuiHelpers.ScaledDummy(1.0f);

        using var bottomBar = ImRaii.Child("BottomBar", new Vector2(0, 0), false, 0);
        if (bottomBar)
            Helper.MainMenuIcon();
    }
}
