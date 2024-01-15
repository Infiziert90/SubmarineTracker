using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    public readonly HashSet<SubmarineExplorationPretty> MustInclude = new();

    private uint[] BestPath = Array.Empty<uint>();
    private bool ComputingPath;
    private Build.RouteBuild LastComputedBuild;
    private DateTime ComputeStart = DateTime.Now;

    private bool Calculate;

    private int LastSeenMap;
    private int LastSeenRank;
    private bool OptionsChanged;

    private bool IgnoreUnlocks;
    private bool AvgBonus;

    public ExcelSheetSelector.ExcelSheetPopupOptions<SubmarineExplorationPretty>? ExplorationPopupOptions;

    private void ExpTab()
    {
        if (ImGui.BeginTabItem($"{Loc.Localize("Builder Tab - Best Exp", "Best Exp")}##BestExp"))
        {
            if (ImGui.BeginChild("ExpSelector", new Vector2(0, -(170 * ImGuiHelpers.GlobalScale))))
            {
                if (!Submarines.KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
                {
                    Helper.NoData();

                    ImGui.EndChild();
                    ImGui.EndTabItem();
                    return;
                }

                if (ImGui.BeginChild("BestPath", new Vector2(0, (170 * ImGuiHelpers.GlobalScale))))
                {
                    var maps = ExplorationSheet
                           .Where(r => r.StartingPoint)
                           .Select(r => ExplorationSheet.GetRow(r.RowId + 1)!)
                           .Where(r => r.RankReq <= CurrentBuild.Rank)
                           .Where(r => IgnoreUnlocks || fcSub.UnlockedSectors[r.RowId])
                           .Select(r => ToStr(r.Map.Value!.Name))
                           .ToArray();

                    // Always pick highest rank map if smaller than possible
                    if (maps.Length <= CurrentBuild.Map)
                    {
                        CurrentBuild.Map = maps.Length - 1;
                        Reset(CurrentBuild.Map);
                    }

                    var selectedMap = CurrentBuild.Map;
                    Helper.DrawComboWithArrows("##mapSelection", ref selectedMap, ref maps);

                    var mapChanged = selectedMap != CurrentBuild.Map;
                    if (mapChanged)
                    {
                        Reset(selectedMap);
                        CurrentBuild.Map = selectedMap;
                    }

                    var beginCalculation = false;
                    if (Configuration.CalculateOnInteraction)
                    {
                        if (Calculate)
                            beginCalculation = true;
                    }
                    else if (mapChanged || !LastComputedBuild.SameBuild(CurrentBuild) || OptionsChanged)
                    {
                        beginCalculation = true;
                    }

                    if (beginCalculation && !ComputingPath)
                    {
                        // Don't set it false until we sure it got begins calculation
                        OptionsChanged = false;

                        BestPath = Array.Empty<uint>();
                        ComputeStart = DateTime.Now;
                        ComputingPath = true;
                        Task.Run(() =>
                        {
                            LastComputedBuild = new Build.RouteBuild(CurrentBuild);
                            Calculate = false;

                            var mustInclude = MustInclude.Select(s => s.RowId).ToArray();
                            var unlocked = fcSub.UnlockedSectors.Where(pair => pair.Value).Select(pair => pair.Key).ToArray();
                            var path = Voyage.FindBestPath(CurrentBuild, unlocked, mustInclude, ignoreUnlocks: IgnoreUnlocks, avgExpBonus: AvgBonus);
                            if (!path.Any())
                                CurrentBuild.NotOptimized();

                            ComputingPath = false;
                            BestPath = path;
                        });
                    }

                    var startPoint = ExplorationSheet.First(r => r.Map.Row == CurrentBuild.Map + 1).RowId;
                    var height = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
                    if (ImGui.BeginListBox("##bestPoints", new Vector2(-1, height)))
                    {
                        if (ComputingPath)
                        {
                            ImGui.Text($"{Loc.Localize("Terms - Loading", "Loading")} {new string('.', (int)((DateTime.Now - ComputeStart).TotalMilliseconds / 500) % 5)}");
                        }

                        if (BestPath.Any())
                        {
                            foreach (var location in BestPath.Select(s => ExplorationSheet.GetRow(s)!))
                                if (location.RowId > startPoint)
                                    ImGui.Text($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}");

                            CurrentBuild.UpdateOptimized(Voyage.CalculateDistance(BestPath.Prepend(startPoint).Select(t => ExplorationSheet.GetRow(t)!)));
                        }
                        else
                        {
                            ImGui.Text(Configuration.CalculateOnInteraction && !Calculate ? Loc.Localize("Best EXP Calculation - Manual Calculation", "Not calculated ...") : Loc.Localize("Best EXP Calculation - Nothing Found", "No route found, check speed and range ..."));
                        }

                        ImGui.EndListBox();
                    }

                    if (Configuration.CalculateOnInteraction)
                    {
                        if (ImGui.Button(Loc.Localize("Best EXP Calculation - Calculate", "Calculate")))
                        {
                            BestPath = Array.Empty<uint>();
                            Calculate = true;
                        }
                    }
                }
                ImGui.EndChild();

                if (ImGui.BeginChild("ExpOptions", new Vector2(0, 0)))
                {
                    var width = ImGui.GetContentRegionAvail().X / 3;
                    var length = ImGui.CalcTextSize($"{Loc.Localize("Terms - Must Include", "Must Include")} 5 / 5").X + 25.0f;

                    ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Options", "Options:"));
                    ImGuiHelpers.ScaledIndent(10.0f);
                    OptionsChanged |= ImGui.Checkbox(Loc.Localize("Best EXP Checkbox - No Automatic", "Disable Automatic Calculation"), ref Configuration.CalculateOnInteraction);
                    OptionsChanged |= ImGui.Checkbox(Loc.Localize("Best EXP Checkbox - Ignore Unlocks", "Ignore Unlocks"), ref IgnoreUnlocks);
                    OptionsChanged |= ImGui.Checkbox(Loc.Localize("Best EXP Checkbox - Avg Bonus", "Use Avg Exp Bonus"), ref AvgBonus);
                    ImGuiComponents.HelpMarker(Loc.Localize("Best EXP Tooltip - Avg Bonus", "This calculation takes only guaranteed bonus exp into account.\nWith this option it will instead take the avg of possible exp bonus."));
                    if (Configuration.DurationLimit != DurationLimit.None)
                        OptionsChanged |= ImGui.Checkbox(Loc.Localize("Best EXP Checkbox - Maximize Duration", "Maximize Duration"), ref Configuration.MaximizeDuration);
                    ImGuiHelpers.ScaledIndent(-10.0f);

                    ImGui.AlignTextToFramePadding();
                    ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Best EXP Entry - Duration Limit", "Duration Limit"));
                    ImGui.SameLine(length);
                    ImGui.SetNextItemWidth(width);
                    if (ImGui.BeginCombo($"##durationLimitCombo", Configuration.DurationLimit.GetName()))
                    {
                        foreach (var durationLimit in (DurationLimit[])Enum.GetValues(typeof(DurationLimit)))
                        {
                            if (ImGui.Selectable(durationLimit.GetName()))
                            {
                                Configuration.DurationLimit = durationLimit;
                                Configuration.Save();

                                OptionsChanged = true;
                            }
                        }

                        ImGui.EndCombo();
                    }

                    if (Configuration.DurationLimit == DurationLimit.Custom)
                    {
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Terms - Custom", "Custom"));
                        ImGui.SameLine(length);

                        ImGui.SetNextItemWidth(width / 5.0f);
                        if (ImGui.InputInt("##CustomHourInput", ref Configuration.CustomHour, 0))
                        {
                            Configuration.CustomHour = Math.Clamp(Configuration.CustomHour, 1, 123);
                            Configuration.Save();
                        }
                        ImGui.SameLine();
                        ImGui.TextUnformatted(":");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(width / 5.0f);
                        if (ImGui.InputInt("##CustomMinInput", ref Configuration.CustomMinute, 0))
                        {
                            Configuration.CustomMinute = Math.Clamp(Configuration.CustomMinute, 0, 59);
                            Configuration.Save();
                        }
                        ImGui.SameLine();
                        ImGui.TextUnformatted(Loc.Localize("Best EXP Entry - Hours and Minutes", "Hours & Minutes"));
                    }


                    ImGui.TextColored(ImGuiColors.DalamudViolet, $"{Loc.Localize("Terms - Must Include", "Must Include")} {MustInclude.Count} / 5");

                    var listHeight = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
                    if (MustInclude.Count >= 5) ImGui.BeginDisabled();
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(30.0f * ImGuiHelpers.GlobalScale, listHeight));
                    ImGui.PopFont();
                    if (MustInclude.Count >= 5) ImGui.EndDisabled();

                    // Reset to refresh the internal state
                    if (LastSeenMap != CurrentBuild.Map || LastSeenRank != CurrentBuild.Rank)
                    {
                        MustInclude.Clear();
                        ExplorationPopupOptions = null;
                    }

                    LastSeenMap = CurrentBuild.Map;
                    LastSeenRank = CurrentBuild.Rank;

                    var startPoint = ExplorationSheet.First(r => r.Map.Row == CurrentBuild.Map + 1).RowId;
                    var error = !fcSub.UnlockedSectors.ContainsKey(startPoint);
                    if (ExplorationPopupOptions == null)
                    {
                        ExcelSheetSelector.FilteredSearchSheet = null!;
                        ExplorationPopupOptions = new()
                        {
                            FormatRow = e => $"{NumToLetter(e.RowId, true)}. {UpperCaseStr(e.Destination)} ({Loc.Localize("Terms - Rank", "Rank")} {e.RankReq})",
                            FilteredSheet = ExplorationSheet.Where(r => !error && !r.StartingPoint && r.Map.Row == CurrentBuild.Map + 1 && (IgnoreUnlocks || fcSub.UnlockedSectors[r.RowId]) && r.RankReq <= CurrentBuild.Rank)
                        };
                    }

                    if (ExcelSheetSelector.ExcelSheetPopup("ExplorationAddPopup", out var row, ExplorationPopupOptions, error || MustInclude.Count >= 5))
                    {
                        var point = ExplorationSheet.GetRow(row)!;
                        if (MustInclude.Add(point))
                            OptionsChanged = true;
                    }

                    ImGui.SameLine();

                    if (ImGui.BeginListBox("##MustIncludePoints", new Vector2(-1, listHeight)))
                    {
                        foreach (var p in MustInclude.ToArray())
                        {
                            if (ImGui.Selectable($"{NumToLetter(p.RowId - startPoint)}. {UpperCaseStr(p.Destination)}"))
                            {
                                MustInclude.Remove(p);
                                OptionsChanged = true;
                            }
                        }

                        ImGui.EndListBox();
                    }

                    if (OptionsChanged)
                        Configuration.Save();
                }
                ImGui.EndChild();
            }
            ImGui.EndChild();

            ImGui.EndTabItem();
        }
    }

    private void Reset(int newMap)
    {
        ComputingPath = false;
        BestPath = Array.Empty<uint>();
        MustInclude.Clear();
        CurrentBuild.ChangeMap(newMap);
    }
}
