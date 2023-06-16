using Dalamud.Interface.Windowing;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Helpy;

public partial class HelpyWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    public HelpyWindow(Plugin plugin, Configuration configuration) : base("Helpy")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(680, 480),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = configuration;

        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExplorationPretty>()!;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (!Submarines.KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
        {
            Helper.NoData();
            return;
        }

        var storageOpen = false;
        var buttonHeight = ImGui.CalcTextSize("XXX").Y + 10.0f;
        if (ImGui.BeginChild("HelpyContent", new Vector2(0, -(buttonHeight + (25.0f * ImGuiHelpers.GlobalScale)))))
        {
            if (ImGui.BeginTabBar("##helperTabBar"))
            {
                ProgressionTab(fcSub);

                storageOpen = StorageTab();
            }
            ImGui.EndTabBar();
        }
        ImGui.EndChild();

        ImGuiHelpers.ScaledDummy(5);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);

        if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
        {
            if (ImGui.Button("Settings"))
                Plugin.DrawConfigUI();

            if (storageOpen)
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedBlue);
                if (ImGui.Button("Refresh"))
                    Storage.Refresh = true;
                ImGui.PopStyleColor();
            }
        }
        ImGui.EndChild();
    }
}

