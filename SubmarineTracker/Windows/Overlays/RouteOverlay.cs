using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using SubmarineTracker.Data;

using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Overlays;

public class RouteOverlay : Window, IDisposable
{
    private readonly Plugin Plugin;

    private int Map = -1;

    private Voyage.BestRoute BestRoute = Voyage.BestRoute.Empty();
    private bool ComputingPath;
    private DateTime ComputeStart = DateTime.Now;

    public bool Calculate;
    public readonly HashSet<SubmarineExploration> MustInclude = [];

    public RouteOverlay(Plugin plugin) : base("Route Overlay##SubmarineTracker")
    {
        Size = new Vector2(300, 330);

        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
        ForceMainWindow = true;

        Plugin = plugin;
    }

    public void Dispose() { }

    public override unsafe void PreOpenCheck()
    {
        IsOpen = false;
        if (!Plugin.Configuration.AutoSelectCurrent || !Plugin.Configuration.ShowRouteOverlay)
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
                BestRoute = Voyage.BestRoute.Empty();
                MustInclude.Clear();
                Plugin.BuilderWindow.ExplorationPopupOptions = null;

                Map = selectedMap;
                Plugin.BuilderWindow.CurrentBuild.ChangeMap(selectedMap);
            }

            Size = Plugin.Configuration.DurationLimit != DurationLimit.Custom ? new Vector2(300, 330) : new Vector2(300, 350);
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
        if (Plugin.Configuration.HighestLevel < Plugin.BuilderWindow.CurrentBuild.Rank && !MustInclude.Any())
        {
            if (ImGui.IsWindowHovered())
                ImGui.SetTooltip(Loc.Localize("Route Overlay Tooltip - High Rank", "Submarine above threshold and MustInclude is empty\nCheck your config for higher level suggestions."));

            Calculate = false;
            return;
        }

        var fcSub = Plugin.DatabaseCache.GetFreeCompanies()[Plugin.GetFCId];
        if (Calculate && !ComputingPath)
        {
            Calculate = false;
            BestRoute = Voyage.BestRoute.Empty();

            ComputeStart = DateTime.Now;
            ComputingPath = true;

            Task.Run(() =>
            {
                var mustInclude = MustInclude.Select(s => s.RowId).ToArray();
                var unlocked = fcSub.UnlockedSectors.Where(pair => pair.Value).Select(pair => pair.Key).ToArray();
                var path = Voyage.FindBestRoute(Plugin.BuilderWindow.CurrentBuild, unlocked, mustInclude, [], false, false);
                if (path.Path.Length == 0)
                    Plugin.BuilderWindow.CurrentBuild.NotOptimized();

                BestRoute = path;
                ComputingPath = false;
            });
        }

        var startPoint = Sheets.ExplorationSheet.First(r => r.Map.RowId == Plugin.BuilderWindow.CurrentBuild.Map + 1).RowId;

        var height = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
        using (var listBox = ImRaii.ListBox("##BestPoints", new Vector2(-1, height)))
        {
            if (listBox.Success)
            {
                if (ComputingPath)
                {
                    ImGui.TextUnformatted($"{Loc.Localize("Terms - Loading", "Loading")} {new string('.', (int)((DateTime.Now - ComputeStart).TotalMilliseconds / 500) % 5)}");
                }
                else if (BestRoute.Path.Length == 0)
                {
                    ImGui.TextUnformatted(Loc.Localize("Best EXP Calculation - Nothing Found", "No route found, check speed and range ..."));
                }

                if (BestRoute.Path.Length != 0)
                {
                    foreach (var location in BestRoute.PathPretty)
                        ImGui.TextUnformatted($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}");

                    Plugin.BuilderWindow.CurrentBuild.UpdateOptimized(BestRoute);
                }
            }
        }

