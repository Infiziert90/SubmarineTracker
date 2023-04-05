using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows;

public class BuilderWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    public static ExcelSheet<Item> ItemSheet = null!;
    public static ExcelSheet<SubmarineRank> RankSheet = null!;
    public static ExcelSheet<SubmarinePart> PartSheet = null!;
    public static ExcelSheet<SubmarineMap> MapSheet = null!;
    public static ExcelSheet<SubmarineExploration> ExplorationSheet = null!;

    public int SelectSub = 0;
    public int SelectedRank = 1;
    public int SelectedHull = 3;
    public int SelectedStern = 4;
    public int SelectedBow = 1;
    public int SelectedBridge = 2;

    public int SelectedMap;
    public List<uint> SelectedLocations = new();

    public int BreakpointT2 = 0;
    public int BreakpointT3 = 0;
    public int Normal = 0;
    public int Optimal = 0;
    public int Favor = 0;
    public int RequiredRange = 0;

    public int OrgSpeed;

    public BuilderWindow(Plugin plugin, Configuration configuration) : base("Builder")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 600),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = configuration;

        ItemSheet = Plugin.Data.GetExcelSheet<Item>()!;
        RankSheet = Plugin.Data.GetExcelSheet<SubmarineRank>()!;
        PartSheet = Plugin.Data.GetExcelSheet<SubmarinePart>()!;
        MapSheet = Plugin.Data.GetExcelSheet<SubmarineMap>()!;
        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExploration>()!;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.BeginChild("SubContent", new Vector2(0, -50)))
        {
            var sub = new Submarines.Submarine();

            if (ImGui.BeginChild("SubSelector", new Vector2(0, -140)))
            {
                if (ImGui.BeginTabBar("SubBuilderTab"))
                {
                    if (ImGui.BeginTabItem("Normal"))
                    {
                        var existingSubs = Submarines.KnownSubmarines.Values
                             .SelectMany(fc => fc.Submarines.Select(s => $"{s.Name} ({s.BuildIdentifier()})"))
                             .ToArray();
                        existingSubs = existingSubs.Prepend("Custom").ToArray();

                        var windowWidth = ImGui.GetWindowWidth() / 2;
                        ImGui.PushItemWidth(windowWidth - 5.0f);
                        ImGui.Combo("##existingSubs", ref SelectSub, existingSubs, existingSubs.Length);

                        ImGui.PopItemWidth();
                        ImGui.SameLine();
                        ImGui.PushItemWidth(windowWidth);
                        ImGui.SliderInt("##SliderRank", ref SelectedRank, 1, (int)RankSheet.Last().RowId, "Rank %d");
                        ImGui.PopItemWidth();

                        if (existingSubs[SelectSub] != "Custom")
                        {
                            var fc = Submarines.KnownSubmarines.Values.First(fc => fc.Submarines.Any(s => $"{s.Name} ({s.BuildIdentifier()})" == existingSubs[SelectSub]));
                            sub = fc.Submarines.First(s => $"{s.Name} ({s.BuildIdentifier()})" == existingSubs[SelectSub]);

                            SelectedRank = (int)sub.Rank;
                            SelectedHull = sub.Hull;
                            SelectedStern = sub.Stern;
                            SelectedBow = sub.Bow;
                            SelectedBridge = sub.Bridge;

                            var subBuild = new Submarines.SubmarineBuild(SelectedRank, SelectedHull, SelectedStern, SelectedBow, SelectedBridge);
                            OrgSpeed = subBuild.Speed;
                        }

                        ImGuiHelpers.ScaledDummy(5);

                        if (ImGui.BeginTable("##submarinePartSelection", 5))
                        {
                            ImGui.TableSetupColumn("Type");
                            ImGui.TableSetupColumn("Hull");
                            ImGui.TableSetupColumn("Stern");
                            ImGui.TableSetupColumn("Bow");
                            ImGui.TableSetupColumn("Bridge");

                            ImGui.TableHeadersRow();

                            BuildTableEntries("Shark", 0);
                            BuildTableEntries("Uniki", 4);
                            BuildTableEntries("Whale", 8);
                            BuildTableEntries("Coelac.", 12);
                            BuildTableEntries("Syldra", 16);

                            BuildTableEntries("MShark", 20);
                            BuildTableEntries("MUniki", 24);
                            BuildTableEntries("MWhale", 28);
                            BuildTableEntries("MCoelac.", 32);
                            BuildTableEntries("MSyldra", 36);
                        }
                        ImGui.EndTable();

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Route"))
                    {
                        var maps = MapSheet.Where(r => r.RowId != 0).Select(r => ToStr(r.Name)).ToArray();
                        var selectedMap = SelectedMap;
                        ImGui.Combo("##mapsSelection", ref selectedMap, maps, maps.Length);
                        if (selectedMap != SelectedMap)
                        {
                            SelectedMap = selectedMap;
                            SelectedLocations.Clear();
                        }

                        var explorations = ExplorationSheet
                                                .Where(r => r.Map.Row == SelectedMap + 1)
                                                .Where(r => !r.Passengers)
                                                .Where(r => !SelectedLocations.Contains(r.RowId))
                                                .ToList();

                        ImGui.TextColored(ImGuiColors.HealerGreen, $"Selected {SelectedLocations.Count} / 5");
                        var startPoint = ExplorationSheet.First(r => r.Map.Row == SelectedMap + 1).RowId;

                        var height = ImGui.CalcTextSize("X").Y * 6.5f; // 5 items max, we give padding space for 6.5
                        if (ImGui.BeginListBox("##selectedPoints", new Vector2(-1, height)))
                        {
                            foreach (var location in SelectedLocations.ToArray())
                            {
                                var p = ExplorationSheet.GetRow(location)!;
                                if (ImGui.Selectable($"{NumToLetter(location - startPoint)}. {ToStr(p.Location)}"))
                                    SelectedLocations.Remove(location);
                            }
                            ImGui.EndListBox();
                        }

                        ImGui.TextColored(ImGuiColors.ParsedOrange, $"Select with click");
                        if (ImGui.BeginListBox("##pointsToSelect", new Vector2(-1, height * 1.4f)))
                        {
                            foreach (var location in explorations)
                            {
                                if (SelectedLocations.Count < 5)
                                {
                                    if (ImGui.Selectable($"{NumToLetter(location.RowId - startPoint)}. {ToStr(location.Location)}"))
                                        SelectedLocations.Add(location.RowId);
                                }
                                else
                                {
                                    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ImGuiColors.DalamudRed);
                                    ImGui.Selectable($"{NumToLetter(location.RowId - startPoint)}. {ToStr(location.Location)}");
                                    ImGui.PopStyleColor();
                                }
                            }
                            ImGui.EndListBox();
                        }

                        ImGui.EndTabItem();
                    }
                }
                ImGui.EndTabBar();
            }
            ImGui.EndChild();

            if (ImGui.BeginChild("SubStats", new Vector2(0, 0)))
            {
                var build = new Submarines.SubmarineBuild(SelectedRank, SelectedHull, SelectedStern, SelectedBow,
                                                          SelectedBridge);

                // Reset to costum build if not equal anymore
                if (sub.IsValid() && !build.EqualsSubmarine(sub))
                    SelectSub = 0;

                var secondRow = 80.0f;
                var thirdRow = 130.0f;
                var fourthRow = 200.0f;
                var sixthRow = 250.0f;
                var seventhRow = 300.0f;
                ImGui.TextUnformatted("Calculated Stats:");
                ImGui.TextColored(ImGuiColors.HealerGreen, $"Surveillance");
                ImGui.SameLine(secondRow);
                SelectRequiredColor(BreakpointT3, build.Surveillance);

                ImGui.SameLine(thirdRow);
                ImGui.TextColored(ImGuiColors.HealerGreen, $"Retrieval");
                ImGui.SameLine(fourthRow);
                SelectRequiredColor(Optimal, build.Retrieval);

                ImGui.SameLine(sixthRow);
                ImGui.TextColored(ImGuiColors.HealerGreen, $"Favor");
                ImGui.SameLine(seventhRow);
                SelectRequiredColor(Favor, build.Favor);

                ImGui.TextColored(ImGuiColors.HealerGreen, $"Speed");
                ImGui.SameLine(secondRow);
                SelectOrgColor(OrgSpeed, build.Speed);

                var startPoint = ExplorationSheet.First(r => r.Map.Row == SelectedMap + 1).RowId;
                var points = SelectedLocations.Prepend(startPoint).ToList();
                RequiredRange = (int) Submarines.CalculateDistance(points);

                ImGui.SameLine(thirdRow);
                ImGui.TextColored(ImGuiColors.HealerGreen, $"Range");
                ImGui.SameLine(fourthRow);
                SelectRequiredColor(RequiredRange, build.Range);

                if (ImGui.CollapsingHeader("Required Stats"))
                {
                    // ImGui.InputInt("T2", ref BreakpointT2, 0);
                    ImGui.InputInt("T3", ref BreakpointT3, 0);
                    // ImGui.InputInt("Normal", ref Normal, 0);
                    ImGui.InputInt("Optimal", ref Optimal, 0);
                    ImGui.InputInt("Favor", ref Favor, 0);
                }
            }
            ImGui.EndChild();
        }
        ImGui.EndChild();

        ImGuiHelpers.ScaledDummy(5);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);

        if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedBlue);
            if (ImGui.Button("Reset"))
                Reset();
            ImGui.PopStyleColor();
        }
        ImGui.EndChild();
    }

    public void BuildTableEntries(string label, int number)
    {
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(label);
        ImGui.TableNextColumn();
        ImGui.RadioButton($"##{label}H", ref SelectedHull, number + 3);
        ImGui.TableNextColumn();
        ImGui.RadioButton($"##{label}S", ref SelectedStern, number + 4);
        ImGui.TableNextColumn();
        ImGui.RadioButton($"##{label}B", ref SelectedBow, number + 1);
        ImGui.TableNextColumn();
        ImGui.RadioButton($"##{label}Br", ref SelectedBridge, number + 2);
        ImGui.TableNextRow();
    }

    public void SelectOrgColor(int org, int current)
    {
        switch (org)
        {
            case 0:
                ImGui.TextUnformatted($"{current}");
                break;
            case var n when n > current:
                ImGui.TextColored(ImGuiColors.DalamudRed, $"{current}");
                break;
            case var n when n == current:
                ImGui.TextColored(ImGuiColors.HealerGreen, $"{current}");
                break;
            case var n when n < current:
                ImGui.TextColored(ImGuiColors.ParsedGold, $"{current}");
                break;
        }
    }

    public void SelectRequiredColor(int required, int current)
    {
        switch (required)
        {
            case 0:
                ImGui.TextUnformatted($"{current}");
                break;
            case var n when n > current:
                ImGui.TextColored(ImGuiColors.DalamudRed, $"{current} ({required})");
                break;
            case var n when n == current:
                ImGui.TextColored(ImGuiColors.HealerGreen, $"{current}");
                break;
            case var n when n < current:
                ImGui.TextColored(ImGuiColors.ParsedGold, $"{current}");
                break;
        }
    }

    public SubmarinePart GetPart(int partId) => PartSheet.GetRow((uint) partId)!;

    public void Reset()
    {
        SelectedRank = 1;
        SelectedHull = 3;
        SelectedStern = 4;
        SelectedBow = 1;
        SelectedBridge = 2;

        SelectedMap = 0;
        SelectedLocations.Clear();

        BreakpointT2 = 0;
        BreakpointT3 = 0;
        Normal = 0;
        Optimal = 0;
        Favor = 0;

        OrgSpeed = 0;
    }
}
