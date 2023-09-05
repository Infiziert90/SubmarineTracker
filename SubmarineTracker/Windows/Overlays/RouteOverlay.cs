using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Overlays;

public class RouteOverlay : Window, IDisposable
{
    private readonly Plugin Plugin;
    private readonly Configuration Configuration;

    private int Map = -1;

    private uint[] BestPath = Array.Empty<uint>();
    private bool Calculate;
    private bool ComputingPath;
    private DateTime ComputeStart = DateTime.Now;
    public readonly HashSet<SubmarineExplorationPretty> MustInclude = new();

    public static ExcelSheet<SubmarineExplorationPretty> ExplorationSheet = null!;

    public RouteOverlay(Plugin plugin, Configuration configuration) : base("Route Overlay")
    {
        Size = new Vector2(300, 350);

        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;

        Plugin = plugin;
        Configuration = configuration;

        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExplorationPretty>()!;
    }

    public void Dispose() { }

    public override unsafe void PreOpenCheck()
    {
        IsOpen = false;
        if (!Configuration.AutoSelectCurrent || !Configuration.ShowRouteOverlay)
            return;

        // Always refresh submarine if we have interface selection
        Plugin.BuilderWindow.RefreshCache();
        try
        {
            var addonPtr = Plugin.GameGui.GetAddonByName("AirShipExploration");
            if (addonPtr == nint.Zero)
            {
                Map = -1;
                return;
            }

            var explorationBaseNode = (AtkUnitBase*) addonPtr;
            Position = new Vector2(explorationBaseNode->X - (Size!.Value.X * ImGuiHelpers.GlobalScale), explorationBaseNode->Y + 5);
            PositionCondition = ImGuiCond.Always;

            // Check if submarine voyage log is open and not Airship
            var map = (int) explorationBaseNode->AtkValues[2].UInt;
            if (map < 63191)
                return;

            var selectedMap = map - 63191; // 63191 = Deep-sea Site
            if (selectedMap != Map)
            {
                Calculate = true;
                BestPath = Array.Empty<uint>();
                MustInclude.Clear();
                Plugin.BuilderWindow.ExplorationPopupOptions = null;

                Map = selectedMap;
                Plugin.BuilderWindow.CurrentBuild.ChangeMap(selectedMap);
            }

            IsOpen = true;
        }
        catch
        {
            // Something went wrong, we don't draw
            Map = -1;
        }
    }

    public override void PreDraw()
    {
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Helper.TransparentBackground);
    }

    public override void Draw()
    {
        if (Configuration.HighestLevel < Plugin.BuilderWindow.CurrentBuild.Rank && !MustInclude.Any())
        {
            if (ImGui.IsWindowHovered())
                ImGui.SetTooltip("Submarine above threshold and MustInclude is empty\nCheck your config for higher level suggestions.");

            Calculate = false;
            return;
        }

        var fcSub = Submarines.KnownSubmarines[Plugin.ClientState.LocalContentId];
        if (Calculate && !ComputingPath)
        {
            Calculate = false;

            BestPath = Array.Empty<uint>();
            ComputeStart = DateTime.Now;
            ComputingPath = true;

            Task.Run(() =>
            {
                var mustInclude = MustInclude.Select(s => s.RowId).ToArray();
                var unlocked = fcSub.UnlockedSectors.Where(pair => pair.Value).Select(pair => pair.Key).ToArray();
                var path = Voyage.FindBestPath(Plugin.BuilderWindow.CurrentBuild, unlocked, mustInclude);
                if (!path.Any())
                    Plugin.BuilderWindow.CurrentBuild.NotOptimized();

                BestPath = path;
                ComputingPath = false;
            });
        }

        var startPoint = ExplorationSheet.First(r => r.Map.Row == Plugin.BuilderWindow.CurrentBuild.Map + 1).RowId;
        var height = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
        if (ImGui.BeginListBox("##BestPoints", new Vector2(-1, height)))
        {
            if (ComputingPath)
            {
                ImGui.Text($"Loading {new string('.', (int)((DateTime.Now - ComputeStart).TotalMilliseconds / 500) % 5)}");
            }
            else if (!BestPath.Any())
            {
                ImGui.Text("No route found, check speed and range ...");
            }

            if (BestPath.Any())
            {
                foreach (var location in BestPath.Select(s => ExplorationSheet.GetRow(s)!))
                        ImGui.Text($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}");

                Plugin.BuilderWindow.CurrentBuild.UpdateOptimized(Voyage.CalculateDistance(BestPath.Prepend(startPoint).Select(t => ExplorationSheet.GetRow(t)!)));
            }

            ImGui.EndListBox();
        }

        if (ImGui.Button("Recalculate"))
        {
            BestPath = Array.Empty<uint>();
            Calculate = true;
        }

        var width = ImGui.GetContentRegionAvail().X / 3;

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Duration Limit");
        ImGui.SetNextItemWidth(width);
        if (ImGui.BeginCombo($"##durationLimitCombo", Configuration.DurationLimit.GetName()))
        {
            foreach (var durationLimit in (DurationLimit[])Enum.GetValues(typeof(DurationLimit)))
            {
                if (ImGui.Selectable(durationLimit.GetName()))
                {
                    Configuration.DurationLimit = durationLimit;
                    Configuration.Save();
                }
            }

            ImGui.EndCombo();
        }
        if (Configuration.DurationLimit != DurationLimit.None)
        {
            ImGui.SameLine();
            if (ImGui.Checkbox("Maximize Duration", ref Configuration.MaximizeDuration))
                Configuration.Save();
        }

        ImGui.TextColored(ImGuiColors.DalamudViolet, $"Must Include {MustInclude.Count} / 5");

        var listHeight = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
        if (MustInclude.Count >= 5) ImGui.BeginDisabled();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(30.0f * ImGuiHelpers.GlobalScale, listHeight));
        ImGui.PopFont();
        if (MustInclude.Count >= 5) ImGui.EndDisabled();

        if (Plugin.BuilderWindow.ExplorationPopupOptions == null)
        {
            ExcelSheetSelector.FilteredSearchSheet = null!;
            Plugin.BuilderWindow.ExplorationPopupOptions = new()
            {
                FormatRow = e => $"{NumToLetter(e.RowId - startPoint)}. {UpperCaseStr(e.Destination)} (Rank {e.RankReq})",
                FilteredSheet = ExplorationSheet.Where(r => r.Map.Row == Plugin.BuilderWindow.CurrentBuild.Map + 1 && fcSub.UnlockedSectors[r.RowId] && r.RankReq <= Plugin.BuilderWindow.CurrentBuild.Rank)
            };
        }

        if (ExcelSheetSelector.ExcelSheetPopup("ExplorationAddPopup", out var row, Plugin.BuilderWindow.ExplorationPopupOptions, MustInclude.Count >= 5))
            MustInclude.Add(ExplorationSheet.GetRow(row)!);

        ImGui.SameLine();

        if (ImGui.BeginListBox("##MustIncludePoints", new Vector2(-1, listHeight)))
        {
            foreach (var p in MustInclude.ToArray())
                if (ImGui.Selectable($"{NumToLetter(p.RowId - startPoint)}. {UpperCaseStr(p.Destination)}"))
                    MustInclude.Remove(p);

            ImGui.EndListBox();
        }
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor();
    }
}