        var changed = false;
        var length = ImGui.CalcTextSize($"{Loc.Localize("Terms - Must Include", "Must Include")} {MustInclude.Count} / 5").X + 25.0f;
        var width = ImGui.GetContentRegionAvail().X / 3;

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Best EXP Entry - Duration Limit", "Duration Limit"));
        ImGui.SetNextItemWidth(width);
        using (var combo = ImRaii.Combo("##DurationLimitCombo", Plugin.Configuration.DurationLimit.GetName()))
        {
            if (combo.Success)
            {
                foreach (var durationLimit in (DurationLimit[])Enum.GetValues(typeof(DurationLimit)))
                {
                    if (ImGui.Selectable(durationLimit.GetName()))
                    {
                        Plugin.Configuration.DurationLimit = durationLimit;
                        changed = true;
                    }
                }
            }
        }

        if (Plugin.Configuration.DurationLimit != DurationLimit.None)
        {
            ImGui.SameLine(length);
            changed |= ImGui.Checkbox(Loc.Localize("Best EXP Checkbox - Maximize Duration", "Maximize Duration"), ref Plugin.Configuration.MaximizeDuration);
        }

        if (Plugin.Configuration.DurationLimit == DurationLimit.Custom)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Best EXP Entry - Hours and Minutes", "Hours & Minutes"));
            ImGui.SameLine(length);
            ImGui.SetNextItemWidth(width / 2.5f);
            changed |= ImGui.InputInt("##CustomHourInput", ref Plugin.Configuration.CustomHour, 0);

            ImGui.SameLine();
            ImGui.TextUnformatted(":");
            ImGui.SameLine();

            ImGui.SetNextItemWidth(width / 2.5f);
            changed |= ImGui.InputInt("##CustomMinInput", ref Plugin.Configuration.CustomMinute, 0);
        }

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.DalamudViolet, $"{Loc.Localize("Terms - Must Include", "Must Include")} {MustInclude.Count} / 5");
        ImGui.SameLine(length);
        changed |= ImGui.Checkbox(Loc.Localize("Best EXP Checkbox - Auto Include", "Auto Include"), ref Plugin.Configuration.MainRouteAutoInclude);
        ImGuiComponents.HelpMarker(Loc.Localize("Best EXP Tooltip - Auto Include", "Auto include the next main sector, if there is one"));

        var listHeight = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
        using (ImRaii.Disabled(MustInclude.Count >= 5))
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(30.0f * ImGuiHelpers.GlobalScale, listHeight));
        }

        if (Plugin.BuilderWindow.ExplorationPopupOptions == null)
        {
            ExcelSheetSelector<SubmarineExploration>.FilteredSearchSheet = null!;
            Plugin.BuilderWindow.ExplorationPopupOptions = new()
            {
                FormatRow = e => $"{NumToLetter(e.RowId - startPoint)}. {UpperCaseStr(e.Destination)} ({Loc.Localize("Terms - Rank", "Rank")} {e.RankReq})",
                FilteredSheet = Sheets.ExplorationSheet.Where(r => r.Map.RowId == Plugin.BuilderWindow.CurrentBuild.Map + 1 && fcSub.UnlockedSectors[r.RowId] && r.RankReq <= Plugin.BuilderWindow.CurrentBuild.Rank)
            };
        }

        if (ExcelSheetSelector<SubmarineExploration>.ExcelSheetPopup("ExplorationAddPopup", out var row, Plugin.BuilderWindow.ExplorationPopupOptions, MustInclude.Count >= 5))
            changed |= MustInclude.Add(Sheets.ExplorationSheet.GetRow(row)!);

        ImGui.SameLine();

        using (var listBox = ImRaii.ListBox("##MustIncludePoints", new Vector2(-1, listHeight)))
        {
            if (listBox.Success)
            {
                foreach (var p in MustInclude.ToArray())
                    if (ImGui.Selectable($"{NumToLetter(p.RowId - startPoint)}. {UpperCaseStr(p.Destination)}"))
                        changed |= MustInclude.Remove(p);
            }
        }

        if (changed)
        {
            Plugin.Configuration.CustomHour = Math.Clamp(Plugin.Configuration.CustomHour, 1, 123);
            Plugin.Configuration.CustomMinute = Math.Clamp(Plugin.Configuration.CustomMinute, 0, 59);

            Calculate = true;
            Plugin.Configuration.Save();
        }
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor();
    }
}
