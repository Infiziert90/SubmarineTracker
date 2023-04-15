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

    private static ExcelSheet<Item> ItemSheet = null!;
    public static ExcelSheet<SubmarineExploration> ExplorationSheet = null!;

    private int SelectedSubmarine = 0;
    private int SelectedVoyage = 0;

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

        ItemSheet = Plugin.Data.GetExcelSheet<Item>()!;
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

                VoyageTab();
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
            if (!Configuration.CustomLootWithValue.Any())
            {
                ImGui.TextColored(ImGuiColors.ParsedOrange, "No Custom Loot");
                ImGui.TextColored(ImGuiColors.ParsedOrange, "Add items to the custom tab in your config.");

                ImGui.EndTabItem();
                return;
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            var numSubs = 0;
            var numVoyages = 0;
            var moneyMade = 0;
            var bigList = new Dictionary<Item, int>();
            foreach (var fc in Submarines.KnownSubmarines.Values)
            {
                fc.RebuildStats();
                numSubs += fc.Submarines.Count;
                numVoyages += fc.SubLoot.Values.Sum(subs => subs.Loot.Count);

                foreach (var (item, count) in fc.AllLoot.SelectMany(x=>x.Value))
                {
                    if (!Configuration.CustomLootWithValue.ContainsKey(item.RowId))
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

                    moneyMade += count * Configuration.CustomLootWithValue[item.RowId];
                }
            }
            ImGui.EndTable();

            ImGuiHelpers.ScaledDummy(10.0f);
            ImGui.TextWrapped($"Your {numSubs} submarines have collected this loot over a combined {numVoyages} voyages");
            ImGui.TextWrapped($"This made you a total of {moneyMade:N0} gil");

            ImGui.EndTabItem();
        }
    }

        private void VoyageTab()
    {
        if (ImGui.BeginTabItem("Voyage"))
        {
            var existingSubs = Submarines.KnownSubmarines.Values
                                         .SelectMany(fc => fc.Submarines.Select(s => $"{s.Name} ({s.BuildIdentifier()})"))
                                         .ToArray();
            var selectedSubmarine = SelectedSubmarine;
            ImGui.Combo("##existingSubs", ref selectedSubmarine, existingSubs, existingSubs.Length);
            if (selectedSubmarine != SelectedSubmarine)
            {
                SelectedSubmarine = selectedSubmarine;
                SelectedVoyage = 0;
            }

            var selectedSub = Submarines.KnownSubmarines.Values.SelectMany(fc => fc.Submarines).ToList()[SelectedSubmarine];
            var fc = Submarines.KnownSubmarines.Values.First(fcLoot => fcLoot.SubLoot.Values.Any(loot => loot.Loot.ContainsKey((uint) ((DateTimeOffset) selectedSub.ReturnTime).ToUnixTimeSeconds())));
            var submarineLoot = fc.SubLoot.Values.First(loot => loot.Loot.ContainsKey((uint)((DateTimeOffset)selectedSub.ReturnTime).ToUnixTimeSeconds()));

            var submarineVoyage = submarineLoot.Loot.Keys.Select(k => $"{DateTime.UnixEpoch.AddSeconds(k).ToLocalTime()}").ToArray();
            ImGui.Combo("##voyageSelection", ref SelectedVoyage, submarineVoyage, submarineVoyage.Length);

            var loot = submarineLoot.Loot.First(kv => $"{DateTime.UnixEpoch.AddSeconds(kv.Key).ToLocalTime()}" == submarineVoyage[SelectedVoyage]);
            foreach (var detailedLoot in loot.Value)
            {
                var primaryItem = ItemSheet.GetRow(detailedLoot.Primary)!;
                var additionalItem = ItemSheet.GetRow(detailedLoot.Additional)!;

                ImGui.TextUnformatted(ToStr(ExplorationSheet.GetRow(detailedLoot.Point)!.Location));

                if (ImGui.BeginTable($"##VoyageLootTable", 3))
                {
                    ImGui.TableSetupColumn("##icon", 0, 0.15f);
                    ImGui.TableSetupColumn("##item");
                    ImGui.TableSetupColumn("##amount", 0, 0.3f);

                    ImGui.TableNextColumn();
                    DrawIcon(primaryItem.Icon);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(ToStr(primaryItem.Name));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{detailedLoot.PrimaryCount}");
                    ImGui.TableNextRow();

                    if (detailedLoot.ValidAdditional)
                    {
                        ImGui.TableNextColumn();
                        DrawIcon(additionalItem.Icon);
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(ToStr(additionalItem.Name));
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{detailedLoot.AdditionalCount}");
                        ImGui.TableNextRow();
                    }
                }
                ImGui.EndTable();
            }
            ImGui.EndTabItem();
        }
    }

    private static void DrawIcon(uint iconId)
    {
        var texture = TexturesCache.Instance!.GetTextureFromIconId(iconId);
        ImGui.Image(texture.ImGuiHandle, IconSize);
    }
}
