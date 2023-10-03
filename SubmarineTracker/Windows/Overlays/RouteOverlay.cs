using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Components;
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
    private bool ComputingPath;
    private DateTime ComputeStart = DateTime.Now;

    public bool Calculate;
    public readonly HashSet<SubmarineExplorationPretty> MustInclude = new();

    public static ExcelSheet<SubmarineExplorationPretty> ExplorationSheet = null!;

    public RouteOverlay(Plugin plugin, Configuration configuration) : base("Route Overlay##SubmarineTracker")
    {
        Size = new Vector2(300, 330);

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

            Size = Configuration.DurationLimit != DurationLimit.Custom ? new Vector2(300, 330) : new Vector2(300, 350);
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
                ImGui.SetTooltip(Loc.Localize("Route Overlay Tooltip - High Rank", "Submarine above threshold and MustInclude is empty\nCheck your config for higher level suggestions."));

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
                ImGui.Text($"{Loc.Localize("Terms - Loading", "Loading")} {new string('.', (int)((DateTime.Now - ComputeStart).TotalMilliseconds / 500) % 5)}");
            }
            else if (!BestPath.Any())
            {
                ImGui.Text(Loc.Localize("Best EXP Calculation - Nothing Found", "No route found, check speed and range ..."));
            }

            if (BestPath.Any())
            {
                foreach (var location in BestPath.Select(s => ExplorationSheet.GetRow(s)!))
                        ImGui.Text($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}");

                Plugin.BuilderWindow.CurrentBuild.UpdateOptimized(Voyage.CalculateDistance(BestPath.Prepend(startPoint).Select(t => ExplorationSheet.GetRow(t)!)));
            }

            ImGui.EndListBox();
        }

        var changed = false;
        var length = ImGui.CalcTextSize($"{Loc.Localize("Terms - Must Include", "Must Include")} {MustInclude.Count} / 5").X + 25.0f;
        var width = ImGui.GetContentRegionAvail().X / 3;

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Best EXP Entry - Duration Limit", "Duration Limit"));
        ImGui.SetNextItemWidth(width);
        if (ImGui.BeginCombo($"##durationLimitCombo", Configuration.DurationLimit.GetName()))
        {
            foreach (var durationLimit in (DurationLimit[])Enum.GetValues(typeof(DurationLimit)))
            {
                if (ImGui.Selectable(durationLimit.GetName()))
                {
                    Configuration.DurationLimit = durationLimit;
                    changed = true;
                }
            }

            ImGui.EndCombo();
        }

        if (Configuration.DurationLimit != DurationLimit.None)
        {
            ImGui.SameLine(length);
            changed |= ImGui.Checkbox(Loc.Localize("Best EXP Checkbox - Maximize Duration", "Maximize Duration"), ref Configuration.MaximizeDuration);
        }

        if (Configuration.DurationLimit == DurationLimit.Custom)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Best EXP Entry - Hours and Minutes", "Hours & Minutes"));
            ImGui.SameLine(length);
            ImGui.SetNextItemWidth(width / 2.5f);
            changed |= ImGui.InputInt("##CustomHourInput", ref Configuration.CustomHour, 0);

            ImGui.SameLine();
            ImGui.TextUnformatted(":");
            ImGui.SameLine();

            ImGui.SetNextItemWidth(width / 2.5f);
            changed |= ImGui.InputInt("##CustomMinInput", ref Configuration.CustomMinute, 0);
        }

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.DalamudViolet, $"{Loc.Localize("Terms - Must Include", "Must Include")} {MustInclude.Count} / 5");
        ImGui.SameLine(length);
        changed |= ImGui.Checkbox(Loc.Localize("Best EXP Checkbox - Auto Include", "Auto Include"), ref Configuration.MainRouteAutoInclude);
        ImGuiComponents.HelpMarker(Loc.Localize("Best EXP Tooltip - Auto Include", "Auto include the next main sector, if there is one"));

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
                FormatRow = e => $"{NumToLetter(e.RowId - startPoint)}. {UpperCaseStr(e.Destination)} ({Loc.Localize("Terms - Rank", "Rank")} {e.RankReq})",
                FilteredSheet = ExplorationSheet.Where(r => r.Map.Row == Plugin.BuilderWindow.CurrentBuild.Map + 1 && fcSub.UnlockedSectors[r.RowId] && r.RankReq <= Plugin.BuilderWindow.CurrentBuild.Rank)
            };
        }

        if (ExcelSheetSelector.ExcelSheetPopup("ExplorationAddPopup", out var row, Plugin.BuilderWindow.ExplorationPopupOptions, MustInclude.Count >= 5))
            changed |= MustInclude.Add(ExplorationSheet.GetRow(row)!);

        ImGui.SameLine();

        if (ImGui.BeginListBox("##MustIncludePoints", new Vector2(-1, listHeight)))
        {
            foreach (var p in MustInclude.ToArray())
                if (ImGui.Selectable($"{NumToLetter(p.RowId - startPoint)}. {UpperCaseStr(p.Destination)}"))
                    changed |= MustInclude.Remove(p);

            ImGui.EndListBox();
        }

        if (changed)
        {
            Configuration.CustomHour = Math.Clamp(Configuration.CustomHour, 1, 123);
            Configuration.CustomMinute = Math.Clamp(Configuration.CustomMinute, 0, 59);

            Calculate = true;
            Configuration.Save();
        }
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor();
    }
}
