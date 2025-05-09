﻿using System.Collections;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Loot;

public partial class LootWindow
{
    private void RouteTab()
    {
        using var tabItem = ImRaii.TabItem($"{Language.LootTabRoutes}##RouteHistory");
        if (!tabItem.Success)
            return;

        Dictionary<uint, (string Title, ulong LocalId)> existingSubs = new();
        foreach (var (id, knownFC) in Plugin.DatabaseCache.GetFreeCompanies())
            foreach (var s in Plugin.DatabaseCache.GetSubmarines(id))
                existingSubs.Add(s.Register, ($"{Plugin.NameConverter.GetName(knownFC)}{s.Name} ({s.Build.FullIdentifier()})", id));

        if (existingSubs.Count == 0)
        {
            Helper.NoData();
            return;
        }

        var selectedSubmarine = SelectedSubmarine;
        if (!existingSubs.TryGetValue(SelectedSubmarine, out var preview))
            (selectedSubmarine, preview) = existingSubs.First();


        var selectedFC = preview.LocalId;
        using (var combo = ImRaii.Combo("##existingSubs", preview.Title))
        {
            if (combo.Success)
            {
                foreach (var (key, value) in existingSubs)
                    if (ImGui.Selectable($"{value.Title}##{key}"))
                        selectedSubmarine = key;
            }
        }
        Helper.DrawArrowsDictionary(ref selectedSubmarine, existingSubs.Keys.ToArray(), 1);

        if (SelectedSubmarine != selectedSubmarine)
        {
            SelectedSubmarine = selectedSubmarine;
            SelectedVoyage = 0;

            selectedFC = existingSubs[selectedSubmarine].LocalId;
        }

        var fc = Plugin.DatabaseCache.GetFreeCompanies()[selectedFC];
        var sub = Plugin.DatabaseCache.GetSubmarines().First(sub => sub.Register == SelectedSubmarine);

        var submarineLoot = Plugin.DatabaseCache.GetLoot().Where(l => l.FreeCompanyId == fc.FreeCompanyId).Where(l => l.Register == sub.Register).ToArray();
        if (submarineLoot.Length == 0)
        {
            Helper.TextColored(ImGuiColors.ParsedOrange, Language.LootTabHistoryWrong);
            return;
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        using var table = ImRaii.Table("RouteTable", 3);
        ImGui.TableSetupColumn(Language.TermsDate, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn(Language.TermsRoute);
        ImGui.TableSetupColumn(Language.TermsUnlocked, ImGuiTableColumnFlags.WidthFixed);

        ImGui.TableHeadersRow();

        var routeHistory = submarineLoot.GroupBy(l => l.Return).Select(l => l.ToArray()).ToArray();
        using var clipper = new ListClipper(routeHistory.Length, itemHeight: ImGui.CalcTextSize("W").Y * 1.1f);
        foreach (var i in clipper.Rows)
        {
            var detailedLoot = routeHistory[i];

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{detailedLoot[0].Date}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"({Voyage.SectorToMapThreeLetter(detailedLoot[0].Sector)}) {Utils.SectorsToPath(" -> ", detailedLoot.Select(s => s.Sector).ToList())}");

            var unlocks = detailedLoot.Where(l => l.Unlocked != 0).Select(l => l.Unlocked);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{string.Join(", ", unlocks.Select(u => Utils.NumToLetter(u, true)))}");
        }
    }
}
