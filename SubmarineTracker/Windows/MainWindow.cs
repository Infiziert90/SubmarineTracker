using System;
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

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    public static ExcelSheet<SubmarineMap> MapSheet = null!;
    public static ExcelSheet<SubmarineExploration> ExplorationSheet = null!;

    private ulong CurrentSelection;
    private static Vector2 IconSize = new(28, 28);

    public MainWindow(Plugin plugin, Configuration configuration) : base("Tracker")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(710, 460),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = configuration;

        MapSheet = Plugin.Data.GetExcelSheet<SubmarineMap>()!;
        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExploration>()!;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGuiHelpers.ScaledDummy(5.0f);

        if (!Submarines.KnownSubmarines.Values.Any(s => s.Submarines.Any()))
        {
            ImGui.PushTextWrapPos();
            ImGui.TextColored(ImGuiColors.ParsedOrange, "No Data, pls talk to the Voyage Control Panel -> Submersible Management.");
            ImGui.PopTextWrapPos();
            return;
        }

        if (ImGui.BeginChild("SubContent", new Vector2(0, -50)))
        {
            var buttonWidth = ImGui.CalcTextSize("XXXXX@Halicarnassus").X + 10;
            ImGui.Columns(2, "columns", true);
            ImGui.SetColumnWidth(0, buttonWidth + 20);

            if (ImGui.BeginChild("##fcList"))
            {
                if (!Submarines.KnownSubmarines.ContainsKey(CurrentSelection))
                    CurrentSelection = Submarines.KnownSubmarines.Keys.First();

                var current = CurrentSelection;

                foreach (var (key, fc) in Submarines.KnownSubmarines.Where((kv) => kv.Value.Submarines.Any()))
                {
                    var text = $"{fc.Tag}@{fc.World}##{key}";
                    if (current == key)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedPink);
                        if (ImGui.Button(text, new Vector2(buttonWidth, 0)))
                            CurrentSelection = key;
                        ImGui.PopStyleColor();
                    }
                    else
                        if (ImGui.Button(text, new Vector2(buttonWidth, 0)))
                            CurrentSelection = key;
                }
            }
            ImGui.EndChild();

            ImGui.NextColumn();

            var selectedFc = Submarines.KnownSubmarines[CurrentSelection];
            if (ImGui.BeginTabBar("##fcSubmarineDetail"))
            {
                if (ImGui.BeginTabItem("Overview"))
                {
                    ImGuiHelpers.ScaledDummy(5.0f);
                    var rankMaxLength = ImGui.CalcTextSize("Rank 105").X + 25.0f;

                    foreach (var sub in selectedFc.Submarines)
                    {
                        ImGui.Indent(10.0f);

                        ImGui.TextColored(ImGuiColors.HealerGreen, sub.Name);

                        ImGui.TextColored(ImGuiColors.TankBlue, $"Rank {sub.Rank}");
                        ImGui.SameLine(rankMaxLength);
                        ImGui.TextColored(ImGuiColors.TankBlue, $"({sub.BuildIdentifier()})");

                        var nameSpacing = ImGui.CalcTextSize("NameWith20Letters123").X + 15.0f;
                        if (sub.IsOnVoyage())
                        {
                            var time = "";
                            if (Configuration.ShowTimeInOverview)
                            {
                                time = " Done ";
                                var returnTime = sub.ReturnTime - DateTime.Now.ToUniversalTime();
                                if (returnTime.TotalSeconds > 0)
                                    time = $" {(int) returnTime.TotalHours:#00}:{returnTime:mm}:{returnTime:ss} ";
                            }

                            if (Configuration.ShowRouteInOverview)
                            {
                                var startPoint = Submarines.FindVoyageStartPoint(sub.Points.First());
                                time += $" {string.Join(" -> ", sub.Points.Select(p => NumToLetter(p - startPoint)))} ";
                            }

                            ImGui.SameLine(nameSpacing);
                            ImGui.TextColored(ImGuiColors.ParsedOrange, time.Length != 0 ? $"[{time}]" : "");
                        }
                        else
                        {

                            if (Configuration.ShowTimeInOverview || Configuration.ShowRouteInOverview)
                            {
                                ImGui.SameLine(nameSpacing);
                                ImGui.TextColored(ImGuiColors.ParsedOrange,"[No Voyage Data]");
                            }
                        }

                        ImGui.Unindent(10.0f);
                    }

                    ImGui.EndTabItem();
                }

                foreach (var (sub, idx) in selectedFc.Submarines.Select((val, i) => (val, i)))
                {
                    if (ImGui.BeginTabItem($"{sub.Name}##{idx}"))
                    {
                        DetailedSub(sub);
                        ImGui.EndTabItem();
                    }
                }

                if (ImGui.BeginTabItem("Loot"))
                {
                    ImGuiHelpers.ScaledDummy(5.0f);

                    selectedFc.RebuildStats();

                    if (ImGui.BeginChild("##LootOverview"))
                    {
                        if (!selectedFc.AllLoot.Any())
                        {
                            ImGui.TextColored(ImGuiColors.ParsedOrange, "No Data");
                            ImGui.TextColored(ImGuiColors.ParsedOrange, "Tracking starts when you send your subs on voyage again.");
                        }
                        else
                        {
                            if (ImGui.BeginTabBar("##fcLootMap"))
                            {
                                var halfWindowWidth = ImGui.GetWindowWidth() / 2;
                                foreach (var map in MapSheet.Where(r => r.RowId != 0))
                                {
                                    var text = LootTable.MapToShort(map.RowId);
                                    if (text == "")
                                        text = ToStr(map.Name);

                                    if (ImGui.BeginTabItem(text))
                                    {
                                        ImGuiHelpers.ScaledDummy(10.0f);
                                        var cursorPosition = ImGui.GetCursorPos();
                                        foreach (var ((point, loot), idx) in selectedFc.AllLoot.Where(kv => GetPoint(kv.Key).Map.Row == map.RowId).Select((val, i) => (val, i)))
                                        {
                                            if (idx % 2 == 0)
                                                cursorPosition = ImGui.GetCursorPos();
                                            else
                                            {
                                                cursorPosition.X += halfWindowWidth;
                                                ImGui.SetCursorPos(cursorPosition);
                                            }

                                            ImGui.TextUnformatted(ToStr(ExplorationSheet.GetRow(point)!.Location));
                                            ImGuiHelpers.ScaledDummy(5.0f);
                                            foreach (var ((item, count), iIdx) in loot.Select((val, ii) => (val, ii)))
                                            {
                                                ImGui.Indent(10.0f);
                                                if (idx % 2 == 1)
                                                    ImGui.SetCursorPosX(cursorPosition.X + 10.0f);
                                                DrawIcon(item.Icon);
                                                ImGui.SameLine();
                                                ImGui.TextUnformatted(ToStr(item.Name));
                                                var length = ImGui.CalcTextSize($"{count}").X;
                                                ImGui.SameLine(idx % 2 == 0 ? 220.0f - length : 220.0f + cursorPosition.X - length);
                                                ImGui.TextUnformatted($"{count}");

                                                ImGui.Unindent(10.0f);
                                            }

                                            ImGuiHelpers.ScaledDummy(10.0f);
                                        }

                                        ImGui.EndTabItem();
                                    }
                                }
                            }
                            ImGui.EndTabBar();
                        }
                    }
                    ImGui.EndChild();

                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }
        ImGui.EndChild();

        ImGuiHelpers.ScaledDummy(5);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);

        if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
        {
            if (ImGui.Button("Settings"))
                Plugin.DrawConfigUI();

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedBlue);
            if (ImGui.Button("Reload"))
                Submarines.LoadCharacters();
            ImGui.PopStyleColor();
        }
        ImGui.EndChild();
    }

    private void DetailedSub(Submarines.Submarine sub)
    {
        ImGuiHelpers.ScaledDummy(5.0f);
        ImGui.Indent(10.0f);

        if (ImGui.BeginTable($"##submarineOverview##{sub.Name}", 2))
        {
            ImGui.TableSetupColumn("##key", 0, 0.2f);
            ImGui.TableSetupColumn("##value");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted("Rank");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{sub.Rank}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted("Build");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{sub.BuildIdentifier()}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted("Repair");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{sub.Build.RepairCosts}");

            if (sub.ValidExpRange())
            {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("EXP");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{sub.CExp} / {sub.NExp}");
                ImGui.SameLine();
                ImGui.TextUnformatted($"{(double) sub.CExp / sub.NExp * 100.0:##0.00}%");
            }

            if (sub.IsOnVoyage())
            {
                AddTableSpacing();

                var time = "Done";
                var returnTime = sub.ReturnTime - DateTime.Now.ToUniversalTime();
                if (returnTime.TotalSeconds > 0)
                    time = $"{(int) returnTime.TotalHours:#00}:{returnTime:mm}:{returnTime:ss} h";

                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Time");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(time);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Date");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{sub.ReturnTime.ToLocalTime()}");

                var startPoint = Submarines.FindVoyageStartPoint(sub.Points.First());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Map");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{GetMapName(startPoint)}");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Route");
                ImGui.TableNextColumn();
                ImGui.TextWrapped($"{string.Join(" -> ", sub.Points.Select(p => NumToLetter(p - startPoint)))}");
            }

            AddTableSpacing();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted("WIP");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"More coming soon");
        }
        ImGui.EndTable();

        if (Configuration.ShowExtendedPartsList)
        {
            ImGuiHelpers.ScaledDummy(10.0f);

            if (ImGui.BeginTable($"##submarineOverview##{sub.Name}", 2))
            {
                ImGui.TableSetupColumn("##icon", 0, 0.15f);
                ImGui.TableSetupColumn("##partName");

                ImGui.TableNextColumn();
                DrawIcon(sub.HullIconId);
                ImGui.TableNextColumn();
                ImGui.TextColored(ImGuiColors.ParsedGold, sub.HullName);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                DrawIcon(sub.SternIconId);
                ImGui.TableNextColumn();
                ImGui.TextColored(ImGuiColors.ParsedGold, sub.SternName);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                DrawIcon(sub.BowIconId);
                ImGui.TableNextColumn();
                ImGui.TextColored(ImGuiColors.ParsedGold, sub.BowName);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                DrawIcon(sub.BridgeIconId);
                ImGui.TableNextColumn();
                ImGui.TextColored(ImGuiColors.ParsedGold, sub.BridgeName);
                ImGui.TableNextRow();
            }

            ImGui.EndTable();
        }

        ImGui.Unindent(10.0f);
    }

    private void AddTableSpacing()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGuiHelpers.ScaledDummy(10.0f);
        ImGui.TableNextRow();
    }

    private static void DrawIcon(uint iconId)
    {
        var texture = TexturesCache.Instance!.GetTextureFromIconId(iconId);
        ImGui.Image(texture.ImGuiHandle, IconSize);
    }

    private static string GetMapName(uint key) => ToStr(ExplorationSheet.First(r => r.RowId == key).Map.Value!.Name);
    private static SubmarineExploration GetPoint(uint key) => ExplorationSheet.GetRow(key)!;
}
