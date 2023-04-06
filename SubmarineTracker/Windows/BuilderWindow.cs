using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
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

    public static ExcelSheet<SubmarineRank> RankSheet = null!;
    public static ExcelSheet<SubmarinePart> PartSheet = null!;
    public static ExcelSheet<SubmarineMap> MapSheet = null!;
    public static ExcelSheet<SubmarineExploration> ExplorationSheet = null!;

    public int SelectSub;
    public int SelectedRank = 1;
    public int SelectedHull = 3;
    public int SelectedStern = 4;
    public int SelectedBow = 1;
    public int SelectedBridge = 2;

    public int SelectedMap;
    public List<uint> SelectedLocations = new();

    public int OrgSpeed;

    public BuilderWindow(Plugin plugin, Configuration configuration) : base("Builder")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 650),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = configuration;

        RankSheet = Plugin.Data.GetExcelSheet<SubmarineRank>()!;
        PartSheet = Plugin.Data.GetExcelSheet<SubmarinePart>()!;
        MapSheet = Plugin.Data.GetExcelSheet<SubmarineMap>()!;
        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExploration>()!;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var infoTabOpen = false;

        if (ImGui.BeginChild("SubContent", new Vector2(0, -50)))
        {
            var sub = new Submarines.Submarine();

            if (ImGui.BeginChild("SubSelector", new Vector2(0, -85)))
            {
                if (ImGui.BeginTabBar("SubBuilderTab"))
                {
                    BuildTab(ref sub);

                    RouteTab();

                    infoTabOpen |= InfoTab();
                }
                ImGui.EndTabBar();
            }
            ImGui.EndChild();

            if (!infoTabOpen)
            {
                if (ImGui.BeginChild("SubStats", new Vector2(0, 0)))
                {
                    var build = new Submarines.SubmarineBuild(SelectedRank, SelectedHull, SelectedStern, SelectedBow,
                                                              SelectedBridge);

                    // Reset to costum build if not equal anymore
                    if (sub.IsValid() && !build.EqualsSubmarine(sub))
                        SelectSub = 0;

                    var startPoint = ExplorationSheet.First(r => r.Map.Row == SelectedMap + 1).RowId;
                    var points = SelectedLocations.Prepend(startPoint).ToList();
                    var optimizedDistance = Submarines.CalculateDistance(points);
                    var breakpoints = LootTable.CalculateRequired(SelectedLocations);

                    var windowWidth = ImGui.GetWindowWidth();
                    var secondRow = windowWidth / 5.1f;
                    var thirdRow = windowWidth / 2.9f;
                    var fourthRow = windowWidth / 2.0f;
                    var sixthRow = windowWidth / 1.5f;
                    var seventhRow = windowWidth / 1.3f;

                    ImGui.TextUnformatted("Calculated Stats:");
                    ImGui.TextColored(ImGuiColors.HealerGreen, $"Surveillance");
                    ImGui.SameLine(secondRow);
                    SelectRequiredColor(breakpoints.T2, build.Surveillance, breakpoints.T3);

                    ImGui.SameLine(thirdRow);
                    ImGui.TextColored(ImGuiColors.HealerGreen, $"Retrieval");
                    ImGui.SameLine(fourthRow);
                    SelectRequiredColor(breakpoints.Normal, build.Retrieval, breakpoints.Optimal);

                    ImGui.SameLine(sixthRow);
                    ImGui.TextColored(ImGuiColors.HealerGreen, $"Favor");
                    ImGui.SameLine(seventhRow);
                    SelectRequiredColor(breakpoints.Favor, build.Favor);

                    ImGui.TextColored(ImGuiColors.HealerGreen, $"Speed");
                    ImGui.SameLine(secondRow);
                    SelectOrgColor(OrgSpeed, build.Speed);

                    ImGui.SameLine(thirdRow);
                    ImGui.TextColored(ImGuiColors.HealerGreen, $"Range");
                    ImGui.SameLine(fourthRow);
                    SelectRequiredColor(optimizedDistance.Distance, build.Range);
                }
                ImGui.EndChild();
            }
        }
        ImGui.EndChild();

        ImGuiHelpers.ScaledDummy(5);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);

        if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
        {
            if (!infoTabOpen)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedBlue);
                if (ImGui.Button("Reset"))
                    Reset();
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedBlue);
                if (ImGui.Button("Submarine Discord"))
                    Dalamud.Utility.Util.OpenLink("https://discord.gg/GAVegXNtwK");
                ImGui.PopStyleColor();

                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedGrey);
                if (ImGui.Button("Spreadsheet"))
                    Dalamud.Utility.Util.OpenLink("https://docs.google.com/spreadsheets/d/1-j0a-I7bQdjnXkplP9T4lOLPH2h3U_-gObxAiI4JtpA/edit#gid=1894926908");
                ImGui.PopStyleColor();
            }
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

    private void BuildTab(ref Submarines.Submarine sub)
    {
        if (ImGui.BeginTabItem("Build"))
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
    }

    private void RouteTab()
    {
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
            if (ImGui.BeginListBox("##pointsToSelect", new Vector2(-1, height * 1.95f)))
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
                        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ImGuiColors.DPSRed);
                        ImGui.Selectable($"{NumToLetter(location.RowId - startPoint)}. {ToStr(location.Location)}");
                        ImGui.PopStyleColor();
                    }
                }
                ImGui.EndListBox();
            }

            ImGui.EndTabItem();
        }
    }

    private static bool InfoTab()
    {
        var open = ImGui.BeginTabItem("Info");
        if (open)
        {
            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.TextColored(ImGuiColors.DalamudViolet, "How are these stats calculated?");
            ImGui.TextUnformatted("All calculations are based on sheets from the game");
            ImGuiHelpers.ScaledDummy(10.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "How are these breakpoints calculated?");
            ImGui.TextWrapped("The range breakpoint is calculated using the same method as the game. However all other breakpoints are calculated off of community data gathered from the submarine discord.\nSpecial thanks to Mystic Spirit for maintaining the current sheet.");
            ImGuiHelpers.ScaledDummy(10.0f);

            var spacing = ImGui.CalcTextSize("Optimal").X + 20.0f;

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Breakpoints:");
            ImGui.TextUnformatted("T2");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Surveillance required for a chance to get loot from Tier 2");
            ImGui.TextUnformatted("T3");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Surveillance required for a chance to get loot from Tier 3");
            ImGui.TextUnformatted("Normal");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Retrieval required for normal retrieval level.");
            ImGui.TextUnformatted("Optimal");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Retrieval required for optimal retrieval level");
            ImGui.TextUnformatted("Favor");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Favor required for chance to get two items from a sector");

            ImGuiHelpers.ScaledDummy(10.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Colors:");
            ImGui.TextUnformatted("White");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Nothing selected or no data available");
            ImGui.TextColored(ImGuiColors.ParsedGold,"Gold");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Requirement exceeded");
            ImGui.TextColored(ImGuiColors.HealerGreen,"Green");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("T3/Optimal/Favor reached");
            ImGui.TextColored(ImGuiColors.ParsedPink,"Pink");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("T2/Normal reached, followed by T3/Optimal");
            ImGui.TextColored(ImGuiColors.DalamudRed,"Red");
            ImGui.SameLine(spacing);
            ImGui.TextUnformatted("Requirement not fulfilled, followed by requirement");
        }
        ImGui.EndTabItem();

        return open;
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

    public void SelectRequiredColor(int minRequired, int current, int maxRequired = -1)
    {
        if (minRequired == 0)
        {
            ImGui.TextUnformatted($"{current}");
        }
        else if (minRequired > current)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, $"{current} ({minRequired})");
        }
        else if (maxRequired == -1)
        {
            if (minRequired == current)
                ImGui.TextColored(ImGuiColors.HealerGreen, $"{current}");
            else
                ImGui.TextColored(ImGuiColors.ParsedGold, $"{current} ({minRequired})");
        }
        else
        {
            if (maxRequired == current)
                ImGui.TextColored(ImGuiColors.HealerGreen, $"{current}");
            else if (current > minRequired && current < maxRequired)
                ImGui.TextColored(ImGuiColors.ParsedPink, $"{current} ({maxRequired})");
            else
                ImGui.TextColored(ImGuiColors.ParsedGold, $"{current} ({maxRequired})");
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

        OrgSpeed = 0;
    }
}
