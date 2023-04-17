using System;
using System.Collections.Generic;
using ImGuiNET;
using SubmarineTracker.Data;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Logging;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows;

public partial class BuilderWindow
{
    private uint[] BestPath = Array.Empty<uint>();
    private bool ComputingPath;
    private int LastComputedRank;
    private DateTime ComputeStart = DateTime.Now;

    private void FindBestPath()
    {
        ComputingPath = true;
        LastComputedRank = SelectedRank;
        var startPoint = ExplorationSheet.First(r => r.Map.Row == SelectedMap + 1);

        if (Submarines.KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
        {
            var valid = ExplorationSheet
                .Where(r => r.Map.Row == SelectedMap + 1 && !r.Passengers & fcSub.UnlockedSectors[r.RowId] && r.RankReq <= SelectedRank)
                .ToList();

            PluginLog.Verbose("Start Building List");
            var paths = valid.Select(t => new[] { startPoint.RowId, t.RowId }.ToList()).ToHashSet(new ListComparer());
            var i = 1;
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

            var build = new Submarines.SubmarineBuild(SelectedRank, SelectedHull, SelectedStern, SelectedBow, SelectedBridge);

            if (!allPaths.Any())
            {
                ComputingPath = false;
                OptimizedRoute = (0, new List<SubmarineExplorationPretty>());
                BestPath = Array.Empty<uint>();
                return;
            }

            PluginLog.Verbose($"List Count: {paths.Count}");
            PluginLog.Verbose("Starting distance calculation");
            var optimalDistances = allPaths.AsParallel().Select(Submarines.CalculateDistance).Where(t => t.Distance <= build.Range).ToArray();
            PluginLog.Verbose("Done distance calculation");
            if (!optimalDistances.Any())
            {
                ComputingPath = false;
                OptimizedRoute = (0, new List<SubmarineExplorationPretty>());
                BestPath = Array.Empty<uint>();
                return;
            }

            BestPath = optimalDistances.AsParallel().Select(t =>
            {
                var path = t.Points.Prepend(startPoint).ToArray();
                var rowIdPath = new List<uint>();
                uint exp = 0;
                foreach (var submarineExplorationPretty in path.Skip(1))
                {
                    rowIdPath.Add(submarineExplorationPretty.RowId);
                    exp += submarineExplorationPretty.ExpReward;
                }
                return new Tuple<uint[], TimeSpan, double>(
                    rowIdPath.ToArray(),
                    TimeSpan.FromSeconds(Submarines.CalculateDuration(path, build)),
                    exp
                );
            }).OrderByDescending(t => t.Item3 / t.Item2.TotalMinutes).Select(t => t.Item1).First();
            PluginLog.Verbose("Done optimal calculation");
            ComputingPath = false;
        }
    }

    static List<List<SubmarineExplorationPretty>> DeduplicateLists(List<List<uint>> inputLists, List<SubmarineExplorationPretty> points)
    {
        var hashSet = new HashSet<List<uint>>(new ListComparer());
        foreach (var list in inputLists)
        {
            hashSet.Add(list);
        }

        return hashSet.Select(t => t.Select(f => points.First(k => k.RowId == f)).ToList()).ToList();
    }

    private void ExpTab()
    {
        if (ImGui.BeginTabItem("Best Exp"))
        {
            if (ImGui.BeginChild("ExpSelector", new Vector2(0, -110)))
            {
                var maps = ExplorationSheet
                           .Where(r => r.Passengers)
                           .Where(r => ExplorationSheet.GetRow(r.RowId + 1)!.RankReq <= SelectedRank)
                           .Select(r => ToStr(r.Map.Value!.Name))
                           .ToArray();

                // prevent previous selection from being impossible when switching builds
                if (maps.Length <= SelectedMap)
                    SelectedMap = maps.Length - 1;

                var selectedMap = SelectedMap;
                ImGui.Combo("##mapsSelection", ref selectedMap, maps, maps.Length);
                if ((selectedMap != SelectedMap || BestPath == Array.Empty<uint>() || LastComputedRank != SelectedRank) && !ComputingPath)
                {
                    SelectedMap = selectedMap;
                    BestPath = Array.Empty<uint>();
                    ComputeStart = DateTime.Now;
                    Task.Run(FindBestPath);
                }

                var startPoint = ExplorationSheet.First(r => r.Map.Row == SelectedMap + 1).RowId;
                var height = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
                if (ImGui.BeginListBox("##bestPoints", new Vector2(-1, height)))
                {
                    if (ComputingPath)
                    {
                        ImGui.Text($"Loading {new string('.',(int)((DateTime.Now - ComputeStart).TotalMilliseconds / 500) % 5)}");
                    }
                    if (BestPath != Array.Empty<uint>())
                    {
                        foreach (var location in BestPath)
                        {
                            var p = ExplorationSheet.GetRow(location)!;
                            if (location > startPoint)
                                ImGui.Text($"{NumToLetter(location - startPoint)}. {UpperCaseStr(p.Destination)}");
                        }
                        OptimizedRoute = Submarines.CalculateDistance(BestPath.ToList().Prepend(startPoint).Select(t => ExplorationSheet.GetRow(t)!));
                    }

                    ImGui.EndListBox();
                }

            }
            ImGui.EndChild();

            ImGui.EndTabItem();
        }
    }
}
