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
        var startPoint = ExplorationSheet.First(r => r.Map.Row == SelectedMap + 1).RowId;

        if (Submarines.KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
        {
            var valid = ExplorationSheet
                .Where(r => r.Map.Row == SelectedMap + 1 && !r.Passengers & fcSub.UnlockedSectors[r.RowId] && r.RankReq <= SelectedRank)
                .ToList();

            var validPoints = valid.Select(t => t.RowId).ToList();

            var paths = validPoints.Select(t => new[] { startPoint, t }.ToList()).ToList();
            var i = 1;
            while (i++ < 5)
            {
                foreach (var path in paths.ToArray())
                {
                    foreach (var validPoint in validPoints.Where(t => !path.Contains(t)))
                    {
                        var pathNew = path.ToList();
                        pathNew.Add(validPoint);
                        paths.Add(pathNew.ToList());
                    }
                }
            }

            var build = new Submarines.SubmarineBuild(SelectedRank, SelectedHull, SelectedStern, SelectedBow, SelectedBridge);

            if (!paths.Any())
            {
                ComputingPath = false;
                OptimizedRoute = (0, new List<uint>());
                BestPath = Array.Empty<uint>();
                return;
            }

            PluginLog.Verbose("Deduplicating List");
            var deduplicatedLists = DeduplicateLists(paths);
            PluginLog.Verbose("Starting distance calculation");
            var optimalDistances = deduplicatedLists.AsParallel().Select(Submarines.CalculateDistance).Where(t => t.Distance <= build.Range).ToArray();
            PluginLog.Verbose("Done distance calculation");
            BestPath = optimalDistances.Select(t => new Tuple<uint[], TimeSpan, double>(
                                                   t.Points.ToArray(),
                                                   TimeSpan.FromSeconds(Submarines.CalculateDuration(t.Points.ToList().Prepend(startPoint), build)),
                                                   valid.Where(k => t.Points.Contains(k.RowId)).Select(k => (double)k.ExpReward).Sum()
                                               )).OrderByDescending(t => t.Item3 / t.Item2.TotalMinutes).Select(t => t.Item1).First();
            PluginLog.Verbose(BestPath.Length.ToString());
            ComputingPath = false;
        }
    }

    static List<List<uint>> DeduplicateLists(List<List<uint>> inputLists)
    {
        var hashSet = new HashSet<List<uint>>(new ListComparer());
        foreach (var list in inputLists)
        {
            hashSet.Add(list);
        }

        return hashSet.ToList();
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
                        OptimizedRoute = Submarines.CalculateDistance(BestPath.ToList().Prepend(startPoint));
                    }

                    ImGui.EndListBox();
                }

            }
            ImGui.EndChild();

            ImGui.EndTabItem();
        }
    }
}
