using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    private ulong CurrentSelection;

    private static float RankMaxLength = ImGui.CalcTextSize("Rank 105").X + 25.0f;
    private static Vector2 IconSize = new(32, 32);

    public MainWindow(Plugin plugin, Configuration configuration) : base("Tracker")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(550, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = configuration;
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

                    foreach (var sub in selectedFc.Submarines)
                    {
                        ImGui.Indent(10.0f);

                        ImGui.TextColored(ImGuiColors.HealerGreen, sub.Name);
                        ImGui.TextColored(ImGuiColors.TankBlue, $"Rank {sub.Rank}");
                        TextWithCalculatedSpacing($"({sub.BuildIdentifier()})", sub.cExp, sub.nExp);

                        ImGui.Unindent(10.0f);
                    }

                    ImGui.EndTabItem();
                }

                foreach (var (sub, idx) in selectedFc.Submarines.Select((val, i) => (val, i)))
                {
                    if (ImGui.BeginTabItem($"{sub.Name}##{idx}"))
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

                            if (sub.ValidExpRange())
                            {
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("EXP");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{sub.cExp} / {sub.nExp}");
                            }

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted("WIP");
                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"More coming soon");
                        }
                        ImGui.EndTable();

                        if (Configuration.ShowExtendedPartsList)
                        {
                            ImGuiHelpers.ScaledDummy(5);
                            ImGui.Separator();
                            ImGuiHelpers.ScaledDummy(5);

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
                        ImGui.EndTabItem();
                    }
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

    private void TextWithCalculatedSpacing(string build, uint count, uint total)
    {
        var perc = "";
        if (total > 0)
            perc = $"{(double) count / total * 100.0:##0.00} %%";

        ImGui.SameLine(RankMaxLength);
        ImGui.TextColored(ImGuiColors.TankBlue, build);
        ImGui.SameLine((RankMaxLength * 2.7f) - ImGui.CalcTextSize(perc).X);
        ImGui.TextColored(ImGuiColors.TankBlue, perc);
    }

    private static void DrawIcon(uint iconId)
    {
        var texture = TexturesCache.Instance!.GetTextureFromIconId(iconId);
        ImGui.Image(texture.ImGuiHandle, IconSize);
    }
}
