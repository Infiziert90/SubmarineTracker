using System.Threading.Tasks;
using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    private List<SubmarineExplorationPretty> MustInclude = new();

    private uint[] BestPath = Array.Empty<uint>();
    private bool ComputingPath;
    private int LastComputedRank;
    private DateTime ComputeStart = DateTime.Now;
    private bool Error;

    private bool Calculate;

    private int LastSeenMap;
    private int LastSeenRank;
    private bool OptionsChanged;

    private bool IgnoreUnlocks = false;

    private Dictionary<int, (int, List<SubmarineExplorationPretty>)[]> CachedDistances = new();

    private uint[] FindBestPath(Build.RouteBuild routeBuild)
    {
        Error = false;
        LastComputedRank = routeBuild.Rank;

        if (Submarines.KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
        {
            List<SubmarineExplorationPretty> valid;
            int highestRank;
            try
            {
                valid = ExplorationSheet
                            .Where(r => r.Map.Row == routeBuild.Map + 1 && !r.StartingPoint && r.RankReq <= routeBuild.Rank)
                            .Where(r => IgnoreUnlocks || fcSub.UnlockedSectors[r.RowId])
                            .ToList();
                highestRank = valid.Max(r => r.RankReq);
            }
            catch (KeyNotFoundException)
            {
                Error = true;
                ComputingPath = false;
                Calculate = false;
                return Array.Empty<uint>();
            }

            var startPoint = ExplorationSheet.First(r => r.Map.Row == routeBuild.Map + 1);
            if (!CachedDistances.TryGetValue(highestRank, out var distances))
            {
                var paths = valid.Select(t => new[] { startPoint.RowId, t.RowId }.ToList()).ToHashSet(new ListComparer());
                if (MustInclude.Any())
                    paths = new[] { MustInclude.Select(t => t.RowId).Prepend(startPoint.RowId).ToList() }.ToHashSet(new ListComparer());

                var i = MustInclude.Any() ? MustInclude.Count : 1;
                while (i++ < 5)
                {
                    foreach (var path in paths.ToArray())
                    {
                        foreach (var validPoint in valid.Where(t => !path.Contains(t.RowId)))
                        {
                            var pathNew = path.ToList();
                            pathNew.Add(validPoint.RowId);
                            paths.Add(pathNew.ToList());
                        }
                    }
                }

                var allPaths = paths.AsParallel().Select(t => t.Select(f => valid.FirstOrDefault(k => k.RowId == f) ?? startPoint)).ToList();

                if (!allPaths.Any())
                {
                    ComputingPath = false;
                    Calculate = false;
                    return Array.Empty<uint>();
                }

                distances = allPaths.AsParallel().Select(Voyage.CalculateDistance).ToArray();
                CachedDistances.Add(highestRank, distances);
            }
            var build = routeBuild.GetSubmarineBuild;
            var optimalDistances = distances.Where(t => t.Item1 <= build.Range).ToArray();
            if (!optimalDistances.Any())
            {
                ComputingPath = false;
                Calculate = false;
                return Array.Empty<uint>();
            }

            var bestPath = optimalDistances.AsParallel().Select(t =>
                {
                    var path = t.Item2.Prepend(startPoint).ToArray();

                    return new Tuple<uint[], TimeSpan, double>(
                        t.Item2.Select(t => t.RowId).ToArray(),
                        TimeSpan.FromSeconds(Voyage.CalculateDuration(path, build)),
                        SectorBreakpoints.CalculateExpForSectors(t.Item2, CurrentBuild.GetSubmarineBuild)
                    );
                })
              .Where(t => t.Item2 < DateUtil.DurationToTime(Configuration.DurationLimit))
              .OrderByDescending(t => MaximizeDuration ? t.Item3 : t.Item3 / t.Item2.TotalMinutes)
              .Select(t => t.Item1)
              .FirstOrDefault();

            if (bestPath == null)
            {
                ComputingPath = false;
                Calculate = false;
                return Array.Empty<uint>();
            }

            ComputingPath = false;
            Calculate = false;

            return bestPath;
        }

        Error = true;
        ComputingPath = false;
        Calculate = false;

        return Array.Empty<uint>();
    }

    private void ExpTab()
    {
        if (ImGui.BeginTabItem("Best Exp"))
        {
            if (!Submarines.KnownSubmarines.ContainsKey(Plugin.ClientState.LocalContentId))
            {
                if (ImGui.BeginChild("ExpSelector", new Vector2(0, -110)))
                {
                    Helper.NoData();
                }
                ImGui.EndChild();
                return;
            }

            if (ImGui.BeginChild("ExpSelector", new Vector2(0, -(110 * ImGuiHelpers.GlobalScale))))
            {
                if (ImGui.BeginChild("BestPath", new Vector2(0, (170 * ImGuiHelpers.GlobalScale))))
                {
                    var maps = ExplorationSheet
                       .Where(r => r.StartingPoint)
                       .Where(r => ExplorationSheet.GetRow(r.RowId + 1)!.RankReq <= CurrentBuild.Rank)
                       .Select(r => ToStr(r.Map.Value!.Name))
                       .ToArray();

                    // Always pick highest rank map if smaller then possible
                    if (maps.Length <= CurrentBuild.Map)
                    {
                        CurrentBuild.Map = maps.Length - 1;
                        Reset(CurrentBuild.Map);
                    }

                    var selectedMap = CurrentBuild.Map;
                    ImGui.Combo("##mapsSelection", ref selectedMap, maps, maps.Length);

                    var mapChanged = selectedMap != CurrentBuild.Map;
                    if (mapChanged)
                        Reset(selectedMap);

                    CurrentBuild.Map = selectedMap;

                    var beginCalculation = false;
                    if (Configuration.CalculateOnInteraction)
                    {
                        if (Calculate)
                            beginCalculation = true;
                    }
                    else
                    {
                        if (mapChanged || LastComputedRank != CurrentBuild.Rank || OptionsChanged)
                            beginCalculation = true;
                    }

                    if (beginCalculation && !ComputingPath && !Error)
                    {
                        // Don't set it false until we sure it got begins calculation
                        OptionsChanged = false;

                        BestPath = Array.Empty<uint>();
                        ComputeStart = DateTime.Now;
                        ComputingPath = true;
                        Task.Run(() =>
                        {
                            var path = FindBestPath(CurrentBuild);
                            if (!path.Any())
                                CurrentBuild.NotOptimized();

                            BestPath = path;
                        });
                    }

                    var startPoint = ExplorationSheet.First(r => r.Map.Row == CurrentBuild.Map + 1).RowId;
                    var height = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
                    if (ImGui.BeginListBox("##bestPoints", new Vector2(-1, height)))
                    {
                        if (ComputingPath)
                        {
                            ImGui.Text($"Loading {new string('.', (int)((DateTime.Now - ComputeStart).TotalMilliseconds / 500) % 5)}");
                        }
                        else if (!BestPath.Any())
                        {
                            ImGui.Text(Configuration.CalculateOnInteraction && !Calculate ? "Not calculated ..." : "No route found, check speed and range ...");
                        }

                        if (Error)
                        {
                            ImGui.TextWrapped("Error: Unable to calculate, please refresh your data (Voyage Control Panel -> Submersible Management).");
                            if (Submarines.KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
                                if (fcSub.UnlockedSectors.ContainsKey(startPoint))
                                    Error = false;
                        }

                        if (BestPath.Any())
                        {
                            foreach (var location in BestPath)
                            {
                                var p = ExplorationSheet.GetRow(location)!;
                                if (location > startPoint)
                                    ImGui.Text($"{NumToLetter(location - startPoint)}. {UpperCaseStr(p.Destination)}");
                            }
                            CurrentBuild.UpdateOptimized(Voyage.CalculateDistance(BestPath.ToList().Prepend(startPoint).Select(t => ExplorationSheet.GetRow(t)!)));
                        }

                        ImGui.EndListBox();
                    }

                    if (Configuration.CalculateOnInteraction)
                    {
                        if (ImGui.Button("Calculate"))
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
                    var length = ImGui.CalcTextSize("Must Include 5 / 5").X + 25.0f;

                    var changed = false;
                    ImGui.TextColored(ImGuiColors.DalamudViolet, "Options:");
                    ImGui.Indent(10.0f);
                    changed |= ImGui.Checkbox("Disable automatic calculation", ref Configuration.CalculateOnInteraction);
                    if (ImGui.Checkbox("Ignore unlocks", ref IgnoreUnlocks))
                    {
                        changed = true;
                        OptionsChanged = true;
                    }
                    ImGui.TextColored(ImGuiColors.DalamudViolet, "Duration Limit");
                    ImGui.SameLine(length);
                    ImGui.SetNextItemWidth(width);
                    if (ImGui.BeginCombo($"##durationLimitCombo", DateUtil.GetDurationLimitName(Configuration.DurationLimit)))
                    {
                        foreach (var durationLimit in (DurationLimit[])Enum.GetValues(typeof(DurationLimit)))
                        {
                            if (ImGui.Selectable(DateUtil.GetDurationLimitName(durationLimit)))
                            {
                                Configuration.DurationLimit = durationLimit;
                                Configuration.Save();

                                OptionsChanged = true;
                            }
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.Checkbox("Maximize Duration limit", ref MaximizeDuration);

                    if (Submarines.KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
                    {
                        ImGui.TextColored(ImGuiColors.DalamudViolet, $"Must Include {MustInclude.Count} / 5");
                        ImGui.SameLine(length);
                        if (MustInclude.Count >= 5) ImGui.BeginDisabled();
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(width, 0));
                        ImGui.PopFont();
                        if (MustInclude.Count >= 5) ImGui.EndDisabled();

                        // Reset to refresh the internal state
                        if (LastSeenMap != CurrentBuild.Map || LastSeenRank != CurrentBuild.Rank)
                        {
                            MustInclude.Clear();
                            ExcelSheetSelector.FilteredSearchSheet = null!;
                        }

                        LastSeenMap = CurrentBuild.Map;
                        LastSeenRank = CurrentBuild.Rank;

                        try
                        {
                            var startPoint = ExplorationSheet.First(r => r.Map.Row == CurrentBuild.Map + 1).RowId;
                            ExcelSheetSelector.ExcelSheetPopupOptions<SubmarineExplorationPretty> ExplorationPopupOptions = new()
                            {
                                FormatRow = e => $"{NumToLetter(e.RowId - startPoint)}. {UpperCaseStr(e.Destination)} (Rank {e.RankReq})",
                                FilteredSheet = ExplorationSheet.Where(r => r.Map.Row == CurrentBuild.Map + 1 && !r.StartingPoint && fcSub.UnlockedSectors[r.RowId] && r.RankReq <= CurrentBuild.Rank)
                            };

                            if (ExcelSheetSelector.ExcelSheetPopup("ExplorationAddPopup", out var row, ExplorationPopupOptions, MustInclude.Count >= 5))
                            {
                                var point = ExplorationSheet.GetRow(row)!;
                                if (!MustInclude.Contains(point))
                                {
                                    MustInclude.Add(point);
                                    OptionsChanged = true;
                                }
                            }

                            var height = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
                            if (ImGui.BeginListBox("##MustIncludePoints", new Vector2(-1, height)))
                            {
                                foreach (var p in MustInclude.ToArray())
                                    if (ImGui.Selectable($"{NumToLetter(p.RowId - startPoint)}. {UpperCaseStr(p.Destination)}"))
                                    {
                                        MustInclude.Remove(p);
                                        OptionsChanged = true;
                                    }

                                ImGui.EndListBox();
                            }
                        }
                        catch (KeyNotFoundException)
                        {
                            ImGui.TextWrapped("Error: Unable to find unlocked sectors, please refresh your data (Voyage Control Panel -> Submersible Management)");
                        }
                    }

                    ImGui.Unindent(10.0f);

                    if (changed)
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
        Error = false;
        ComputingPath = false;
        BestPath = Array.Empty<uint>();
        MustInclude.Clear();
        CurrentBuild.ChangeMap(newMap);
    }
}
