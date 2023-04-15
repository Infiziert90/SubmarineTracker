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

public class LootWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    public static ExcelSheet<SubmarineRank> RankSheet = null!;
    public static ExcelSheet<SubmarinePart> PartSheet = null!;
    public static ExcelSheet<SubmarineMap> MapSheet = null!;
    public static ExcelSheet<SubmarineExploration> ExplorationSheet = null!;

    private static Vector2 IconSize = new(28, 28);

    public LootWindow(Plugin plugin, Configuration configuration) : base("Custom Loot Overview")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(320, 500),
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
        if (ImGui.BeginChild("SubContent", new Vector2(0, -50)))
        {
            if (ImGui.BeginTabBar("##LootTabBar"))
            {
                CustomLootTab();
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
        }
        ImGui.EndChild();
    }

    private void CustomLootTab()
    {
        if (ImGui.BeginTabItem("Custom"))
        {
            if (!Configuration.CustomLoot.Any())
            {
                ImGui.TextColored(ImGuiColors.ParsedOrange, "No Custom Loot");
                ImGui.TextColored(ImGuiColors.ParsedOrange, "Add items to the custom tab in your config.");

                ImGui.EndTabItem();
                return;
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            var numSubs = 0;
            var numVoyages = 0;
            var bigList = new Dictionary<Item, int>();
            foreach (var fc in Submarines.KnownSubmarines.Values)
            {
                fc.RebuildStats();
                numSubs += fc.Submarines.Count;
                numVoyages += fc.SubLoot.Values.Sum(subs => subs.Loot.Count);

                foreach (var (item, count) in fc.AllLoot.SelectMany(x=>x.Value))
                {
                    if (!Configuration.CustomLoot.Contains(item.RowId))
                        continue;

                    if(!bigList.ContainsKey(item)){
                        bigList.Add(item, count);
                    }
                    else
                    {
                        bigList[item] += count;
                    }
                }
            }

            if (!bigList.Any())
            {
                ImGui.TextColored(ImGuiColors.ParsedOrange, "Selected items haven't been found so far.");

                ImGui.EndTabItem();
                return;
            }

            if (ImGui.BeginTable($"##customLootTable", 3))
            {
                ImGui.TableSetupColumn("##icon", 0, 0.15f);
                ImGui.TableSetupColumn("##item");
                ImGui.TableSetupColumn("##amount", 0, 0.3f);

                foreach (var (item, count) in bigList)
                {
                    ImGui.TableNextColumn();
                    DrawIcon(item.Icon);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(ToStr(item.Name));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{count}");
                    ImGui.TableNextRow();
                }
            }
            ImGui.EndTable();

            ImGuiHelpers.ScaledDummy(10.0f);
            ImGui.TextWrapped($"Your {numSubs} submarines have collected this loot over a combined {numVoyages} voyages");

            ImGui.EndTabItem();
        }
    }

    public SubmarinePart GetPart(int partId) => PartSheet.GetRow((uint) partId)!;

    private static void DrawIcon(uint iconId)
    {
        var texture = TexturesCache.Instance!.GetTextureFromIconId(iconId);
        ImGui.Image(texture.ImGuiHandle, IconSize);
    }
}
