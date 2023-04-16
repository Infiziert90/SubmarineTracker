using System;
using System.Collections.Generic;
using Dalamud.Interface.Colors;
using ImGuiNET;
using SubmarineTracker.Data;
using System.Linq;
using System.Numerics;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows;

public partial class BuilderWindow
{
    private uint[] BestPath;

    private void ExpTab()
    {
        if (ImGui.BeginTabItem("Best Exp"))
        {
            if (ImGui.BeginChild("SubSelector", new Vector2(0, -110)))
            {
                var maps = MapSheet.Where(r => r.RowId != 0).Select(r => ToStr(r.Name)).ToArray();
                var selectedMap = SelectedMap;
                ImGui.Combo("##mapsSelection", ref selectedMap, maps, maps.Length);
                uint startPoint;
                if (selectedMap != SelectedMap || BestPath == null)
                {
                    SelectedMap = selectedMap;
                    startPoint = ExplorationSheet.First(r => r.Map.Row == SelectedMap + 1).RowId;

                    if (Submarines.KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
                    {
                        var valid = ExplorationSheet
                            .Where(r => r.Map.Row == SelectedMap + 1 && !r.Passengers & fcSub.UnlockedSectors[r.RowId] && r.RankReq <= SelectedRank)
                            .ToList();

                        var validPoints = valid.Select(t => t.RowId).ToList();

                        var paths = validPoints.Select(t => new[] { startPoint, t }).ToList();
                        var i = 1;
                        while (i++ < 5)
                        {
                            foreach (var path in paths.ToArray())
                            {
                                foreach (var validPoint in validPoints.Where(t => !path.Contains(t)))
                                {
                                    var pathNew = path.ToList();
                                    pathNew.Add(validPoint);
                                    paths.Add(pathNew.ToArray());
                                }
                            }
                        }

                        var build = new Submarines.SubmarineBuild(SelectedRank, SelectedHull, SelectedStern, SelectedBow,
                                                                  SelectedBridge);

                        BestPath = paths.Select(Submarines.CalculateDistance)
                                            .Where(t => t.Distance <= build.Range && t.Points.Any())
                                            .Select(t => new Tuple<uint[], TimeSpan, double>(
                                                        t.Points.ToArray(),
                                                        TimeSpan.FromSeconds(Submarines.CalculateDuration(t.Points.ToList().Prepend(startPoint), build)),
                                                        valid.Where(k => t.Points.Contains(k.RowId)).Select(k => (double)k.ExpReward).Sum()
                                                    )).OrderByDescending(t => t.Item3 / t.Item2.TotalMinutes).Select(t => t.Item1).First();
                        OptimizedRoute = Submarines.CalculateDistance(BestPath.ToList().Prepend(startPoint));
                    }
                }
                startPoint = ExplorationSheet.First(r => r.Map.Row == SelectedMap + 1).RowId;
                var height = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
                if (ImGui.BeginListBox("##bestPoints", new Vector2(-1, height)))
                {
                    foreach (var location in BestPath)
                    {
                        var p = ExplorationSheet.GetRow(location)!;
                        ImGui.Text($"{NumToLetter(location - startPoint)}. {ToStr(p.Location)}");
                    }

                    ImGui.EndListBox();
                }

            }
            ImGui.EndChild();

            ImGui.EndTabItem();
        }
    }
}
