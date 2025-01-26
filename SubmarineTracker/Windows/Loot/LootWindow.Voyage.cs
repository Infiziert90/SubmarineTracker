using static SubmarineTracker.Data.Loot;

namespace SubmarineTracker.Windows.Loot;

public partial class LootWindow
{
    private static readonly int MaxLength = "Craftsman's Command Mat".Length;

    private uint SelectedSubmarine;
    private int SelectedVoyage;

    private void VoyageTab()
    {
        using var tabItem = ImRaii.TabItem($"{Loc.Localize("Loot Tab - History", "History")}##VoyageHistory");
        if (!tabItem.Success)
            return;

        Dictionary<uint, (string Title, ulong LocalId)> existingSubs = new();
        foreach (var (id, knownFC) in Plugin.DatabaseCache.GetFreeCompanies())
            foreach (var s in Plugin.DatabaseCache.GetSubmarines(id))
                existingSubs.Add(s.Register, ($"{Plugin.NameConverter.GetName(knownFC)} - {s.Name} ({s.Build.FullIdentifier()})", id));

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
            ImGui.TextColored(ImGuiColors.ParsedOrange, Loc.Localize("Loot Tab History - Wrong", "Something went wrong."));
            return;
        }

        var dict = new Dictionary<uint, List<SubmarineTracker.Loot>>();
        foreach (var l in submarineLoot.Where(loot => !Plugin.Configuration.ExcludeLegacy || loot.Valid))
            if (!dict.TryAdd(l.Return, [l]))
                dict[l.Return].Add(l);

        var lootHistory = dict.OrderByDescending(pair => pair.Key).Select(pair => pair.Value).ToArray();
        if (lootHistory.Length == 0)
        {
            ImGui.TextColored(ImGuiColors.ParsedOrange, Loc.Localize("Loot Tab History - Not Tracked", "Tracking starts when you send your subs on voyage again."));
            return;
        }

        Helper.ClippedCombo("##voyageSelection", ref SelectedVoyage, lootHistory, entry => $"{entry[0].Date}");
        Helper.DrawArrows(ref SelectedVoyage, lootHistory.Length, 2);

        ImGuiHelpers.ScaledDummy(5.0f);

        var loot = lootHistory[SelectedVoyage];
        var stats = loot[0];
        if (stats.Valid)
            ImGui.TextColored(ImGuiColors.TankBlue, $"{Loc.Localize("Terms - Rank", "Rank")}: {stats.Rank} SRF: {stats.Surv}, {stats.Ret}, {stats.Fav}");
        else
            ImGui.TextColored(ImGuiColors.ParsedOrange, Loc.Localize("Loot Tab History - Legacy Data", "-Legacy Data-"));

        ImGuiHelpers.ScaledDummy(5.0f);

        foreach (var detailedLoot in loot)
        {
            var primaryItem = Sheets.ItemSheet.GetRow(detailedLoot.Primary)!;
            var additionalItem = Sheets.ItemSheet.GetRow(detailedLoot.Additional)!;

            ImGui.TextColored(ImGuiColors.HealerGreen, Sheets.ExplorationSheet.GetRow(detailedLoot.Sector)!.ToName());
            using var indent = ImRaii.PushIndent(10.0f);
            if (stats.Valid)
                ImGui.TextUnformatted($"DD: {ProcToText(detailedLoot.FavProc)} --- Ret: {ProcToText(detailedLoot.PrimaryRetProc)}");

            using var table = ImRaii.Table("##VoyageLootTable", 4);
            if (!table.Success)
                return;

            ImGui.TableSetupColumn("##icon", 0, 0.2f);
            ImGui.TableSetupColumn("##item");
            ImGui.TableSetupColumn("##amount", 0, 0.2f);
            ImGui.TableSetupColumn("##survProc", 0, 0.4f);

            ImGui.TableNextColumn();
            Helper.DrawScaledIcon(primaryItem.Icon, IconSize);

            var name = Utils.ToStr(primaryItem.Name);
            if (MaxLength < name.Length)
                name = name.Truncate(MaxLength);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(name);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Utils.ToStr(primaryItem.Name));

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"x{detailedLoot.PrimaryCount}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{(stats.Valid ? ProcToText(detailedLoot.PrimarySurvProc) : "")}");

            ImGui.TableNextRow();

            if (detailedLoot.ValidAdditional)
            {
                ImGui.TableNextColumn();
                Helper.DrawScaledIcon(additionalItem.Icon, IconSize);

                name = Utils.ToStr(additionalItem.Name);
                if (MaxLength < name.Length)
                    name = name.Truncate(MaxLength);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(name);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Utils.ToStr(additionalItem.Name));

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"x{detailedLoot.AdditionalCount}");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{(stats.Valid ? ProcToText(detailedLoot.AdditionalSurvProc) : "")}");

                ImGui.TableNextRow();
            }
        }
    }
}
