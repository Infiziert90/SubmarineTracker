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

    private static float RankMaxLength = ImGui.CalcTextSize("Rank 105").X + 25.0f;
    private static Vector2 IconSize = new(32, 32);

    public MainWindow(Plugin plugin, Configuration configuration) : base("Tracker")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.BeginChild("SubContent", new Vector2(0, -50)))
        {
            if (!Submarines.KnownSubmarines.Values.Any(s => s.Submarines.Any()))
            {
                ImGui.TextColored(ImGuiColors.ParsedOrange, "No Data");
                return;
            }

            foreach (var fc in Submarines.KnownSubmarines.Values.Where(value => value.Submarines.Any()))
            {
                ImGui.TextUnformatted($"FC: {fc.Tag}@{fc.World}");
                foreach (var sub in fc.Submarines)
                {
                    ImGui.Indent(10.0f);

                    ImGui.TextColored(ImGuiColors.TankBlue, $"Rank {sub.Rank}");
                    ImGui.SameLine(RankMaxLength);
                    ImGui.TextColored(ImGuiColors.TankBlue, $"({sub.BuildIdentifier()})");
                    ImGui.TextColored(ImGuiColors.HealerGreen, sub.Name);

                    if (Configuration.ShowExtendedPartsList)
                    {
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
            }
        }
        ImGui.EndChild();

        ImGuiHelpers.ScaledDummy(5);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);

        if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedBlue);
            if (ImGui.Button("Reload"))
                Submarines.LoadCharacters();
            ImGui.PopStyleColor();
        }
        ImGui.EndChild();
    }

    private static void DrawIcon(uint iconId)
    {
        var texture = TexturesCache.Instance!.GetTextureFromIconId(iconId);
        ImGui.Image(texture.ImGuiHandle, IconSize);
    }
}
