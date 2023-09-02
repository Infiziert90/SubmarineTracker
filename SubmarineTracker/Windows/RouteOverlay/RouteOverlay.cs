using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.RouteOverlay;

public class RouteOverlay : Window, IDisposable
{
    private readonly Plugin Plugin;
    private readonly Configuration Configuration;

    private int Map = -1;

    private uint[] BestPath = Array.Empty<uint>();
    private bool Calculate;
    private bool ComputingPath;
    private DateTime ComputeStart = DateTime.Now;

    public static ExcelSheet<SubmarineExplorationPretty> ExplorationSheet = null!;

    public RouteOverlay(Plugin plugin, Configuration configuration) : base("Route Overlay")
    {
        Size = new Vector2(300, 350);

        Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;

        Plugin = plugin;
        Configuration = configuration;

        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExplorationPretty>()!;
    }

    public void Dispose() { }

    public override unsafe void PreOpenCheck()
    {
        if (!Configuration.AutoSelectCurrent || !Configuration.ShowRouteOverlay)
        {
            Reset();
            return;
        }

        try
        {
            var addonPtr = Plugin.GameGui.GetAddonByName("AirShipExploration");
            if (addonPtr == nint.Zero)
            {
                Reset();
                return;
            }

            var explorationBaseNode = (AtkUnitBase*) addonPtr;
            Position = new Vector2(explorationBaseNode->X - (Size!.Value.X * ImGuiHelpers.GlobalScale), explorationBaseNode->Y + 5);
            PositionCondition = ImGuiCond.Always;

            // Check if submarine voyage log is open and not Airship
            var map = (int) explorationBaseNode->AtkValues[2].UInt;
            if (map < 63191)
            {
                Reset();
                return;
            }

            var selectedMap = map - 63191; // 63191 = Deep-sea Site
            if (selectedMap != Map)
            {
                Calculate = true;
                BestPath = Array.Empty<uint>();
                Plugin.BuilderWindow.MustInclude.Clear();
                Plugin.BuilderWindow.ExplorationPopupOptions = null;
                Map = selectedMap;
            }

            IsOpen = true;
        }
        catch
        {
            // Something went wrong, we don't draw
            Reset();
        }
    }

    public override void PreDraw()
    {
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Helper.TransparentBackground);
    }

    public override void Draw()
    {
        // Always refresh submarine if we have interface selection
        Plugin.BuilderWindow.RefreshCache();
        Plugin.BuilderWindow.CurrentBuild.ChangeMap(Map);

        if (Configuration.HighestLevel < Plugin.BuilderWindow.CurrentBuild.Rank)
        {
            if (ImGui.IsWindowHovered())
                ImGui.SetTooltip("Submarine above threshold\nCheck your config for higher level suggestions.");

            Calculate = false;
            return;
        }

        if (Calculate && !ComputingPath)
        {
            Calculate = false;

            BestPath = Array.Empty<uint>();
            ComputeStart = DateTime.Now;
            ComputingPath = true;
            Task.Run(() =>
            {
                var path = Plugin.BuilderWindow.FindBestPath(Plugin.BuilderWindow.CurrentBuild);
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

        ImGui.TextColored(ImGuiColors.DalamudViolet, $"Must Include {Plugin.BuilderWindow.MustInclude.Count} / 5");

        var listHeight = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
        if (Plugin.BuilderWindow.MustInclude.Count >= 5) ImGui.BeginDisabled();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(30.0f * ImGuiHelpers.GlobalScale, listHeight));
        ImGui.PopFont();
        if (Plugin.BuilderWindow.MustInclude.Count >= 5) ImGui.EndDisabled();

        var fcSub = Submarines.KnownSubmarines[Plugin.ClientState.LocalContentId];
        if (Plugin.BuilderWindow.ExplorationPopupOptions == null)
        {
            ExcelSheetSelector.FilteredSearchSheet = null!;
            Plugin.BuilderWindow.ExplorationPopupOptions = new()
            {
                FormatRow = e => $"{NumToLetter(e.RowId - startPoint)}. {UpperCaseStr(e.Destination)} (Rank {e.RankReq})",
                FilteredSheet = ExplorationSheet.Where(r => r.Map.Row == Plugin.BuilderWindow.CurrentBuild.Map + 1 && fcSub.UnlockedSectors[r.RowId] && r.RankReq <= Plugin.BuilderWindow.CurrentBuild.Rank)
            };
        }

        if (ExcelSheetSelector.ExcelSheetPopup("ExplorationAddPopup", out var row, Plugin.BuilderWindow.ExplorationPopupOptions, Plugin.BuilderWindow.MustInclude.Count >= 5))
        {
            var point = ExplorationSheet.GetRow(row)!;
            if (!Plugin.BuilderWindow.MustInclude.Contains(point))
                Plugin.BuilderWindow.MustInclude.Add(point);
        }

        ImGui.SameLine();

        if (ImGui.BeginListBox("##MustIncludePoints", new Vector2(-1, listHeight)))
        {
            foreach (var p in Plugin.BuilderWindow.MustInclude.ToArray())
            {
                if (ImGui.Selectable($"{NumToLetter(p.RowId - startPoint)}. {UpperCaseStr(p.Destination)}"))
                    Plugin.BuilderWindow.MustInclude.Remove(p);
            }

            ImGui.EndListBox();
        }
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor();
    }

    private void Reset()
    {
        Map = -1;
        IsOpen = false;
    }
}
