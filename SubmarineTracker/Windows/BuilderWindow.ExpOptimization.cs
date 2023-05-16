using System;
using System.Collections.Generic;
using ImGuiNET;
using SubmarineTracker.Data;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface.Colors;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows;

public partial class BuilderWindow
{
    private uint[] BestPath = Array.Empty<uint>();
    private bool ComputingPath;
    private int LastComputedRank;
    private DateTime ComputeStart = DateTime.Now;
    private bool Error;

    private bool Calculate = false;

    private void FindBestPath()
    {
        Error = false;
        LastComputedRank = CurrentBuild.Rank;
        var startPoint = ExplorationSheet.First(r => r.Map.Row == CurrentBuild.Map + 1);

        if (Submarines.KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
        {
            List<SubmarineExplorationPretty> valid;
            try
            {
                valid = ExplorationSheet
                            .Where(r => r.Map.Row == CurrentBuild.Map + 1 && !r.Passengers && fcSub.UnlockedSectors[r.RowId] && r.RankReq <= CurrentBuild.Rank)
                            .ToList();
            }
            catch (KeyNotFoundException)
            {
                Error = true;
                ComputingPath = false;
                Calculate = false;
                CurrentBuild.NoOptimized();
                BestPath = Array.Empty<uint>();
                return;
            }

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
            var build = new Submarines.SubmarineBuild(CurrentBuild);

            if (!allPaths.Any())
            {
                ComputingPath = false;
                Calculate = false;
                CurrentBuild.NoOptimized();
                BestPath = Array.Empty<uint>();
                return;
            }

            var optimalDistances = allPaths.AsParallel().Select(Submarines.CalculateDistance).Where(t => t.Distance <= build.Range).ToArray();
            if (!optimalDistances.Any())
            {
                ComputingPath = false;
                Calculate = false;
                CurrentBuild.NoOptimized();
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

            ComputingPath = false;
            Calculate = false;
        }
        else
        {
            Error = true;
            ComputingPath = false;
            Calculate = false;
        }
    }

    private void ExpTab()
    {
        if (ImGui.BeginTabItem("Best Exp"))
        {
            if (ImGui.BeginChild("ExpSelector", new Vector2(0, -110)))
            {
                if (ImGui.BeginChild("BestPath", new Vector2(0, -70)))
                {
                    var maps = ExplorationSheet
                       .Where(r => r.Passengers)
                       .Where(r => ExplorationSheet.GetRow(r.RowId + 1)!.RankReq <= CurrentBuild.Rank)
                       .Select(r => ToStr(r.Map.Value!.Name))
                       .ToArray();

                    // Always pick highest rank map if smaller then possible
                    if (maps.Length <= CurrentBuild.Map)
                        CurrentBuild.Map = maps.Length - 1;

                    var selectedMap = CurrentBuild.Map;
                    ImGui.Combo("##mapsSelection", ref selectedMap, maps, maps.Length);

                    var mapChanged = selectedMap != CurrentBuild.Map;
                    if (mapChanged)
                    {
                        Error = false;
                        ComputingPath = false;
                        BestPath = Array.Empty<uint>();
                    }
                    CurrentBuild.Map = selectedMap;

                    var beginCalculation = false;
                    if (Configuration.CalculateOnInteraction)
                    {
                        if (Calculate)
                            beginCalculation = true;
                    }
                    else
                    {
                        if (mapChanged || !BestPath.Any() || LastComputedRank != CurrentBuild.Rank)
                            beginCalculation = true;
                    }

                    if (beginCalculation && !ComputingPath && !Error)
                    {
                        BestPath = Array.Empty<uint>();
                        ComputeStart = DateTime.Now;
                        ComputingPath = true;
                        Task.Run(FindBestPath);
                    }

                    var startPoint = ExplorationSheet.First(r => r.Map.Row == CurrentBuild.Map + 1).RowId;
                    var height = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
                    if (ImGui.BeginListBox("##bestPoints", new Vector2(-1, height)))
                    {
                        if (ComputingPath)
                        {
                            ImGui.Text($"Loading {new string('.',(int)((DateTime.Now - ComputeStart).TotalMilliseconds / 500) % 5)}");
                        }
                        else if (!BestPath.Any())
                        {
                            ImGui.Text(Configuration.CalculateOnInteraction && !Calculate ? "Not calculated ..." : "No route found ...");
                        }

                        if (Error)
                        {
                            ImGui.TextWrapped("No Data, pls talk to the Voyage Control Panel -> Submersible Management.");
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
                            CurrentBuild.UpdateOptimized(Submarines.CalculateDistance(BestPath.ToList().Prepend(startPoint).Select(t => ExplorationSheet.GetRow(t)!)));
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
                    var changed = false;
                    ImGui.TextColored(ImGuiColors.DalamudViolet, "Options:");
                    ImGui.Indent(10.0f);
                    changed |= ImGui.Checkbox("Calculate only on button click", ref Configuration.CalculateOnInteraction);
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
}
